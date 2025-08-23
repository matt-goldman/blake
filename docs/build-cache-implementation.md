# Build Cache Implementation Strategy

## Code Structure Overview

### New Files to Create

```
src/Blake.BuildTools/
├── Cache/
│   ├── IBuildCache.cs              # Core cache interface
│   ├── BuildCacheManifest.cs       # JSON data model
│   ├── CacheEntry.cs               # Individual file cache entry
│   ├── JsonBuildCache.cs           # JSON persistence implementation
│   └── FileHasher.cs               # Content hashing utilities
├── Generator/
│   └── SiteGenerator.cs            # Modified for cache integration
└── Utils/
    └── CacheConstants.cs           # Cache file names and constants
```

### Interface Design

```csharp
// IBuildCache.cs
public interface IBuildCache
{
    Task<CacheEntry?> GetCacheEntryAsync(string filePath);
    Task SetCacheEntryAsync(string filePath, CacheEntry entry);
    Task<bool> IsValidAsync();
    Task InvalidateAsync();
    Task SaveAsync();
    Task<BuildCacheStats> GetStatsAsync();
}

// CacheEntry.cs  
public class CacheEntry
{
    public string ContentHash { get; init; } = string.Empty;
    public DateTime LastModified { get; init; }
    public string? TemplatePath { get; init; }
    public string? OutputPath { get; init; }
    public Dictionary<string, string> Metadata { get; init; } = new();
}

// BuildCacheManifest.cs
public class BuildCacheManifest
{
    public string Version { get; init; } = "1.0";
    public DateTime LastBuild { get; init; }
    public Dictionary<string, CacheEntry> Files { get; init; } = new();
    public Dictionary<string, string> Configuration { get; init; } = new();
}
```

### Integration Points

#### 1. GenerationOptions Enhancement
```csharp
// Add to GenerationOptions.cs
public class GenerationOptions
{
    // ... existing properties ...
    public bool ForceRebuild { get; init; } = false;
    public bool CleanCache { get; init; } = false;
    public IBuildCache? Cache { get; init; }
}
```

#### 2. SiteGenerator Modifications
```csharp
// In SiteGenerator.BuildAsync()
public static async Task BuildAsync(GenerationOptions options, ILogger logger, CancellationToken cancellationToken)
{
    // Initialize cache
    var cache = options.Cache ?? new JsonBuildCache(options.ProjectPath);
    
    if (options.CleanCache)
    {
        await cache.InvalidateAsync();
    }
    
    // ... existing setup code ...
    
    // Enhanced processing with cache checking
    await BakeContentWithCache(context, options, cache, logger, cancellationToken);
    
    // Save cache after successful build
    await cache.SaveAsync();
}

private static async Task BakeContentWithCache(
    BlakeContext context,
    GenerationOptions options,
    IBuildCache cache,
    ILogger logger,
    CancellationToken cancellationToken)
{
    foreach (var mdPage in context.MarkdownPages)
    {
        // Check cache before processing
        var cacheEntry = await cache.GetCacheEntryAsync(mdPage.MdPath);
        var currentHash = await FileHasher.ComputeHashAsync(mdPage.MdPath);
        
        if (!options.ForceRebuild && 
            cacheEntry != null && 
            cacheEntry.ContentHash == currentHash &&
            await IsTemplateUnchanged(cacheEntry.TemplatePath, cache))
        {
            logger.LogDebug("⚡ Skipping unchanged file: {MdPath}", mdPage.MdPath);
            // Load from cache and add to GeneratedPages
            context.GeneratedPages.Add(LoadFromCache(cacheEntry));
            continue;
        }
        
        // Process file and update cache
        var generatedPage = await ProcessMarkdownFile(mdPage, options, logger, cancellationToken);
        context.GeneratedPages.Add(generatedPage);
        
        await cache.SetCacheEntryAsync(mdPage.MdPath, new CacheEntry
        {
            ContentHash = currentHash,
            LastModified = File.GetLastWriteTime(mdPage.MdPath),
            TemplatePath = mdPage.TemplatePath,
            OutputPath = generatedPage.OutputPath
        });
    }
}
```

#### 3. CLI Integration
```csharp
// In Program.cs BakeBlakeAsync()
var options = new GenerationOptions
{
    // ... existing options ...
    ForceRebuild = args.Contains("--force"),
    CleanCache = args.Contains("--clean-cache")
};
```

### Cache File Location Strategy

1. **Primary Location**: `.blake-cache.json` in project root (same directory as `.csproj`)
2. **Fallback**: Temporary directory if project root is read-only
3. **Detection Logic**: 
   ```csharp
   public static string GetCacheFilePath(string projectPath)
   {
       var primaryPath = Path.Combine(projectPath, ".blake-cache.json");
       
       try
       {
           // Test write access
           var testFile = Path.Combine(projectPath, ".blake-write-test");
           File.WriteAllText(testFile, "test");
           File.Delete(testFile);
           return primaryPath;
       }
       catch
       {
           // Fall back to temp directory
           var tempDir = Path.Combine(Path.GetTempPath(), "blake-cache");
           Directory.CreateDirectory(tempDir);
           var hash = ComputePathHash(projectPath);
           return Path.Combine(tempDir, $"blake-cache-{hash}.json");
       }
   }
   ```

### Template Change Detection

```csharp
public static async Task<bool> HasTemplateChanged(string templatePath, IBuildCache cache)
{
    var cacheEntry = await cache.GetCacheEntryAsync(templatePath);
    if (cacheEntry == null) return true;
    
    var currentHash = await FileHasher.ComputeHashAsync(templatePath);
    return currentHash != cacheEntry.ContentHash;
}

public static async Task<IEnumerable<string>> GetAffectedMarkdownFiles(string templatePath, BlakeContext context)
{
    return context.MarkdownPages
        .Where(md => md.TemplatePath == templatePath)
        .Select(md => md.MdPath);
}
```

### Error Handling Strategy

1. **Cache Corruption**: Always validate cache format and fall back to full rebuild
2. **File Access Errors**: Log warning and continue with full rebuild
3. **Hash Computation Errors**: Skip caching for affected files
4. **Concurrent Access**: Use file locking or atomic operations

```csharp
public class JsonBuildCache : IBuildCache
{
    public async Task<bool> IsValidAsync()
    {
        try
        {
            if (!File.Exists(_cacheFilePath)) return false;
            
            var json = await File.ReadAllTextAsync(_cacheFilePath);
            var manifest = JsonSerializer.Deserialize<BuildCacheManifest>(json);
            
            return manifest?.Version == SupportedVersion;
        }
        catch
        {
            return false;
        }
    }
    
    public async Task SaveAsync()
    {
        var tempFile = _cacheFilePath + ".tmp";
        try
        {
            var json = JsonSerializer.Serialize(_manifest, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            
            await File.WriteAllTextAsync(tempFile, json);
            File.Move(tempFile, _cacheFilePath); // Atomic operation
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }
}
```

### Testing Strategy

1. **Unit Tests**: Mock file system using `System.IO.Abstractions`
2. **Integration Tests**: Real file system with temporary directories
3. **Performance Tests**: Benchmark using `BenchmarkDotNet`

```csharp
// Example unit test structure
public class JsonBuildCacheTests
{
    private readonly IFileSystem _fileSystem;
    private readonly JsonBuildCache _cache;
    
    public JsonBuildCacheTests()
    {
        _fileSystem = new MockFileSystem();
        _cache = new JsonBuildCache("/test/project", _fileSystem);
    }
    
    [Fact]
    public async Task GetCacheEntry_ExistingFile_ReturnsEntry()
    {
        // Arrange
        var filePath = "/test/project/Posts/test.md";
        var expectedEntry = new CacheEntry { ContentHash = "abc123" };
        
        // Setup mock file system with cache file
        _fileSystem.AddFile("/test/project/.blake-cache.json", 
            JsonSerializer.Serialize(new BuildCacheManifest 
            { 
                Files = { [filePath] = expectedEntry } 
            }));
        
        // Act
        var result = await _cache.GetCacheEntryAsync(filePath);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedEntry.ContentHash, result.ContentHash);
    }
}
```

This implementation strategy provides a clear roadmap for implementing the build cache system while maintaining Blake's principles of simplicity and reliability.
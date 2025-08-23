# Incremental Builds Analysis for Blake

## Current State

Blake currently regenerates the entire site on every `blake bake` command, processing all markdown files regardless of whether they have changed. This results in:

- Unnecessary processing time for large sites
- Complete regeneration of all `.razor` files and content index
- No optimization for CI/CD pipelines or development workflows

## Current Build Flow

1. **Discovery**: Scan all folders for `template.razor` files
2. **Mapping**: Map markdown files to their corresponding templates  
3. **Processing**: For each markdown file:
   - Read and parse frontmatter
   - Render markdown to HTML
   - Combine with template to generate `.razor` file
4. **Output**: Write all generated files and `GeneratedContentIndex.cs`

## Evaluation of Incremental Build Options

### Option 1: Hash-based Content Tracking

**Approach**: Store content hashes in the generated site index and compare before regenerating.

**Pros**:
- Accurate detection of content changes
- Works with build systems and CI/CD
- Can detect changes in both content and frontmatter

**Cons**:
- Still requires reading all markdown files to compute hashes
- Limited performance improvement for file I/O
- Complex integration with current `GeneratedContentIndex`

**Implementation Complexity**: Medium

### Option 2: Timestamp-based Tracking  

**Approach**: Store file timestamps in generated filenames or site index.

**Pros**:
- Very fast to check (file metadata only)
- No need to read file contents for unchanged files
- Simple to implement

**Cons**:
- Generated site index is not committed (timestamps lost)
- File timestamps unreliable across different systems/git operations
- Cannot detect content changes with same timestamp
- Template changes not easily tracked

**Implementation Complexity**: Low

### Option 3: In-memory File Watcher

**Approach**: Use file system watchers during `blake serve` to rebuild only changed files.

**Pros**:
- Perfect for development and hot reload scenarios
- Real-time response to file changes
- Optimal for `blake serve` workflow

**Cons**:
- Only useful during development (`blake serve`)
- No benefit for CI/CD or one-time builds
- Complex state management for file dependencies
- Does not persist across tool invocations

**Implementation Complexity**: High

### Option 4: Build Cache Manifest (Recommended)

**Approach**: Create a persistent `.blake-cache.json` file tracking build state.

**Cache Contents**:
```json
{
  "version": "1.0",
  "lastBuild": "2024-01-15T10:30:00Z",
  "files": {
    "Posts/my-post.md": {
      "contentHash": "sha256:abc123...",
      "lastModified": "2024-01-15T09:00:00Z",
      "templatePath": "Posts/template.razor",
      "outputPath": ".generated/posts/MyPost.razor"
    },
    "Posts/template.razor": {
      "contentHash": "sha256:def456...",
      "lastModified": "2024-01-10T14:00:00Z"
    }
  },
  "config": {
    "projectPath": "/path/to/project",
    "useDefaultRenderers": true,
    "includeDrafts": false
  }
}
```

**Pros**:
- Comprehensive tracking of all build inputs
- Works in all scenarios (development, CI/CD, local builds)
- Can track markdown files, templates, and configuration changes
- Standard practice in build systems
- Extensible for future enhancements
- Reliable across different environments

**Cons**:
- Additional file to manage (standard in build tools)
- Slightly more complex implementation

**Implementation Complexity**: Medium

### Option 5: Hybrid Timestamp + Selective Hashing

**Approach**: Use timestamps for quick filtering, then compute hashes only for changed files.

**Pros**:
- Fast filtering using timestamps
- Accurate change detection via hashing
- Best of both approaches

**Cons**:
- More complex logic and edge cases
- Still vulnerable to timestamp reliability issues
- May not provide significant benefits over Option 4

**Implementation Complexity**: High

## Recommendation: Build Cache Manifest (Option 4)

### Rationale

1. **Universally Applicable**: Works for all Blake use cases (development, CI/CD, local builds)
2. **Comprehensive**: Tracks all relevant inputs (markdown, templates, configuration)
3. **Reliable**: Not dependent on file system timestamps or external factors
4. **Standard Practice**: Similar to `package-lock.json`, `.git`, etc.
5. **Extensible**: Can be enhanced with additional metadata over time
6. **Performance**: Provides actual performance benefits across all scenarios

### Integration Points

1. **CLI Commands**: All build-triggering commands benefit
   - `blake bake`: Skip unchanged files
   - `blake serve`: Faster initial build + file watcher for ongoing changes
   - `blake new`: Initialize empty cache

2. **MSBuild Integration**: Templates using MSBuild integration get automatic optimization

3. **Plugin System**: Plugins can participate in cache invalidation

## Implementation Strategy

### Phase 1: Core Infrastructure
- [ ] Design `IBuildCache` interface
- [ ] Implement `BuildCache` class with JSON persistence  
- [ ] Add cache file location logic (`.blake-cache.json` in project root)
- [ ] Create file content hashing utilities

### Phase 2: SiteGenerator Integration
- [ ] Modify `SiteGenerator.BuildAsync` to check cache before processing
- [ ] Implement selective file processing based on cache state
- [ ] Update cache after successful builds
- [ ] Handle cache invalidation for template changes

### Phase 3: CLI Enhancements  
- [ ] Add `--force` flag to bypass cache
- [ ] Add `--clean-cache` flag to reset cache
- [ ] Improve logging to show which files are being skipped/processed
- [ ] Update help text with new options

### Phase 4: Advanced Features
- [ ] Integrate file watcher with cache for `blake serve`
- [ ] Add configuration change detection
- [ ] Add plugin-contributed cache keys
- [ ] Performance metrics and reporting

### Phase 5: Testing & Documentation
- [ ] Unit tests for build cache functionality
- [ ] Integration tests for incremental build scenarios
- [ ] Performance benchmarks
- [ ] Documentation updates

## Expected Benefits

### Performance Improvements
- **Small changes**: 80-95% reduction in build time
- **No changes**: 90-99% reduction in build time  
- **Template changes**: Rebuild only affected content
- **Large sites**: Significant improvement with hundreds of pages

### Developer Experience
- Faster `blake bake` during development
- Faster `blake serve` startup
- Clear indication of what's being processed
- Reliable builds across different environments

### CI/CD Benefits
- Faster builds in continuous integration
- Reduced resource usage
- More efficient deployment pipelines

## Future Enhancements

1. **Distributed Caching**: Share cache across team/CI environments
2. **Dependency Tracking**: Track inter-page dependencies for more accurate rebuilds
3. **Parallel Processing**: Process independent files concurrently
4. **Smart Template Changes**: Partial rebuilds when templates change

## Risk Mitigation

1. **Cache Corruption**: Always validate cache before use, fall back to full rebuild
2. **Configuration Changes**: Detect changes in Blake configuration and invalidate cache
3. **Plugin Changes**: Provide plugin API for cache participation
4. **Version Compatibility**: Include cache format version for future migrations

## Success Metrics

1. **Build Time Reduction**: Measure improvement in typical scenarios
2. **Developer Adoption**: Track usage of incremental builds
3. **Reliability**: Monitor cache hit/miss rates and fallback scenarios
4. **User Feedback**: Gather feedback on performance improvements

## Conclusion

The build cache manifest approach provides the most comprehensive and reliable solution for incremental builds in Blake. It addresses all use cases while following established patterns from other build systems. The implementation can be done incrementally, providing immediate benefits while building toward more advanced features.
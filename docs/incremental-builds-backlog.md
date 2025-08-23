# Incremental Builds Implementation Backlog

This document outlines the sub-issues and implementation plan for Blake incremental builds using a build cache manifest approach.

## Phase 1: Core Infrastructure

### Issue 1.1: Design Build Cache Interface
**Epic**: Core Infrastructure  
**Story Points**: 3  
**Dependencies**: None

**Description**: Design the core interfaces and data structures for the build cache system.

**Acceptance Criteria**:
- [ ] Define `IBuildCache` interface with methods:
  - `Task<CacheEntry?> GetCacheEntryAsync(string filePath)`
  - `Task SetCacheEntryAsync(string filePath, CacheEntry entry)`
  - `Task<bool> IsValidAsync()`
  - `Task InvalidateAsync()`
  - `Task SaveAsync()`
- [ ] Define `CacheEntry` class with properties:
  - `string ContentHash`
  - `DateTime LastModified`
  - `string? TemplatePath`
  - `string? OutputPath`
- [ ] Define `BuildCacheManifest` class for JSON serialization
- [ ] Include comprehensive XML documentation

**Implementation Notes**:
- Use `System.Text.Json` for serialization
- Design for extensibility (additional metadata in future)
- Consider async patterns for file I/O

### Issue 1.2: Implement File Content Hashing
**Epic**: Core Infrastructure  
**Story Points**: 2  
**Dependencies**: Issue 1.1

**Description**: Create utilities for computing and comparing file content hashes.

**Acceptance Criteria**:
- [ ] Implement `FileHasher` class with methods:
  - `Task<string> ComputeHashAsync(string filePath)`
  - `Task<bool> HasChangedAsync(string filePath, string expectedHash)`
- [ ] Use SHA256 for content hashing
- [ ] Handle file read errors gracefully
- [ ] Include performance optimizations for large files
- [ ] Add unit tests for various file scenarios

**Implementation Notes**:
- Consider using memory-mapped files for large content
- Handle encoding considerations for text files
- Include error handling for file access issues

### Issue 1.3: Implement JSON Build Cache
**Epic**: Core Infrastructure  
**Story Points**: 5  
**Dependencies**: Issues 1.1, 1.2

**Description**: Implement the build cache using JSON persistence.

**Acceptance Criteria**:
- [ ] Implement `JsonBuildCache` class implementing `IBuildCache`
- [ ] Handle cache file location (`.blake-cache.json` in project root)
- [ ] Implement atomic file operations (write to temp, then rename)
- [ ] Handle cache file corruption gracefully (fallback to empty cache)
- [ ] Include cache format versioning for future migrations
- [ ] Add configuration validation (detect Blake setting changes)
- [ ] Comprehensive unit tests with file system mocking

**Implementation Notes**:
- Use `System.IO.Abstractions` for testable file operations
- Consider cache file locking for concurrent access
- Include migration path for future cache format versions

## Phase 2: SiteGenerator Integration

### Issue 2.1: Add Cache-Aware File Processing
**Epic**: SiteGenerator Integration  
**Story Points**: 8  
**Dependencies**: Issue 1.3

**Description**: Modify `SiteGenerator.BuildAsync` to use build cache for selective processing.

**Acceptance Criteria**:
- [ ] Integrate `IBuildCache` into `SiteGenerator.BuildAsync`
- [ ] Check cache before processing each markdown file
- [ ] Skip processing for unchanged files (content and template)
- [ ] Update cache entries after successful processing
- [ ] Maintain existing error handling and logging
- [ ] Preserve plugin integration (BeforeBake/AfterBake)
- [ ] Add logging to indicate skipped vs processed files

**Implementation Notes**:
- Add cache to `GenerationOptions` or `BlakeContext`
- Ensure plugin BeforeBake/AfterBake still receive full context
- Consider cache warming during initial scan

### Issue 2.2: Handle Template Change Detection
**Epic**: SiteGenerator Integration  
**Story Points**: 5  
**Dependencies**: Issue 2.1

**Description**: Implement cache invalidation when templates change.

**Acceptance Criteria**:
- [ ] Track template file hashes in cache
- [ ] Detect template changes during build
- [ ] Invalidate affected markdown files when template changes
- [ ] Handle cascading template changes (parent templates)
- [ ] Support both `template.razor` and `cascading-template.razor`
- [ ] Add tests for various template change scenarios

**Implementation Notes**:
- Consider template inheritance hierarchies
- Map template dependencies to markdown files
- Optimize for common case (no template changes)

### Issue 2.3: Regenerate Content Index Intelligently
**Epic**: SiteGenerator Integration  
**Story Points**: 3  
**Dependencies**: Issue 2.1

**Description**: Only regenerate `GeneratedContentIndex.cs` when necessary.

**Acceptance Criteria**:
- [ ] Track content index hash in cache
- [ ] Detect when index content would change
- [ ] Skip index regeneration if unchanged
- [ ] Ensure index includes all current pages (added/removed)
- [ ] Handle partial build scenarios correctly

**Implementation Notes**:
- Compare generated index content before writing
- Consider tracking individual page metadata changes
- Ensure consistent ordering for reproducible builds

## Phase 3: CLI Enhancements

### Issue 3.1: Add Cache Control CLI Options
**Epic**: CLI Enhancements  
**Story Points**: 3  
**Dependencies**: Issue 2.1

**Description**: Add command-line options for cache control.

**Acceptance Criteria**:
- [ ] Add `--force` flag to bypass cache and rebuild all
- [ ] Add `--clean-cache` flag to delete cache file
- [ ] Update help text with new options
- [ ] Ensure options work with all relevant commands (`bake`, `serve`)
- [ ] Add validation for option combinations

**Implementation Notes**:
- Integrate with existing `GenerationOptions`
- Consider adding `--cache-info` for diagnostics
- Update documentation with new flags

### Issue 3.2: Enhance Build Logging
**Epic**: CLI Enhancements  
**Story Points**: 2  
**Dependencies**: Issue 2.1

**Description**: Improve logging to show incremental build information.

**Acceptance Criteria**:
- [ ] Log cache hits/misses during build
- [ ] Show counts of processed vs skipped files
- [ ] Display cache file location and status
- [ ] Include performance metrics (time saved)
- [ ] Maintain existing log levels and verbosity

**Implementation Notes**:
- Use structured logging for metrics
- Consider adding build summary at end
- Ensure logs are helpful for debugging cache issues

## Phase 4: Advanced Features

### Issue 4.1: Integrate File Watcher with Cache
**Epic**: Advanced Features  
**Story Points**: 5  
**Dependencies**: Issues 2.1, 3.1

**Description**: Enhance `blake serve` with file watching for real-time updates.

**Acceptance Criteria**:
- [ ] Implement file system watcher for markdown and template files
- [ ] Update cache in real-time as files change
- [ ] Trigger selective rebuilds on file changes
- [ ] Handle multiple rapid changes efficiently (debouncing)
- [ ] Integrate with existing `blake serve` workflow

**Implementation Notes**:
- Use `FileSystemWatcher` with cross-platform considerations
- Consider debouncing for rapid file changes
- Handle watcher disposal and error recovery

### Issue 4.2: Add Configuration Change Detection
**Epic**: Advanced Features  
**Story Points**: 3  
**Dependencies**: Issue 1.3

**Description**: Detect changes in Blake configuration and invalidate cache appropriately.

**Acceptance Criteria**:
- [ ] Track Blake configuration options in cache
- [ ] Detect changes in `GenerationOptions` settings
- [ ] Invalidate cache when configuration changes
- [ ] Handle plugin configuration changes
- [ ] Support `.blake-config.json` or similar config files

**Implementation Notes**:
- Serialize relevant configuration to cache
- Consider plugin-specific configuration tracking
- Handle backward compatibility for configuration changes

### Issue 4.3: Plugin Cache Participation
**Epic**: Advanced Features  
**Story Points**: 5  
**Dependencies**: Issues 2.1, 4.2

**Description**: Allow plugins to participate in cache invalidation.

**Acceptance Criteria**:
- [ ] Extend `IBlakePlugin` with cache methods:
  - `Task<string[]> GetCacheKeysAsync(BlakeContext context)`
  - `Task<bool> ShouldInvalidateAsync(BlakeContext context, ICacheEntry entry)`
- [ ] Update plugin loading to register cache dependencies
- [ ] Ensure plugin changes trigger appropriate cache invalidation
- [ ] Add documentation for plugin cache participation

**Implementation Notes**:
- Design opt-in cache participation for plugins
- Consider plugin versioning in cache keys
- Ensure backward compatibility with existing plugins

## Phase 5: Testing & Documentation

### Issue 5.1: Comprehensive Unit Tests
**Epic**: Testing & Documentation  
**Story Points**: 8  
**Dependencies**: All Phase 1-4 issues

**Description**: Create comprehensive unit test suite for incremental builds.

**Acceptance Criteria**:
- [ ] Unit tests for all cache classes and interfaces
- [ ] Mock file system for reliable testing
- [ ] Test various cache scenarios (hit, miss, corruption)
- [ ] Test template change detection
- [ ] Test configuration change handling
- [ ] Test plugin cache participation
- [ ] Achieve >90% code coverage for cache-related code

**Implementation Notes**:
- Use `System.IO.Abstractions.TestingHelpers` for file system mocking
- Create test fixtures for common scenarios
- Consider property-based testing for cache validation

### Issue 5.2: Integration Tests
**Epic**: Testing & Documentation  
**Story Points**: 5  
**Dependencies**: Issues 5.1, all Phase 2 issues

**Description**: Create integration tests for end-to-end incremental build scenarios.

**Acceptance Criteria**:
- [ ] Test full incremental build workflows
- [ ] Test Blake CLI commands with cache
- [ ] Test template changes with real file system
- [ ] Test plugin integration with cache
- [ ] Test cache recovery from corruption
- [ ] Performance benchmarks for various site sizes

**Implementation Notes**:
- Use temporary directories for test isolation
- Include realistic test content and templates
- Measure and assert performance improvements

### Issue 5.3: Performance Benchmarks
**Epic**: Testing & Documentation  
**Story Points**: 3  
**Dependencies**: Issue 5.2

**Description**: Create benchmarks to measure incremental build performance.

**Acceptance Criteria**:
- [ ] Benchmark suite for various site sizes (10, 100, 1000+ pages)
- [ ] Measure full build vs incremental build times
- [ ] Test different change scenarios (content, template, config)
- [ ] Generate performance reports
- [ ] Include CI integration for performance regression detection

**Implementation Notes**:
- Use `BenchmarkDotNet` for reliable measurements
- Create representative test content
- Consider memory usage in addition to time

### Issue 5.4: Documentation Updates
**Epic**: Testing & Documentation  
**Story Points**: 3  
**Dependencies**: All implementation issues

**Description**: Update Blake documentation for incremental builds.

**Acceptance Criteria**:
- [ ] Update getting started guide with incremental build explanation
- [ ] Document new CLI options (`--force`, `--clean-cache`)
- [ ] Add troubleshooting section for cache issues
- [ ] Update plugin development guide with cache participation
- [ ] Include performance expectations and best practices

**Implementation Notes**:
- Update both inline help and external documentation
- Include examples of cache usage
- Consider adding FAQ section for common cache questions

## Implementation Timeline

### Sprint 1 (2-3 weeks): Foundation
- Issues 1.1, 1.2, 1.3
- Core cache infrastructure ready

### Sprint 2 (2-3 weeks): Basic Integration  
- Issues 2.1, 2.2, 2.3
- Basic incremental builds working

### Sprint 3 (1-2 weeks): CLI Polish
- Issues 3.1, 3.2
- User-facing features complete

### Sprint 4 (2-3 weeks): Advanced Features
- Issues 4.1, 4.2, 4.3
- Full feature set implemented

### Sprint 5 (2-3 weeks): Testing & Documentation
- Issues 5.1, 5.2, 5.3, 5.4
- Production-ready with full test coverage

## Success Criteria

### Performance Targets
- [ ] 80%+ reduction in build time for unchanged sites
- [ ] 50%+ reduction in build time for single file changes
- [ ] <1 second overhead for cache operations on typical sites

### Quality Targets
- [ ] >90% test coverage for cache-related code
- [ ] Zero cache-related bugs in first month after release
- [ ] Positive user feedback on build performance

### Adoption Targets
- [ ] Default behavior for all Blake commands
- [ ] Template authors adopt cache-friendly practices
- [ ] Plugin authors implement cache participation

## Risk Mitigation

### Technical Risks
1. **Cache Corruption**: Robust validation and fallback to full rebuild
2. **Performance Regression**: Comprehensive benchmarking and monitoring
3. **Plugin Compatibility**: Careful API design and backward compatibility

### User Experience Risks
1. **Complexity**: Hide cache details behind simple CLI interface
2. **Debugging**: Excellent logging and diagnostic options
3. **Migration**: Seamless upgrade path from current Blake versions

### Delivery Risks
1. **Scope Creep**: Stick to defined phases and success criteria
2. **Testing Complexity**: Invest early in testing infrastructure
3. **Documentation Debt**: Update documentation throughout implementation

This backlog provides a clear path to implementing incremental builds in Blake while maintaining the project's focus on simplicity and developer experience.
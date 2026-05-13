namespace Blake.BuildTools.Utils;

/// <summary>
/// Utility methods for version comparison and validation.
/// </summary>
public static class VersionUtils
{
    /// <summary>
    /// Compares two version strings with component-wise comparison to prevent false positives.
    /// This method provides leniency for legitimate version matches while being strict about preventing false positives.
    /// </summary>
    /// <param name="expectedVersion">The expected version (e.g., from package metadata)</param>
    /// <param name="actualVersion">The actual version (e.g., from file version info)</param>
    /// <returns>True if the versions match according to semantic versioning rules</returns>
    public static bool AreVersionsCompatible(string expectedVersion, string actualVersion)
    {
        // Compare versions component-wise for leniency, but avoid false positives
        if (Version.TryParse(expectedVersion, out var expected) && Version.TryParse(actualVersion, out var actual))
        {
            // Compare only as many components as are present in expectedVersion
            bool matches = true;
            if (expected.Major != actual.Major) matches = false;
            if (expected.Minor != -1 && expected.Minor != actual.Minor) matches = false;
            
            // Handle Build component: if expected doesn't specify Build (=-1), actual should be 0 or -1
            if (expected.Build == -1)
            {
                if (actual.Build != 0 && actual.Build != -1) matches = false;
            }
            else
            {
                if (expected.Build != actual.Build) matches = false;
            }
            
            // Handle Revision component: if expected doesn't specify Revision (=-1), actual should be 0 or -1
            if (expected.Revision == -1)
            {
                if (actual.Revision != 0 && actual.Revision != -1) matches = false;
            }
            else
            {
                if (expected.Revision != actual.Revision) matches = false;
            }
            
            return matches;
        }
        
        // Fallback: exact string match for non-parseable versions
        return expectedVersion == actualVersion;
    }
}
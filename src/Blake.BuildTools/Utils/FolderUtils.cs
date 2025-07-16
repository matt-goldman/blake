namespace Blake.CLI.Utils;

public static class FolderUtils
{
    public static void CleanFolder(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
        Directory.CreateDirectory(path);
    }
}

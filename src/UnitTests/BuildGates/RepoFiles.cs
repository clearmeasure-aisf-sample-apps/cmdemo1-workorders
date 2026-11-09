namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

internal static class RepoFiles
{
    public static string Root()
    {
        var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "PrivateBuild.ps1")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new FileNotFoundException("Repository root not found from the test directory.");
    }
}

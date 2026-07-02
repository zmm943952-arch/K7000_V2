using System;
using System.IO;

namespace RfpTestStation.Tests
{
    internal static class TestPaths
    {
        public static string RepoRoot()
        {
            var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (dir != null)
            {
                var profile = Path.Combine(dir.FullName, "Project", "Teststand", "RFP Auto Test Sequence", "profile.html");
                if (File.Exists(profile))
                {
                    return dir.FullName;
                }

                dir = dir.Parent;
            }

            throw new DirectoryNotFoundException("Cannot locate repository root from test output directory.");
        }

        public static string ProfileHtml()
        {
            return Path.Combine(RepoRoot(), "Project", "Teststand", "RFP Auto Test Sequence", "profile.html");
        }
    }
}

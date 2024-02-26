using AventStack.ExtentReports;
using AventStack.ExtentReports.Reporter;
using AventStack.ExtentReports.Reporter.Config;

namespace Atlas.Auto.Utils.Reporting
{
    public class ExtentService
    {
        private const string folderName = @"TestReport";
        private static readonly Lazy<ExtentReports> lazy = new Lazy<ExtentReports>(() => new ExtentReports());

        public static ExtentReports Instance { get { return lazy.Value; } }

        private ExtentService()
        {
        }

        static ExtentService()
        {
            var reporter = new ExtentSparkReporter(GetFilePath())
            {
                Config =
                {
                    Theme = Theme.Dark
                }
            };

            Instance.AttachReporter(reporter);
        }

        private static string GetFilePath()
        {
            var path = GetProjectRootDirectory();
            path = Path.Combine(path, folderName);
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            return path + @"\index.html";
        }

        private static string GetProjectRootDirectory()
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            var parentDirectoryInfo = Directory.GetParent(currentDirectory);
            return parentDirectoryInfo.Parent.Parent.FullName;
        }
    }
}

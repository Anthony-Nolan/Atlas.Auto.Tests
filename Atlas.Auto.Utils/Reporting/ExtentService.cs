using AventStack.ExtentReports;
using AventStack.ExtentReports.Reporter;
using AventStack.ExtentReports.Reporter.Config;

namespace Atlas.Auto.Utils.Reporting;

public class ExtentService
{
    private const string FolderName = @"TestReport";
    private static readonly Lazy<ExtentReports> ExtentReportsLazy = new(() => new ExtentReports());

    public static ExtentReports Instance => ExtentReportsLazy.Value;

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
        var path = Path.Combine(Environment.CurrentDirectory, FolderName);
        Directory.CreateDirectory(path);
        return Path.Combine(path, "index.html");
    }
}
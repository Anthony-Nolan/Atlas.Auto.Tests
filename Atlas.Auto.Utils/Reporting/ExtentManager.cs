using AventStack.ExtentReports;

namespace Atlas.Auto.Utils.Reporting
{
    public static class ExtentManager
    {
        private static Dictionary<string, ExtentTest> parentTestMap = new Dictionary<string, ExtentTest>();
        private static Dictionary<string, ExtentTest> childTestMap = new Dictionary<string, ExtentTest>();
        private static ThreadLocal<ExtentTest> parentTest = new ThreadLocal<ExtentTest>();
        private static ThreadLocal<ExtentTest> childTest = new ThreadLocal<ExtentTest>();

        private static readonly object syncLock = new object();

        // Creates a parent node in the report for the test fixture
        public static ExtentTest CreateForFixture(string testName, string description = null)
        {
            lock (syncLock)
            {
                parentTest.Value = ExtentService.Instance.CreateTest(testName, description);
                parentTestMap.Add(testName, parentTest.Value);
                return parentTest.Value;
            }
        }

        // Creates a node in the report for the individual test
        // node is added to the parent using the parentName
        // if the parent is not available, it will be created
        public static ExtentTest CreateForTest(string parentName, string testName, string description = null)
        {
            lock (syncLock)
            {
                ExtentTest parentTest = null;
                if (!parentTestMap.ContainsKey(parentName))
                {
                    parentTest = ExtentService.Instance.CreateTest(testName);
                    parentTestMap.Add(parentName, parentTest);
                }
                else
                {
                    parentTest = parentTestMap[parentName];
                }
                ExtentManager.parentTest.Value = parentTest;
                childTest.Value = parentTest.CreateNode(testName, description);
                childTestMap.Add(testName, childTest.Value);
                return childTest.Value;
            }
        }
    }
}

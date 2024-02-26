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

        // creates a parent test
        public static ExtentTest CreateTest(string testName, string description = null)
        {
            lock (syncLock)
            {
                parentTest.Value = ExtentService.Instance.CreateTest(testName, description);
                parentTestMap.Add(testName, parentTest.Value);
                return parentTest.Value;
            }
        }

        // creates a node
        // node is added to the parent using the parentName
        // if the parent is not available, it will be created
        public static ExtentTest CreateMethod(string parentName, string testName, string description = null)
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

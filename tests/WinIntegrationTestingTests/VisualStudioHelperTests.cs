using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WinIntegrationTesting;
using System.IO;

namespace WinIntegrationTestingTests
{
    [TestClass]
    public class VisualStudioHelperTests
    {
        [TestMethod]
        public void TestFindSolutionFolderAndFileForAssembly()
        {
            string solutionFolder = VisualStudioHelper.FindSolutionFolderForAssembly(typeof(VisualStudioHelperTests).Assembly);
            string solutionFile = VisualStudioHelper.FindSolutionFileForAssembly(typeof(VisualStudioHelperTests).Assembly);

            string visualStudioHelperTestsFile = Path.Combine(solutionFolder, "tests", "WinIntegrationTestingTests", "VisualStudioHelperTests.cs");
            Assert.IsTrue(File.Exists(visualStudioHelperTestsFile));

            string expectedSolutionFile = Path.Combine(solutionFolder, "WinIntegrationTesting.sln");
            Assert.AreEqual(expectedSolutionFile, solutionFile);
        }

        [TestMethod]
        public void TestFindSolutionFolderAndFileForSubFolder()
        {
            string solutionFolder = VisualStudioHelper.FindSolutionFolderForAssembly(typeof(VisualStudioHelperTests).Assembly);
            string solutionFile = VisualStudioHelper.FindSolutionFileForAssembly(typeof(VisualStudioHelperTests).Assembly);

            string testProjectFolder = Path.Combine(solutionFolder, "tests", "WinIntegrationTestingTests");

            string solutionFolderForSubFolder = VisualStudioHelper.FindSolutionFolderForSubFolder(testProjectFolder);
            Assert.AreEqual(solutionFolder, solutionFolderForSubFolder);

            string solutionFileForSubFolder = VisualStudioHelper.FindSolutionFileForSubFolder(testProjectFolder);
            Assert.AreEqual(solutionFile, solutionFileForSubFolder);
        }
    }
}

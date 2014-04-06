using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Globalization;

namespace FirstFloor.Xcc.Test
{
    [TestClass]
    public class PageTests
    {
        [TestMethod]
        public void TestMyPageWin81Debug()
        {
            TestXaml("WINDOWS_APP;DEBUG", "MyPage.xaml", "MyPage.Win81.Debug.expected.xaml");
        }

        [TestMethod]
        public void TestMyPageWP81Debug()
        {
            TestXaml("WINDOWS_PHONE_APP;DEBUG", "MyPage.xaml", "MyPage.WP81.Debug.expected.xaml");
        }

        [TestMethod]
        public void TestMyPageWin81Release()
        {
            TestXaml("WINDOWS_APP;!DEBUG", "MyPage.xaml", "MyPage.Win81.Release.expected.xaml");
        }

        [TestMethod]
        public void TestMyPageWP81Release()
        {
            TestXaml("WINDOWS_PHONE_APP;!DEBUG", "MyPage.xaml", "MyPage.WP81.Release.expected.xaml");
        }

        private static void TestXaml(string symbols, string xamlName, string expectedXamlName)
        {
            var preprocessor = new XamlPreprocessor(symbols);
            var xaml = LoadXamlPage(xamlName);
            var expected = LoadXamlPage(expectedXamlName);
            var result = preprocessor.ProcessXaml(xaml);

            // perform char-by-char comparison, raise error with index info if mismatch
            var lineNumber = 1;
            for (var i = 0; i < expected.Length && i < result.Length; i++) {
                if (expected[i] != result[i]) {
                    Assert.Fail("Character mismatch at index {0} (line number: {1}. Expected: {2} ({3}), actual: {4} ({5})", i, lineNumber, result[i], (int)result[i], expected[i], (int)expected[i]);
                }
                if (result[i] == '\n') {
                    lineNumber++;
                }
            }

            // still fail if one string is substring of the other 
            Assert.AreEqual(expected, result);
        }

        private static string LoadXamlPage(string pageName)
        {
            var fullName = string.Format(CultureInfo.InvariantCulture, "FirstFloor.Xcc.Test.Xaml.{0}", pageName);

            using (var stream = typeof(PageTests).Assembly.GetManifestResourceStream(fullName)) {
                using (var reader = new StreamReader(stream)) {
                    return reader.ReadToEnd();
                }
            }
        }
    }
}

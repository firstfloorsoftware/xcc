using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FirstFloor.Xcc.Test
{
    [TestClass]
    public class XmlTests
    {
        [TestMethod]
        public void TestInputShouldNotChange()
        {
            var xml =  @"
<Test xmlns:d=""http://schemas.microsoft.com/expression/blend/2008""
      xmlns:mc=""http://schemas.openxmlformats.org/markup-compatibility/2006""
      mc:Ignorable=""d"">
  <Element d:Foo=""42"" />
  <d:Element />
</Test>";
            TestXml(null, false, xml, xml);
        }

        [TestMethod]
        public void TestRemoveIgnorableContent()
        {
            var xml = @"
<Test xmlns:d=""http://schemas.microsoft.com/expression/blend/2008""
      xmlns:mc=""http://schemas.openxmlformats.org/markup-compatibility/2006""
      mc:Ignorable=""d"">
  <Element d:Foo=""42"" />
  <d:Element />
</Test>";
            var expected = @"
<Test>


  <Element />
  
</Test>";

            TestXml(null, true, xml, expected);
        }

        [TestMethod]
        public void TestRootNotInDefaultNamespace()
        {
            var xml = @"
<foo:Test xmlns=""http://xcc.com""
      xmlns:debug=""condition:DEBUG""
      xmlns:d=""http://schemas.microsoft.com/expression/blend/2008""
      xmlns:mc=""http://schemas.openxmlformats.org/markup-compatibility/2006""
      xmlns:foo=""http://foo""
      mc:Ignorable=""d"">
  <Element d:Foo=""42"" />
  <debug:Element />
</foo:Test>";
            var expected = @"
<foo:Test xmlns=""http://xcc.com"" xmlns:d=""http://schemas.microsoft.com/expression/blend/2008"" xmlns:mc=""http://schemas.openxmlformats.org/markup-compatibility/2006"" xmlns:foo=""http://foo"" mc:Ignorable=""d"">





  <Element d:Foo=""42"" />
  <Element />
</foo:Test>";

            TestXml("DEBUG", false, xml, expected);
        }

        [TestMethod]
        public void TestRootNotInDefaultNamespaceWithNoDefaultNamespace()
        {
            var xml = @"
<foo:Test xmlns:debug=""condition:DEBUG""
      xmlns:d=""http://schemas.microsoft.com/expression/blend/2008""
      xmlns:mc=""http://schemas.openxmlformats.org/markup-compatibility/2006""
      xmlns:foo=""http://foo""
      mc:Ignorable=""d"">
  <Element d:Foo=""42"" />
  <debug:Element />
</foo:Test>";
            var expected = @"
<foo:Test xmlns:d=""http://schemas.microsoft.com/expression/blend/2008"" xmlns:mc=""http://schemas.openxmlformats.org/markup-compatibility/2006"" xmlns:foo=""http://foo"" mc:Ignorable=""d"">




  <Element d:Foo=""42"" />
  <foo:Element />
</foo:Test>";

            TestXml("DEBUG", false, xml, expected);
        }

        [TestMethod]
        public void TestRootNotInDefaultNamespaceWithDefaultNamespaceRemoveIgnorableContent()
        {
            var xml = @"
<foo:Test xmlns=""http://xcc.com""
      xmlns:debug=""condition:DEBUG""
      xmlns:d=""http://schemas.microsoft.com/expression/blend/2008""
      xmlns:mc=""http://schemas.openxmlformats.org/markup-compatibility/2006""
      xmlns:foo=""http://foo""
      mc:Ignorable=""d"">
  <Element d:Foo=""42"" />
  <debug:Element />
</foo:Test>";
            var expected = @"
<foo:Test xmlns=""http://xcc.com"" xmlns:foo=""http://foo"">





  <Element />
  <Element />
</foo:Test>";

            TestXml("DEBUG", true, xml, expected);
        }

        [TestMethod]
        public void TestRootNotInDefaultNamespaceWithNoDefaultNamespaceRemoveIgnorableContent()
        {
            var xml = @"
<foo:Test xmlns:debug=""condition:DEBUG""
      xmlns:d=""http://schemas.microsoft.com/expression/blend/2008""
      xmlns:mc=""http://schemas.openxmlformats.org/markup-compatibility/2006""
      xmlns:foo=""http://foo""
      mc:Ignorable=""d"">
  <Element d:Foo=""42"" />
  <debug:Element />
</foo:Test>";
            var expected = @"
<foo:Test xmlns:foo=""http://foo"">




  <Element />
  <foo:Element />
</foo:Test>";

            TestXml("DEBUG", true, xml, expected);
        }

        private static void TestXml(string symbols, bool removeIgnorableContent, string xaml, string expected)
        {
            var preprocessor = new XamlPreprocessor(symbols, removeIgnorableContent);
            var result = preprocessor.ProcessXaml(xaml);

            Assert.AreEqual(expected, result);
        }
    }
}

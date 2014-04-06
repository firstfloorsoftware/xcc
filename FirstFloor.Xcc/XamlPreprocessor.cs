using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace FirstFloor.Xcc
{
    /// <summary>
    /// The actual XAML preprocessor
    /// </summary>
    public class XamlPreprocessor
    {
        private string[] definedSymbols;
        private Dictionary<string, bool> conditionResults = new Dictionary<string, bool>();

        /// <summary>
        /// Initializes a new instance of the <see cref="XamlPreprocessor"/> class.
        /// </summary>
        /// <param name="definedSymbols">The defined symbols.</param>
        public XamlPreprocessor(string definedSymbols)
        {
            this.definedSymbols = definedSymbols.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToArray();
        }

        /// <summary>
        /// Processes the specified source XAML file and writes the results to specified target path.
        /// </summary>
        /// <param name="sourcePath">The source path.</param>
        /// <param name="targetPath">The target path.</param>
        public void ProcessXamlFile(string sourcePath, string targetPath)
        {
            var xamlDoc = XDocument.Load(sourcePath, LoadOptions.PreserveWhitespace);
            ProcessXaml(xamlDoc);

            using (var stream = File.OpenWrite(targetPath)) {
                SaveDocument(xamlDoc, stream);
            }
        }

        /// <summary>
        /// Processes the specified source XAML and returns the result.
        /// </summary>
        /// <param name="xaml">The xaml.</param>
        /// <returns></returns>
        public string ProcessXaml(string xaml)
        {
            var xamlDoc = XDocument.Parse(xaml, LoadOptions.PreserveWhitespace);
            ProcessXaml(xamlDoc);

            using (var stream = new MemoryStream()) {
                SaveDocument(xamlDoc, stream);

                stream.Seek(0, SeekOrigin.Begin);
                using (var reader = new StreamReader(stream)) {
                    return reader.ReadToEnd();
                }
            }
        }

        private static void SaveDocument(XDocument doc, Stream output)
        {
            var settings = new XmlWriterSettings {
                Indent = false,
                OmitXmlDeclaration = true
            };

            // do not dispose writer, this will close stream
            var writer = XmlWriter.Create(output, settings);
            doc.Save(writer);
            writer.Flush();
        }

        private void ProcessXaml(XDocument xamlDoc)
        {
            // using a Stack rather than relatively slow recursion
            var stack = new Stack<XElement>();
            stack.Push(xamlDoc.Root);

            while (stack.Count > 0) {
                var element = stack.Pop();
                if (ProcessElement(element)) {
                    foreach (var e in element.Elements()) {
                        stack.Push(e);
                    }
                }
            }

            // clear mc:ProcessContent from root (XBF compiler chokes on it)
            var processContent = xamlDoc.Root.Attribute(XName.Get("ProcessContent", Xmlns.MarkupCompatibility));
            if (processContent != null) {
                processContent.Remove();
            }
        }

        private bool ProcessElement(XElement element)
        {
            // check if element should be included
            var elemMatch = Include(element.Name);
            if (elemMatch.HasValue) {
                if (elemMatch.Value) {
                    // move element to XAML namespace
                    element.Name = XName.Get(element.Name.LocalName, Xmlns.XamlPresentation);
                }
                else {
                    // remove element
                    element.Remove();

                    return false; // stop processing
                }
            }

            // process attributes
            foreach (var attribute in element.Attributes().ToArray()) {
                var attrMatch = Include(attribute.Name);

                if (attrMatch.HasValue) {
                    if (attrMatch.Value) {
                        // replace attribute
                        attribute.Remove();

                        var attributeName = XName.Get(attribute.Name.LocalName);

                        // make sure any existing attribute with this name is removed
                        var sameNameAttribute = element.Attributes().FirstOrDefault(a => a.Name == attributeName);
                        if (sameNameAttribute != null) {
                            sameNameAttribute.Remove();
                        }

                        element.Add(new XAttribute(attributeName, attribute.Value));
                    }
                    else {
                        // remove attribute
                        attribute.Remove();
                    }
                }
            }

            return true;
        }

        private bool? Include(XName name)
        {
            var condition = GetCondition(name);

            // no condition, ignore
            if (condition == null) {
                return null;
            }

            // try condition result cache
            bool result;
            if (!this.conditionResults.TryGetValue(condition, out result)) {
                if (condition.StartsWith("!")) {
                    var conditionName = condition.Substring(1);
                    result = !this.definedSymbols.Any(s => s == conditionName);
                }
                else {
                    result = this.definedSymbols.Any(s => s == condition);
                }

                this.conditionResults[condition] = result;
            }
            return result;
        }

        private static string GetCondition(XName name)
        {
            if (name.NamespaceName.StartsWith("condition:")) {
                return name.NamespaceName.Substring(10);
            }
            return null;
        }
    }
}

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
        private bool removeIgnorableContent;
        private Dictionary<string, bool> conditionResults = new Dictionary<string, bool>();

        /// <summary>
        /// Initializes a new instance of the <see cref="XamlPreprocessor"/> class.
        /// </summary>
        /// <param name="definedSymbols">The defined symbols.</param>
        /// <param name="removeIgnorableContent">Whether to remove ignorable content.</param>
        public XamlPreprocessor(string definedSymbols, bool removeIgnorableContent)
        {
            this.definedSymbols = (definedSymbols ?? string.Empty).Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToArray();
            this.removeIgnorableContent = removeIgnorableContent;
        }

        /// <summary>
        /// Processes the specified source XAML file and writes the results to specified target path.
        /// </summary>
        /// <param name="sourcePath">The source path.</param>
        /// <param name="targetPath">The target path.</param>
        /// <returns>A value indicating whether the target XAML file has been written. If no changes are made to the XAML, the targetPath is not written and false is returned.</returns>
        public bool ProcessXamlFile(string sourcePath, string targetPath)
        {
            var xamlDoc = XDocument.Load(sourcePath, LoadOptions.PreserveWhitespace);
            if (ProcessXaml(xamlDoc)) {
                // ensure target directory exists
                Directory.CreateDirectory(Path.GetDirectoryName(targetPath));

                using (var stream = File.Create(targetPath)) {
                    SaveDocument(xamlDoc, stream);
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Processes the specified source XAML and returns the result.
        /// </summary>
        /// <param name="xaml">The xaml.</param>
        /// <returns></returns>
        public string ProcessXaml(string xaml)
        {
            var xamlDoc = XDocument.Parse(xaml, LoadOptions.PreserveWhitespace);
            if (!ProcessXaml(xamlDoc)) {
                return xaml;        // no changes, return XAML as-is
            }

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

        private bool ProcessXaml(XDocument xamlDoc)
        {
            // indicates whether the XAML has been updated
            var updated = false;    

            // find the ignorable prefixes defined in optional mc:Ignorable
            var ignorableAttribute = xamlDoc.Root.Attribute(XName.Get("Ignorable", Xmlns.MarkupCompatibility));
            string[] ignorablePrefixes = null;
            if (ignorableAttribute != null) {
                ignorablePrefixes = ignorableAttribute.Value.Split(new char[0], StringSplitOptions.RemoveEmptyEntries);      //char[0] defaults to any whitespace
            }

            // lookup ignorable namespace names (excluding condition namespaces) that should be removed
            var removeNamespaceNames = new string[0];
            if (this.removeIgnorableContent && ignorablePrefixes != null) {
                removeNamespaceNames = (from a in xamlDoc.Root.Attributes()
                                        where a.IsNamespaceDeclaration && !IsCondition(a.Value) && ignorablePrefixes.Contains(a.Name.LocalName)
                                        select a.Value).ToArray();
            }
            
            // using a Stack rather than relatively slow recursion
            var stack = new Stack<XElement>();
            stack.Push(xamlDoc.Root);

            while (stack.Count > 0) {
                var element = stack.Pop();
                bool elementUpdated;
                if (ProcessElement(element, removeNamespaceNames, out elementUpdated)) {
                    foreach (var e in element.Elements()) {
                        stack.Push(e);
                    }
                }
                if (elementUpdated) {
                    updated = true;
                }
            }

            // clear markup compatibility and condition xmlns attributes from root
            //  * WinRT XBF compiler doesn't appreciate mc:ProcessContent
            //  * Xamarin Forms crashes on custom condition namespaces
            var removedPrefixes = new List<string>();
            foreach (var attr in from a in xamlDoc.Root.Attributes().ToArray()          // ToArray since we are modifying the attribute collection
                                 where a.Name == XName.Get("ProcessContent", Xmlns.MarkupCompatibility) || (a.IsNamespaceDeclaration && (IsCondition(a.Value) || removeNamespaceNames.Contains(a.Value)))
                                 select a) {
                attr.Remove();

                if (attr.IsNamespaceDeclaration) {
                    removedPrefixes.Add(attr.Name.LocalName);
                }

                updated = true;
            }
            
            if (this.removeIgnorableContent) {
                if (ignorableAttribute != null) {
                    // remove mc:Ignorable attribute entirely
                    ignorableAttribute.Remove();
                    updated = true;
                }

                // both mc:ProcessContent and mc:Ignorable have been removed, remove xmlns:mc="" as well
                var mcXmlnsAttribute = xamlDoc.Root.Attributes().FirstOrDefault(a => a.IsNamespaceDeclaration && a.Value == Xmlns.MarkupCompatibility);
                if (mcXmlnsAttribute != null) {
                    mcXmlnsAttribute.Remove();
                    updated = true;
                }
            }
            else if (removedPrefixes.Any()) {
                // update existing mc:Ignorable accordingly
                if (ignorableAttribute != null) {
                    ignorableAttribute.Value = string.Join(" ", from p in ignorablePrefixes
                                                                where !removedPrefixes.Contains(p)
                                                                select p);

                    // remove mc:Ignorable if value is empty
                    if (string.IsNullOrEmpty(ignorableAttribute.Value)) {
                        ignorableAttribute.Remove();
                    }
                }
            }

            return updated;
        }

        private bool ProcessElement(XElement element, string[] removeNamespaceNames, out bool updated)
        {
            updated = false;

            // check if element should be included
            var elemMatch = Include(element.Name, removeNamespaceNames);
            if (elemMatch.HasValue) {
                updated = true;

                if (elemMatch.Value) {
                    // move element to default namespace, if not found fall-back to root namespace
                    var root = element.Document.Root;
                    var defaultXmlns = root.Attributes("xmlns").FirstOrDefault();
                    var nsName = defaultXmlns != null ? defaultXmlns.Value : root.Name.NamespaceName;
                    element.Name = XName.Get(element.Name.LocalName, nsName);
                }
                else {
                    // remove element
                    element.Remove();

                    return false; // stop processing
                }
            }

            // process attributes
            foreach (var attribute in element.Attributes().ToArray()) {
                var attrMatch = Include(attribute.Name, removeNamespaceNames);

                if (attrMatch.HasValue) {
                    updated = true;

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

        private bool? Include(XName name, string[] removeNamespaceNames)
        {
            // first check if ignorable content should be removed
            if (this.removeIgnorableContent && removeNamespaceNames.Contains(name.NamespaceName)) {
                return false;
            }

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
            if (IsCondition(name.NamespaceName)) {
                return name.NamespaceName.Substring(10);
            }
            return null;
        }

        private static bool IsCondition(string value)
        {
            return value.StartsWith("condition:", StringComparison.InvariantCulture);
        }
    }
}

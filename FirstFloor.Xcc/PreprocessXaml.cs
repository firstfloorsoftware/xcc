using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace FirstFloor.Xcc
{
    /// <summary>
    /// The MSBuild task for preprocessing conditional compilation symbols in XAML files.
    /// </summary>
    public class PreprocessXaml
        : Task
    {
        /// <summary>
        /// The required DefinedSymbols parameter.
        /// </summary>
        [Required]
        public string DefinedSymbols { get; set; }
        /// <summary>
        /// The required ApplicationDefinitions parameter.
        /// </summary>
        [Required]
        public ITaskItem[] ApplicationDefinitions { get; set; }
        /// <summary>
        /// The required Pages parameter.
        /// </summary>
        [Required]
        public ITaskItem[] Pages { get; set; }
        /// <summary>
        /// The required OutputPath parameter.
        /// </summary>
        [Required]
        public string OutputPath { get; set; }

        /// <summary>
        /// The output OldApplicationDefinitions parameter.
        /// </summary>
        [Output]
        public ITaskItem[] OldApplicationDefinitions { get; set; }
        /// <summary>
        /// The output NewApplicationDefinitions parameter.
        /// </summary>
        [Output]
        public ITaskItem[] NewApplicationDefinitions { get; set; }
        /// <summary>
        /// The output OldPages parameter.
        /// </summary>
        [Output]
        public ITaskItem[] OldPages { get; set; }
        /// <summary>
        /// The output NewPages parameter.
        /// </summary>
        [Output]
        public ITaskItem[] NewPages { get; set; }
        /// <summary>
        /// The output GeneratedFiles parameter.
        /// </summary>
        [Output]
        public ITaskItem[] GeneratedFiles { get; set; }

        /// <summary>
        /// When overridden in a derived class, executes the task.
        /// </summary>
        /// <returns>
        /// true if the task successfully executed; otherwise, false.
        /// </returns>
        public override bool Execute()
        {
            try {
                var oldAppDefs = new List<ITaskItem>();
                var oldPages = new List<ITaskItem>();
                var newAppDefs = new List<ITaskItem>();
                var newPages = new List<ITaskItem>();

                var preprocessor = new XamlPreprocessor(this.DefinedSymbols);

                foreach (var appDef in this.ApplicationDefinitions) {
                    var newAppDef = ProcessFile(appDef, preprocessor);
                    if (newAppDef != null) {
                        oldAppDefs.Add(appDef);
                        newAppDefs.Add(newAppDef);
                    }
                }
                foreach (var page in this.Pages) {
                    var newPage = ProcessFile(page, preprocessor);
                    if (newPage != null) {
                        oldPages.Add(page);
                        newPages.Add(newPage);
                    }
                }

                this.OldApplicationDefinitions = oldAppDefs.ToArray();
                this.NewApplicationDefinitions = newAppDefs.ToArray();
                this.OldPages = oldPages.ToArray();
                this.NewPages = newPages.ToArray();
                this.GeneratedFiles = newAppDefs.Concat(newPages).ToArray();

                return true;
            }
            catch (Exception e) {
                Log.LogErrorFromException(e);

                return false;
            }
        }

        private ITaskItem ProcessFile(ITaskItem file, XamlPreprocessor preprocessor)
        {
            var sourcePath = file.GetMetadata("FullPath");

            // properly resolve linked xaml
            var targetRelativePath = file.GetMetadata("Link");
            if (string.IsNullOrEmpty(targetRelativePath)) {
                targetRelativePath = file.ItemSpec;
            }
            var targetPath = Path.Combine(this.OutputPath, targetRelativePath);

            TaskItem result = null;

            // process XAML
            Log.LogMessage(MessageImportance.High, "Preprocessing {0}", targetRelativePath);
            var start = DateTime.Now;
            if (preprocessor.ProcessXamlFile(sourcePath, targetPath)) {
                // targetPath has been written, create linked item
                result = new TaskItem(targetPath);
                file.CopyMetadataTo(result);
                result.SetMetadata("Link", targetRelativePath);          // this is the trick that makes it all work (replace page with a page link pointing to \obj\debug\preprocessedxaml\*)
            }

            var duration = (DateTime.Now - start).TotalMilliseconds;
            Log.LogMessage(MessageImportance.Low, "Preprocess completed in {0}ms, {1} has {2}changed", duration, targetRelativePath, result == null ? "not " : "");

            return result;
        }
    }
}

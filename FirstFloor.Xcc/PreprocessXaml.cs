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
        /// The output ProcessedApplicationDefinitions parameter.
        /// </summary>
        [Output]
        public ITaskItem[] ProcessedApplicationDefinitions { get; set; }
        /// <summary>
        /// The output ProcessedPages parameter.
        /// </summary>
        [Output]
        public ITaskItem[] ProcessedPages { get; set; }
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
                var processedAppDefs = new List<ITaskItem>();
                var processedPages = new List<ITaskItem>();
                var generatedFiles = new List<ITaskItem>();

                var preprocessor = new XamlPreprocessor(this.DefinedSymbols);

                foreach (var appDef in this.ApplicationDefinitions) {
                    ProcessFile(appDef, preprocessor, processedAppDefs, generatedFiles);
                }
                foreach (var page in this.Pages) {
                    ProcessFile(page, preprocessor, processedPages, generatedFiles);
                }

                this.ProcessedApplicationDefinitions = processedAppDefs.ToArray();
                this.ProcessedPages = processedPages.ToArray();
                this.GeneratedFiles = generatedFiles.ToArray();

                return true;
            }
            catch (Exception e) {
                Log.LogErrorFromException(e);

                return false;
            }
        }

        private void ProcessFile(ITaskItem file, XamlPreprocessor preprocessor, List<ITaskItem> processedFiles, List<ITaskItem> generatedFiles)
        {
            var sourcePath = file.GetMetadata("FullPath");

            // properly resolve linked xaml
            var targetRelativePath = file.GetMetadata("Link");
            if (string.IsNullOrEmpty(targetRelativePath)) {
                targetRelativePath = file.ItemSpec;
            }
            var targetPath = Path.Combine(this.OutputPath, targetRelativePath);

            // ensure target directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(targetPath));

            // process XAML
            Log.LogMessage(MessageImportance.High, "Preprocessing {0}", targetRelativePath);
            var start = DateTime.Now;
            preprocessor.ProcessXamlFile(sourcePath, targetPath);
            Log.LogMessage(MessageImportance.Low, "Preprocess completed in {0}ms", (DateTime.Now - start).TotalMilliseconds);

            // create a linked item to the target path
            var targetFile = new TaskItem(targetPath);
            file.CopyMetadataTo(targetFile);
            targetFile.SetMetadata("Link", targetRelativePath);          // this is the trick that makes it all work (replace page with a page link pointing to \obj\debug\preprocessedxaml\*)

            processedFiles.Add(targetFile);

            // and keep track of the generated file for cleanup
            generatedFiles.Add(new TaskItem(targetPath));
        }
    }
}

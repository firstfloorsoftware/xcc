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
        /// The required EmbeddedXamlResources parameter.
        /// </summary>
        [Required]
        public ITaskItem[] EmbeddedXamlResources { get; set; }
        /// <summary>
        /// The required OutputPath parameter.
        /// </summary>
        [Required]
        public string OutputPath { get; set; }
        /// <summary>
        /// Determines whether ignorable content should be removed.
        /// </summary>
        /// <value>
        /// <c>true</c> if [remove ignorable content]; otherwise, <c>false</c>.
        /// </value>
        public bool RemoveIgnorableContent { get; set; }

        /// <summary>
        /// The output NewApplicationDefinitions parameter.
        /// </summary>
        [Output]
        public ITaskItem[] NewApplicationDefinitions { get; set; }
        /// <summary>
        /// The output NewPages parameter.
        /// </summary>
        [Output]
        public ITaskItem[] NewPages { get; set; }
        /// <summary>
        /// The output NewEmbeddedXamlResources parameter.
        /// </summary>
        [Output]
        public ITaskItem[] NewEmbeddedXamlResources { get; set; }
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
                Log.LogMessage(MessageImportance.Normal, "XCC > DefinedSymbols: {0}", string.Join(",", this.DefinedSymbols));

                var preprocessor = new XamlPreprocessor(this.DefinedSymbols, this.RemoveIgnorableContent);

                var generatedFiles = new List<ITaskItem>();

                this.NewApplicationDefinitions = ProcessFiles(this.ApplicationDefinitions, generatedFiles, preprocessor).ToArray();
                this.NewPages = ProcessFiles(this.Pages, generatedFiles, preprocessor).ToArray();
                this.NewEmbeddedXamlResources = ProcessFiles(this.EmbeddedXamlResources, generatedFiles, preprocessor).ToArray();

                this.GeneratedFiles = generatedFiles.ToArray();

                return true;
            }
            catch (Exception e) {
                Log.LogErrorFromException(e);

                return false;
            }
        }

        private IEnumerable<ITaskItem> ProcessFiles(ITaskItem[] files, List<ITaskItem> generatedFiles, XamlPreprocessor preprocessor)
        {
            foreach (var file in files) {
                var newFile = ProcessFile(file, preprocessor);
                if (newFile != null) {
                    generatedFiles.Add(newFile);
                    yield return newFile;
                }
                else {
                    yield return file;      // return file as-is
                }
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

            // if targetRelativePath is still absolute, use file name
            if (Path.IsPathRooted(targetRelativePath)) {
                targetRelativePath = Path.GetFileName(targetRelativePath);
            }

            var targetPath = Path.Combine(this.OutputPath, targetRelativePath);

            TaskItem result = null;

            // process XAML
            Log.LogMessage(MessageImportance.High, "XCC > Preprocessing {0}", targetRelativePath);
            var start = DateTime.Now;
            if (preprocessor.ProcessXamlFile(sourcePath, targetPath)) {
                // targetPath has been written, create linked item
                result = new TaskItem(targetPath);
                file.CopyMetadataTo(result);
                result.SetMetadata("Link", targetRelativePath);          // this is the trick that makes it all work (replace page with a page link pointing to \obj\debug\preprocessedxaml\*)
            }

            var duration = (DateTime.Now - start).TotalMilliseconds;
            Log.LogMessage(MessageImportance.Normal, "XCC > Preprocess completed in {0}ms, {1} has {2}changed", duration, targetRelativePath, result == null ? "not " : "");

            return result;
        }
    }
}

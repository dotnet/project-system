// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.IO;

namespace Microsoft.VisualStudio.ProjectSystem.Properties.Package
{
    // Sets the ApplicationIcon property to the path to the .ico file. This path is relative
    // to the project file directory. If the selected file is not within the project directory
    // tree, the file will be copied into the project file directory. Previously copied files
    // are not deleted when a new file is selected. ApplicationIcon allows for both full paths
    // and relative paths anywhere on disk, but we are not utilizing that in this implementation.
    // The icon file does not need to be included in the project (such as a None/Content item).
    [ExportInterceptingPropertyValueProvider(ApplicationIconPropertyName, ExportInterceptingPropertyValueProviderFile.ProjectFile)]
    internal sealed class ApplicationIconValueProvider : InterceptingPropertyValueProviderBase
    {
        private const string ApplicationIconPropertyName = "ApplicationIcon";

        private readonly UnconfiguredProject _unconfiguredProject;
        private readonly IFileSystem _fileSystem;

        [ImportingConstructor]
        public ApplicationIconValueProvider(UnconfiguredProject unconfiguredProject, IFileSystem fileSystem)
        {
            _unconfiguredProject = unconfiguredProject;
            _fileSystem = fileSystem;
        }

        public override async Task<string?> OnSetPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties, IReadOnlyDictionary<string, string>? dimensionalConditions = null)
        {
            // Allows the user to clear the application icon.
            if (string.IsNullOrEmpty(unevaluatedPropertyValue))
            {
                return null;
            }

            // If selected path is invalid, ignore and return previous value.
            if (!File.Exists(unevaluatedPropertyValue))
            {
                return await defaultProperties.GetUnevaluatedPropertyValueAsync(propertyName);
            }

            // Copy the file into the project directory.
            // This isn't a limitation of ApplicationIcon; it is to simply mimic the legacy property page interaction.
            if (_unconfiguredProject.IsOutsideProjectDirectory(unevaluatedPropertyValue))
            {
                string fileName = Path.GetFileName(unevaluatedPropertyValue);
                string destination = Path.Combine(Path.GetDirectoryName(_unconfiguredProject.FullPath), fileName);
                _fileSystem.CopyFile(unevaluatedPropertyValue, destination, overwrite: true);
                return fileName;
            }

            return PathHelper.MakeRelative(_unconfiguredProject, unevaluatedPropertyValue);
        }
    }
}

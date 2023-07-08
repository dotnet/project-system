// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.IO;

namespace Microsoft.VisualStudio.ProjectSystem.Properties.Package
{
    // Sets the ApplicationIcon property to the path to the .ico file. This path is relative
    // to the project file directory. If the selected file is not within the project directory
    // tree, the file will be copied into the project file directory. Previously copied files
    // are not deleted when a new file is selected. ApplicationIcon allows for both full paths
    // and relative paths anywhere on disk, but we are not utilizing that in this implementation.
    // The icon file does not need to be included in the project (such as a None/Content item).
    [ExportInterceptingPropertyValueProvider(ConfigurationGeneral.ApplicationIconProperty, ExportInterceptingPropertyValueProviderFile.ProjectFile)]
    internal sealed class ApplicationIconValueProvider : InterceptingPropertyValueProviderBase
    {
        private readonly IProjectItemProvider _sourceItemsProvider;
        private readonly UnconfiguredProject _unconfiguredProject;
        private readonly IFileSystem _fileSystem;

        [ImportingConstructor]
        public ApplicationIconValueProvider(
            [Import(ExportContractNames.ProjectItemProviders.SourceFiles)] IProjectItemProvider sourceItemsProvider,
            UnconfiguredProject unconfiguredProject,
            IFileSystem fileSystem)
        {
            _sourceItemsProvider = sourceItemsProvider;
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

            string propertyValue;
            // Copy the file into the project directory.
            // This isn't a limitation of ApplicationIcon; it is to simply mimic the legacy property page interaction.
            if (_unconfiguredProject.IsOutsideProjectDirectory(unevaluatedPropertyValue))
            {
                try
                {
                    propertyValue = Path.GetFileName(unevaluatedPropertyValue);
                    var destinationInfo = new FileInfo(Path.Combine(Path.GetDirectoryName(_unconfiguredProject.FullPath), propertyValue));
                    if (destinationInfo.Exists && destinationInfo.IsReadOnly)
                    {
                        // The file cannot be copied over; return the previous value.
                        return await defaultProperties.GetUnevaluatedPropertyValueAsync(propertyName);
                    }
                    _fileSystem.CopyFile(unevaluatedPropertyValue, destinationInfo.FullName, overwrite: true);
                }
                catch
                {
                    // If anything goes wrong with trying to copy the file, simply return the previous value.
                    return await defaultProperties.GetUnevaluatedPropertyValueAsync(propertyName);
                }
            }
            else
            {
                propertyValue = PathHelper.MakeRelative(_unconfiguredProject, unevaluatedPropertyValue);
            }

            string existingPropertyValue = await defaultProperties.GetEvaluatedPropertyValueAsync(ConfigurationGeneral.ApplicationIconProperty);
            IProjectItem? existingItem = await GetExistingContentItemAsync(existingPropertyValue);
            if (existingItem is not null)
            {
                await existingItem.SetUnevaluatedIncludeAsync(propertyValue);
            }
            else
            {
                await _sourceItemsProvider.AddAsync(Content.SchemaName, propertyValue);
            }

            return propertyValue;
        }

        private async Task<IProjectItem?> GetExistingContentItemAsync(string existingPropertyValue)
        {
            return await _sourceItemsProvider.GetItemAsync(Content.SchemaName, ci =>
                // If the filename of this item and the filename of the property's value match, consider those to be related to one another.
                ci.PropertiesContext is { IsProjectFile: true } && Path.GetFileName(ci.EvaluatedInclude).Equals(Path.GetFileName(existingPropertyValue), StringComparisons.PropertyLiteralValues));
        }
    }
}

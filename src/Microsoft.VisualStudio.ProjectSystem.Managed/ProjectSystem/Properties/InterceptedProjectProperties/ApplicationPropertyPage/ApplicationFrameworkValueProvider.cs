// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.VS;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    [ExportInterceptingPropertyValueProvider("UseApplicationFramework", ExportInterceptingPropertyValueProviderFile.ProjectFile)]
    [AppliesTo(ProjectCapability.VisualBasic)]
    internal sealed class ApplicationFrameworkValueProvider : InterceptingPropertyValueProviderBase
    {
        private const string ApplicationFrameworkMSBuildProperty = "MyType";
        private const string EnabledValue = "WindowsForms";
        private const string DisabledValue = "WindowsFormsWithCustomSubMain";
        
        private const string UseWPFMSBuildProperty = "UseWPF";
        private const string OutputTypeMSBuildProperty = "OutputType";
        private const string WinExeOutputType = "WinExe";
        private const string StartupObjectMSBuildProperty = "StartupObject";
        private const string NoneItemType = "None";
        private const string ApplicationDefinitionItemType = "ApplicationDefinition";

        private readonly UnconfiguredProject _project;
        private readonly IProjectItemProvider _sourceItemsProvider;

        [ImportingConstructor]
        public ApplicationFrameworkValueProvider(
            UnconfiguredProject project,
            [Import(ExportContractNames.ProjectItemProviders.SourceFiles)] IProjectItemProvider sourceItemsProvider)
        {
            _project = project;
            _sourceItemsProvider = sourceItemsProvider;
        }

        public override async Task<string?> OnSetPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties, IReadOnlyDictionary<string, string>? dimensionalConditions = null)
        {
            if (await IsWPFApplicationAsync(defaultProperties))
            {
                return await SetPropertyValueForWPFApplicationAsync(unevaluatedPropertyValue, defaultProperties);
            }
            else
            {
                return await SetPropertyValueForDefaultProjectTypesAsync(unevaluatedPropertyValue, defaultProperties);
            }
        }

        public override Task<string> OnGetEvaluatedPropertyValueAsync(string propertyName, string evaluatedPropertyValue, IProjectProperties defaultProperties)
        {
            return GetPropertyValueAsync(defaultProperties);
        }

        public override Task<string> OnGetUnevaluatedPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties)
        {
            return GetPropertyValueAsync(defaultProperties);
        }

        private async Task<string> GetPropertyValueAsync(IProjectProperties defaultProperties)
        {
            if (await IsWPFApplicationAsync(defaultProperties))
            {
                return await GetPropertyValueForWPFApplicationAsync(defaultProperties);
            }
            else
            {
                return await GetPropertyValueForDefaultProjectTypesAsync(defaultProperties);
            }
        }

        private async Task<string> GetPropertyValueForWPFApplicationAsync(IProjectProperties defaultProperties)
        {
            string startupObject = await defaultProperties.GetEvaluatedPropertyValueAsync(StartupObjectMSBuildProperty);
            if (!string.IsNullOrEmpty(startupObject))
            {
                // A start-up object is specified for this project. This takes precedence over the Startup URI, so set Use Application Framework to "false".
                return "false";
            }

            string? appXamlFilePath = await GetAppXamlRelativeFilePathAsync(create: false);
            if (string.IsNullOrEmpty(appXamlFilePath))
            {
                // No Application.xaml file; set Use Application Framework to "false".
                return "false";
            }

            return "true";
        }

        private static async Task<string> GetPropertyValueForDefaultProjectTypesAsync(IProjectProperties defaultProperties)
        {
            string? value = await defaultProperties.GetEvaluatedPropertyValueAsync(ApplicationFrameworkMSBuildProperty);

            return value switch
            {
                EnabledValue => "true",
                DisabledValue => "false",
                _ => string.Empty
            };
        }

        private static async Task<bool> IsWPFApplicationAsync(IProjectProperties defaultProperties)
        {
            string useWPFString = await defaultProperties.GetEvaluatedPropertyValueAsync(UseWPFMSBuildProperty);
            string outputTypeString = await defaultProperties.GetEvaluatedPropertyValueAsync(OutputTypeMSBuildProperty);

            return bool.TryParse(useWPFString, out bool useWPF)
                && useWPF
                && StringComparers.PropertyLiteralValues.Equals(outputTypeString, WinExeOutputType);
        }

        private static async Task<string?> SetPropertyValueForDefaultProjectTypesAsync(string unevaluatedPropertyValue, IProjectProperties defaultProperties)
        {
            if (bool.TryParse(unevaluatedPropertyValue, out bool value))
            {
                if (value)
                {
                    // Enabled: <MyType>WindowsForms</MyType>
                    await defaultProperties.SetPropertyValueAsync(ApplicationFrameworkMSBuildProperty, EnabledValue);
                }
                else
                {
                    // Disabled: <MyType>WindowsFormsWithCustomSubMain</MyType>
                    await defaultProperties.SetPropertyValueAsync(ApplicationFrameworkMSBuildProperty, DisabledValue);
                }
            }

            return null;
        }

        private async Task<string?> GetAppXamlRelativeFilePathAsync(bool create)
        {
            SpecialFileFlags flags = create ? SpecialFileFlags.CreateIfNotExist : 0;

            return await _project.GetSpecialFilePathAsync(SpecialFiles.AppXaml, flags);
        }

        private async Task<string?> SetPropertyValueForWPFApplicationAsync(string unevaluatedPropertyValue, IProjectProperties defaultProperties)
        {
            if (bool.TryParse(unevaluatedPropertyValue, out bool value))
            {
                if (value)
                {
                    // Enabled

                    // Create the Application.xaml if it doesn't exist. We don't care about the path, we just need it to be created.
                    string? appXamlFilePath = await GetAppXamlRelativeFilePathAsync(create: true);
                    if (appXamlFilePath is not null)
                    {
                        IEnumerable<IProjectItem> matchingItems = await _sourceItemsProvider.GetItemsAsync(NoneItemType, appXamlFilePath);
                        IProjectItem? appXamlItem = matchingItems.FirstOrDefault();
                        if (appXamlItem is not null)
                        {
                            await appXamlItem.SetItemTypeAsync(ApplicationDefinitionItemType);
                        }
                    }

                    // Clear out the StartupObject if it has a value.
                    string? startupObject = await defaultProperties.GetUnevaluatedPropertyValueAsync(StartupObjectMSBuildProperty);
                    if (!string.IsNullOrEmpty(startupObject))
                    {
                        await defaultProperties.DeletePropertyAsync(StartupObjectMSBuildProperty);
                    }
                }
                else
                {
                    // Disabled

                    // Set the StartupObject if it doesn't already have a value.
                    string? startupObject = await defaultProperties.GetUnevaluatedPropertyValueAsync(StartupObjectMSBuildProperty);
                    if (string.IsNullOrEmpty(startupObject))
                    {
                        await defaultProperties.SetPropertyValueAsync(StartupObjectMSBuildProperty, "Sub Main");
                    }

                    // Set the Application.xaml file's build action to None.
                    string? appXamlFilePath = await GetAppXamlRelativeFilePathAsync(create: false);
                    if (appXamlFilePath is not null)
                    {
                        IEnumerable<IProjectItem> matchingItems = await _sourceItemsProvider.GetItemsAsync(ApplicationDefinitionItemType, appXamlFilePath);
                        IProjectItem? appXamlItem = matchingItems.FirstOrDefault();
                        if (appXamlItem is not null)
                        {
                            await appXamlItem.SetItemTypeAsync(NoneItemType);
                        }
                    }
                }
            }

            return null;
        }
    }
}

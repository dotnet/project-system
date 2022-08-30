// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.VS;
using Microsoft.VisualStudio.ProjectSystem.VS.WindowsForms;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    [ExportInterceptingPropertyValueProvider(
    new[]
    {
        ApplicationFrameworkProperty,
        EnableVisualStylesProperty,
        SingleInstanceProperty,
        SaveMySettingsOnExitProperty,
        HighDpiModeProperty,
        AuthenticationModeProperty,
        ShutdownModeProperty,
        SplashScreenProperty,
        MinimumSplashScreenDisplayTimeProperty
    },
    ExportInterceptingPropertyValueProviderFile.ProjectFile)]
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

        internal const string ApplicationFrameworkProperty = "UseApplicationFramework";
        internal const string EnableVisualStylesProperty = "EnableVisualStyles";
        internal const string SingleInstanceProperty = "SingleInstance";
        internal const string SaveMySettingsOnExitProperty = "SaveMySettingsOnExit";
        internal const string HighDpiModeProperty = "HighDpiMode";
        internal const string AuthenticationModeProperty = "VBAuthenticationMode";
        internal const string ShutdownModeProperty = "ShutdownMode";
        internal const string SplashScreenProperty = "SplashScreen";
        internal const string MinimumSplashScreenDisplayTimeProperty = "MinimumSplashScreenDisplayTime";

        private readonly UnconfiguredProject _project;
        private readonly IProjectItemProvider _sourceItemsProvider;
        private readonly IMyAppFileAccessor _myAppXmlFileAccessor;

        [ImportingConstructor]
        public ApplicationFrameworkValueProvider(
            UnconfiguredProject project,
            [Import(ExportContractNames.ProjectItemProviders.SourceFiles)] IProjectItemProvider sourceItemsProvider,
            IMyAppFileAccessor myAppXamlFileAccessor)
        {
            _project = project;
            _sourceItemsProvider = sourceItemsProvider;
            _myAppXmlFileAccessor = myAppXamlFileAccessor;
        }

        public override async Task<string?> OnSetPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties, IReadOnlyDictionary<string, string>? dimensionalConditions = null)
        {
            if (propertyName == ApplicationFrameworkProperty)
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
            else
            {
                return await SetPropertyValueAsync(propertyName, unevaluatedPropertyValue, defaultProperties);
            }     
        }

        public override Task<string> OnGetEvaluatedPropertyValueAsync(string propertyName, string evaluatedPropertyValue, IProjectProperties defaultProperties)
        {
            if (propertyName == ApplicationFrameworkProperty)
            {
                return GetPropertyValueAsync(defaultProperties);
            }
            else
            {
                return GetPropertyValueAsync(propertyName);
            }
        }

        public override Task<string> OnGetUnevaluatedPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties)
        {
            if (propertyName == ApplicationFrameworkProperty)
            {
                return GetPropertyValueAsync(defaultProperties);
            }
            else
            {
                return GetPropertyValueAsync(propertyName);
            }
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

        private async Task<string?> SetPropertyValueForDefaultProjectTypesAsync(string unevaluatedPropertyValue, IProjectProperties defaultProperties)
        {
            string rootNameSpace = await defaultProperties.GetEvaluatedPropertyValueAsync("RootNamespace");
            
            if (bool.TryParse(unevaluatedPropertyValue, out bool value))
            {
                if (value)
                {
                    // Set in project file: <MyType>WindowsForms</MyType>
                    await defaultProperties.SetPropertyValueAsync(ApplicationFrameworkMSBuildProperty, EnabledValue);

                    // Set in myapp file: <MySubMain>true</MySubMain>
                    await _myAppXmlFileAccessor.SetMySubMainAsync("true");

                    // Set the StartupObject to namespace.My.MyApplication; we should save the actual value in the myapp file.
                    string? startupObjectValue = await defaultProperties.GetEvaluatedPropertyValueAsync(StartupObjectMSBuildProperty);

                    await defaultProperties.SetPropertyValueAsync(StartupObjectMSBuildProperty, rootNameSpace + ".My.MyApplication");

                    if (startupObjectValue is not null)
                    {
                        startupObjectValue.Replace(rootNameSpace + ".", "");
                        await _myAppXmlFileAccessor.SetMainFormAsync(startupObjectValue);
                    }
                }
                else
                {
                    // Set in project file: <MyType>WindowsFormsWithCustomSubMain</MyType>
                    await defaultProperties.SetPropertyValueAsync(ApplicationFrameworkMSBuildProperty, DisabledValue);

                    // Set in myapp file: <MySubMain>false</MySubMain>
                    await _myAppXmlFileAccessor.SetMySubMainAsync("false");

                    // Recover the StartupObject from myapp file and save it to the project file.
                    string? startupObjectValue = await _myAppXmlFileAccessor.GetMainFormAsync();

                    if (startupObjectValue is not null)
                        await defaultProperties.SetPropertyValueAsync(StartupObjectMSBuildProperty, rootNameSpace + "." + startupObjectValue);
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

        private async Task<string> GetPropertyValueAsync(string propertyName)
        {
            string value = propertyName switch
            {
                ApplicationFrameworkProperty => (await _myAppXmlFileAccessor.GetMySubMainAsync()).ToString() ?? string.Empty,
                EnableVisualStylesProperty => (await _myAppXmlFileAccessor.GetEnableVisualStylesAsync()).ToString() ?? string.Empty,
                SingleInstanceProperty => (await _myAppXmlFileAccessor.GetSingleInstanceAsync()).ToString() ?? string.Empty,
                SaveMySettingsOnExitProperty => (await _myAppXmlFileAccessor.GetSaveMySettingsOnExitAsync()).ToString() ?? string.Empty,
                HighDpiModeProperty => (await _myAppXmlFileAccessor.GetHighDpiModeAsync()).ToString() ?? string.Empty,
                AuthenticationModeProperty => (await _myAppXmlFileAccessor.GetAuthenticationModeAsync()).ToString() ?? string.Empty,
                ShutdownModeProperty => (await _myAppXmlFileAccessor.GetShutdownModeAsync()).ToString() ?? string.Empty,
                SplashScreenProperty => await _myAppXmlFileAccessor.GetSplashScreenAsync() ?? string.Empty,
                MinimumSplashScreenDisplayTimeProperty => (await _myAppXmlFileAccessor.GetMinimumSplashScreenDisplayTimeAsync()).ToString() ?? string.Empty,

                _ => throw new InvalidOperationException($"The provider does not support the '{propertyName}' property.")
            };

            if (propertyName == AuthenticationModeProperty)
            {
                value = value switch
                {
                    "0" => "Windows",
                    "1" => "ApplicationDefined",
                    "" => "",

                    _ => throw new InvalidOperationException($"Invalid value '{value}' for '{propertyName}' property.")
                };
            }
            else if (propertyName == ShutdownModeProperty)
            {
                value = value switch
                {
                    "0" => "AfterMainFormCloses",
                    "1" => "AfterAllFormsClose",
                    "" => "",

                    _ => throw new InvalidOperationException($"Invalid value '{value}' for '{propertyName}' property.")
                };
            }

            return value;
        }

        private async Task<string?> SetPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties)
        {
            // ValueProvider needs to convert string enums to valid values to be saved.
            if (propertyName == AuthenticationModeProperty)
            {
                unevaluatedPropertyValue = unevaluatedPropertyValue switch
                {
                    "Windows" => "0",
                    "ApplicationDefined" => "1",
                    _ => unevaluatedPropertyValue
                };
            }
            else if (propertyName == ShutdownModeProperty)
            {
                unevaluatedPropertyValue = unevaluatedPropertyValue switch
                {
                    "AfterMainFormCloses" => "0",
                    "AfterAllFormsClose" => "1",
                    _ => unevaluatedPropertyValue
                };
            }

            await (propertyName switch 
            {
                ApplicationFrameworkProperty => _myAppXmlFileAccessor.SetMySubMainAsync(unevaluatedPropertyValue),
                EnableVisualStylesProperty => _myAppXmlFileAccessor.SetEnableVisualStylesAsync(Convert.ToBoolean(unevaluatedPropertyValue)),
                SingleInstanceProperty => _myAppXmlFileAccessor.SetSingleInstanceAsync(Convert.ToBoolean(unevaluatedPropertyValue)),
                SaveMySettingsOnExitProperty => _myAppXmlFileAccessor.SetSaveMySettingsOnExitAsync(Convert.ToBoolean(unevaluatedPropertyValue)),
                HighDpiModeProperty => _myAppXmlFileAccessor.SetHighDpiModeAsync(Convert.ToInt16(unevaluatedPropertyValue)),
                AuthenticationModeProperty => _myAppXmlFileAccessor.SetAuthenticationModeAsync(Convert.ToInt16(unevaluatedPropertyValue)),
                ShutdownModeProperty => _myAppXmlFileAccessor.SetShutdownModeAsync(Convert.ToInt16(unevaluatedPropertyValue)),
                SplashScreenProperty => _myAppXmlFileAccessor.SetSplashScreenAsync(unevaluatedPropertyValue),
                MinimumSplashScreenDisplayTimeProperty => _myAppXmlFileAccessor.SetMinimumSplashScreenDisplayTimeAsync(Convert.ToInt16(unevaluatedPropertyValue)),

                _ => throw new InvalidOperationException($"The provider does not support the '{propertyName}' property.")
            });

            return null;
        }
    }
}

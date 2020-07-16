// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using VSLangProj;
using VSLangProj80;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties
{
    public abstract class AbstractProjectConfigurationProperties : ProjectConfigurationProperties3
    {
        private readonly ProjectProperties _projectProperties;
        private readonly IProjectThreadingService _threadingService;

        internal AbstractProjectConfigurationProperties(
            ProjectProperties projectProperties,
            IProjectThreadingService threadingService)
        {
            Requires.NotNull(projectProperties, nameof(projectProperties));
            Requires.NotNull(threadingService, nameof(threadingService));

            _projectProperties = projectProperties;
            _threadingService = threadingService;
        }

        public string LanguageVersion
        {
            get
            {
                return _threadingService.ExecuteSynchronously(async () =>
                {
                    ConfiguredBrowseObject browseObjectProperties = await _projectProperties.GetConfiguredBrowseObjectPropertiesAsync();
                    return await browseObjectProperties.LangVersion.GetEvaluatedValueAtEndAsync();
                });
            }

            set
            {
                _threadingService.ExecuteSynchronously(async () =>
                {
                    ConfiguredBrowseObject browseObjectProperties = await _projectProperties.GetConfiguredBrowseObjectPropertiesAsync();
                    await browseObjectProperties.LangVersion.SetValueAsync(value);
                });
            }
        }

        public string CodeAnalysisRuleSet
        {
            get
            {
                return _threadingService.ExecuteSynchronously(async () =>
                {
                    ConfiguredBrowseObject browseObjectProperties = await _projectProperties.GetConfiguredBrowseObjectPropertiesAsync();
                    return await browseObjectProperties.CodeAnalysisRuleSet.GetEvaluatedValueAtEndAsync();
                });
            }

            set
            {
                _threadingService.ExecuteSynchronously(async () =>
                {
                    ConfiguredBrowseObject browseObjectProperties = await _projectProperties.GetConfiguredBrowseObjectPropertiesAsync();
                    await browseObjectProperties.CodeAnalysisRuleSet.SetValueAsync(value);
                });
            }
        }

        public string OutputPath
        {
            get
            {
                return _threadingService.ExecuteSynchronously(async () =>
                {
                    ConfiguredBrowseObject browseObjectProperties = await _projectProperties.GetConfiguredBrowseObjectPropertiesAsync();
                    return await browseObjectProperties.OutputPath.GetEvaluatedValueAtEndAsync();
                });
            }

            set
            {
                _threadingService.ExecuteSynchronously(async () =>
                {
                    ConfiguredBrowseObject browseObjectProperties = await _projectProperties.GetConfiguredBrowseObjectPropertiesAsync();
                    await browseObjectProperties.OutputPath.SetValueAsync(value);
                });
            }
        }

        public string PlatformTarget
        {
            get
            {
                return _threadingService.ExecuteSynchronously(async () =>
                {
                    ConfiguredBrowseObject browseObjectProperties = await _projectProperties.GetConfiguredBrowseObjectPropertiesAsync();
                    return await browseObjectProperties.PlatformTarget.GetEvaluatedValueAtEndAsync();
                });
            }
            set
            {
                _threadingService.ExecuteSynchronously(async () =>
                {
                    ConfiguredBrowseObject browseObjectProperties = await _projectProperties.GetConfiguredBrowseObjectPropertiesAsync();
                    await browseObjectProperties.PlatformTarget.SetValueAsync(value);
                });
            }
        }

        public string IntermediatePath
        {
            get
            {
                return _threadingService.ExecuteSynchronously(async () =>
                {
                    ConfiguredBrowseObject browseObjectProperties = await _projectProperties.GetConfiguredBrowseObjectPropertiesAsync();
                    return await browseObjectProperties.IntermediatePath.GetEvaluatedValueAtEndAsync();
                });
            }
            set
            {
                _threadingService.ExecuteSynchronously(async () =>
                {
                    ConfiguredBrowseObject browseObjectProperties = await _projectProperties.GetConfiguredBrowseObjectPropertiesAsync();
                    await browseObjectProperties.IntermediatePath.SetValueAsync(value);
                });
            }
        }
        public bool RunCodeAnalysis
        {
            get
            {
                return _threadingService.ExecuteSynchronously(async () =>
                {
                    ConfiguredBrowseObject browseObjectProperties = await _projectProperties.GetConfiguredBrowseObjectPropertiesAsync();
                    object? value = await browseObjectProperties.RunCodeAnalysis.GetValueAsync();
                    return ((bool?)value).GetValueOrDefault();
                });
            }
            set
            {
                _threadingService.ExecuteSynchronously(async () =>
                {
                    ConfiguredBrowseObject browseObjectProperties = await _projectProperties.GetConfiguredBrowseObjectPropertiesAsync();
                    await browseObjectProperties.RunCodeAnalysis.SetValueAsync(value);
                });
            }
        }

        public object? ExtenderNames => null;
        public string __id => throw new System.NotImplementedException();
        public bool DebugSymbols { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public bool DefineDebug { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public bool DefineTrace { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public string DefineConstants { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public bool RemoveIntegerChecks { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public uint BaseAddress { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public bool AllowUnsafeBlocks { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public bool CheckForOverflowUnderflow { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public string DocumentationFile { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public bool Optimize { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public bool IncrementalBuild { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public string StartProgram { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public string StartWorkingDirectory { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public string StartURL { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public string StartPage { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public string StartArguments { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public bool StartWithIE { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public bool EnableASPDebugging { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public bool EnableASPXDebugging { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public bool EnableUnmanagedDebugging { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public prjStartAction StartAction { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public object get_Extender(string arg) => throw new System.NotImplementedException();
        public string ExtenderCATID => throw new System.NotImplementedException();
        public prjWarningLevel WarningLevel { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public bool TreatWarningsAsErrors { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public bool EnableSQLServerDebugging { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public uint FileAlignment { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public bool RegisterForComInterop { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public string ConfigurationOverrideFile { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public bool RemoteDebugEnabled { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public string RemoteDebugMachine { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public string NoWarn { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public bool NoStdLib { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public string DebugInfo
        {
            get
            {
                return _threadingService.ExecuteSynchronously(async () =>
                {
                    ConfiguredBrowseObject browseObjectProperties = await _projectProperties.GetConfiguredBrowseObjectPropertiesAsync();
                    return await browseObjectProperties.DebugInfo.GetEvaluatedValueAtEndAsync();
                });
            }
            set
            {
                _threadingService.ExecuteSynchronously(async () =>
                {
                    ConfiguredBrowseObject browseObjectProperties = await _projectProperties.GetConfiguredBrowseObjectPropertiesAsync();
                    await browseObjectProperties.DebugInfo.SetValueAsync(value);
                });
            }
        }
        public string TreatSpecificWarningsAsErrors { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public string CodeAnalysisLogFile { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public string CodeAnalysisRuleAssemblies { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public string CodeAnalysisInputAssembly { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public string CodeAnalysisRules { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public string CodeAnalysisSpellCheckLanguages { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public bool CodeAnalysisUseTypeNameInSuppression { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public string CodeAnalysisModuleSuppressionsFile { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public bool UseVSHostingProcess { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public sgenGenerationOption GenerateSerializationAssemblies { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public bool CodeAnalysisIgnoreGeneratedCode { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public bool CodeAnalysisOverrideRuleVisibilities { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public string CodeAnalysisDictionaries { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public string CodeAnalysisCulture { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public string CodeAnalysisRuleSetDirectories { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public bool CodeAnalysisIgnoreBuiltInRuleSets { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public string CodeAnalysisRuleDirectories { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public bool CodeAnalysisIgnoreBuiltInRules { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public bool CodeAnalysisFailOnMissingRules { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public bool Prefer32Bit { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
    }
}

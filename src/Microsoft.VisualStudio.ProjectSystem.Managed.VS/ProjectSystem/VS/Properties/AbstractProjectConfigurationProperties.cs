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
            Requires.NotNull(projectProperties);
            Requires.NotNull(threadingService);

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
        public string __id => throw new NotImplementedException();
        public bool DebugSymbols { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool DefineDebug { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool DefineTrace { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string DefineConstants { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool RemoveIntegerChecks { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public uint BaseAddress { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool AllowUnsafeBlocks { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool CheckForOverflowUnderflow { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string DocumentationFile { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool Optimize { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool IncrementalBuild { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string StartProgram { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string StartWorkingDirectory { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string StartURL { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string StartPage { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string StartArguments { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool StartWithIE { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool EnableASPDebugging { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool EnableASPXDebugging { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool EnableUnmanagedDebugging { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public prjStartAction StartAction { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public object get_Extender(string arg) => throw new NotImplementedException();
        public string ExtenderCATID => throw new NotImplementedException();
        public prjWarningLevel WarningLevel { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool TreatWarningsAsErrors { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool EnableSQLServerDebugging { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public uint FileAlignment { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool RegisterForComInterop { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string ConfigurationOverrideFile { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool RemoteDebugEnabled { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string RemoteDebugMachine { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string NoWarn { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool NoStdLib { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
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
        public string TreatSpecificWarningsAsErrors { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string CodeAnalysisLogFile { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string CodeAnalysisRuleAssemblies { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string CodeAnalysisInputAssembly { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string CodeAnalysisRules { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string CodeAnalysisSpellCheckLanguages { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool CodeAnalysisUseTypeNameInSuppression { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string CodeAnalysisModuleSuppressionsFile { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool UseVSHostingProcess { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public sgenGenerationOption GenerateSerializationAssemblies { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool CodeAnalysisIgnoreGeneratedCode { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool CodeAnalysisOverrideRuleVisibilities { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string CodeAnalysisDictionaries { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string CodeAnalysisCulture { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string CodeAnalysisRuleSetDirectories { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool CodeAnalysisIgnoreBuiltInRuleSets { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string CodeAnalysisRuleDirectories { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool CodeAnalysisIgnoreBuiltInRules { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool CodeAnalysisFailOnMissingRules { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool Prefer32Bit { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    }
}

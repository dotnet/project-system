// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Internal.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Settings;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    [Export(typeof(IProjectSystemOptions))]
    internal class ProjectSystemOptions : IProjectSystemOptions
    {
        private const string FastUpToDateEnabledSettingKey = @"ManagedProjectSystem\FastUpToDateCheckEnabled";
        private const string FastUpToDateLogLevelSettingKey = @"ManagedProjectSystem\FastUpToDateLogLevel";
        private const string UseDesignerByDefaultSettingKey = @"ManagedProjectSystem\UseDesignerByDefault";
        private const string PreferSingleTargetBuildsForStartupProjects = @"ManagedProjectSystem\PreferSingleTargetBuilds";

        // This setting exists as an option in Roslyn repo: 'FeatureOnOffOptions.SkipAnalyzersForImplicitlyTriggeredBuilds'.
        // Do not change this setting key unless the Roslyn option name is changed.
        internal const string SkipAnalyzersForImplicitlyTriggeredBuildSettingKey = "TextEditor.SkipAnalyzersForImplicitlyTriggeredBuilds";

        private readonly IVsService<ISettingsManager> _settingsManager;
        private readonly IVsService<SVsFeatureFlags, IVsFeatureFlags> _featureFlagsService;

        [ImportingConstructor]
        public ProjectSystemOptions(
            IVsService<SVsSettingsPersistenceManager, ISettingsManager> settingsManager,
            IVsService<SVsFeatureFlags, IVsFeatureFlags> featureFlagsService,
            IProjectThreadingService threadingService)
        {
            _settingsManager = settingsManager;
            _featureFlagsService = featureFlagsService;
        }

        public Task<bool> GetIsFastUpToDateCheckEnabledAsync(CancellationToken cancellationToken)
        {
            return GetSettingValueOrDefaultAsync(FastUpToDateEnabledSettingKey, defaultValue: true, cancellationToken);
        }

        public Task<LogLevel> GetFastUpToDateLoggingLevelAsync(CancellationToken cancellationToken)
        {
            return GetSettingValueOrDefaultAsync(FastUpToDateLogLevelSettingKey, defaultValue: LogLevel.None, cancellationToken);
        }

        public Task<bool> GetUseDesignerByDefaultAsync(string designerCategory, bool defaultValue, CancellationToken cancellationToken)
        {
            return GetSettingValueOrDefaultAsync(UseDesignerByDefaultSettingKey + "\\" + designerCategory, defaultValue, cancellationToken);
        }

        public Task SetUseDesignerByDefaultAsync(string designerCategory, bool value, CancellationToken cancellationToken)
        {
            return SetSettingValueAsync(UseDesignerByDefaultSettingKey + "\\" + designerCategory, value, cancellationToken);
        }

        public Task<bool> GetSkipAnalyzersForImplicitlyTriggeredBuildAsync(CancellationToken cancellationToken)
        {
            return GetSettingValueOrDefaultAsync(SkipAnalyzersForImplicitlyTriggeredBuildSettingKey, defaultValue: true, cancellationToken);
        }

        public Task<bool> GetPreferSingleTargetBuildsForStartupProjectsAsync(CancellationToken cancellationToken)
        {
            return GetSettingValueOrDefaultAsync(PreferSingleTargetBuildsForStartupProjects, defaultValue: true, cancellationToken);
        }

        private async Task<T> GetSettingValueOrDefaultAsync<T>(string name, T defaultValue, CancellationToken cancellationToken)
        {
            ISettingsManager settingsManager = await _settingsManager.GetValueAsync(cancellationToken);

            return settingsManager.GetValueOrDefault(name, defaultValue);
        }

        private async Task SetSettingValueAsync(string name, object value, CancellationToken cancellationToken)
        {
            ISettingsManager settingsManager = await _settingsManager.GetValueAsync(cancellationToken);

            await settingsManager.SetValueAsync(name, value, isMachineLocal: false);
        }

        public ValueTask<bool> IsIncrementalBuildFailureOutputLoggingEnabledAsync(CancellationToken cancellationToken)
        {
            return IsFlagEnabledAsync(FeatureFlagNames.EnableIncrementalBuildFailureOutputLogging, defaultValue: false, cancellationToken);
        }

        public ValueTask<bool> IsIncrementalBuildFailureTelemetryEnabledAsync(CancellationToken cancellationToken)
        {
            return IsFlagEnabledAsync(FeatureFlagNames.EnableIncrementalBuildFailureTelemetry, defaultValue: false, cancellationToken);
        }

        public ValueTask<bool> IsBuildAccelerationEnabledByDefaultAsync(CancellationToken cancellationToken)
        {
            return IsFlagEnabledAsync(FeatureFlagNames.EnableBuildAccelerationByDefault, defaultValue: false, cancellationToken);
        }

        public ValueTask<bool> IsLspPullDiagnosticsEnabledAsync(CancellationToken cancellationToken)
        {
            return IsFlagEnabledAsync(FeatureFlagNames.LspPullDiagnosticsFeatureFlagName, defaultValue: false, cancellationToken);
        }

        private async ValueTask<bool> IsFlagEnabledAsync(string featureName, bool defaultValue, CancellationToken cancellationToken)
        {
            IVsFeatureFlags featureFlags = await _featureFlagsService.GetValueAsync(cancellationToken);

            return featureFlags.IsFeatureEnabled(featureName, defaultValue);
        }
    }
}

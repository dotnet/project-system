// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Setup.Configuration;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    [Export(typeof(IPreviewSDKService))]
    internal class PreviewSDKService : IPreviewSDKService
    {
        private readonly IVsService<SVsSettingsPersistenceManager, ISettingsManager> _settingsManager;
        private const string UsePreviewSdkSettingKey = @"ManagedProjectSystem\UsePreviewSdk";

        [ImportingConstructor]
        public PreviewSDKService(IVsService<SVsSettingsPersistenceManager, ISettingsManager> settingsManager)
        {
            _settingsManager = settingsManager;
        }

        public async Task<bool> IsPreviewSDKInUseAsync()
        {
            var vsSetupConfig = new SetupConfiguration();
            ISetupInstance setupInstance = vsSetupConfig.GetInstanceForCurrentProcess();
            if (setupInstance is ISetupInstanceCatalog setupInstanceCatalog &&
                setupInstanceCatalog.IsPrerelease())
            {
                return true;
            }

            var settings = await _settingsManager?.GetValueAsync();
            return settings.GetValueOrDefault<bool>(UsePreviewSdkSettingKey);
        }
    }
}

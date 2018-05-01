using System;
using System.ComponentModel.Composition;

using Microsoft.VisualStudio.ProjectSystem.VS.Utilities;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Generators
{
    [ExportProjectNodeComService(typeof(IVsSingleFileGeneratorFactory))]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasicOrFSharp)]
    internal class SingleFileGeneratorFactoryAggregator : IVsSingleFileGeneratorFactory
    {
        // Constants for the generator information registry keys
        private const string CLSIDKey = "CLSID";
        private const string DesignTimeSourceKey = "GeneratesDesignTimeSource";
        private const string SharedDesignTimeSourceKey = "GeneratesSharedDesignTimeSource";
        private const string DesignTimeCompilationFlagKey = "UseDesignTimeCompilationFlag";

        private readonly IServiceProvider _serviceProvider;
        private readonly IVsUnconfiguredProjectIntegrationService _projectIntegrationService;

        [ImportingConstructor]
        public SingleFileGeneratorFactoryAggregator(
            [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider,
            IVsUnconfiguredProjectIntegrationService projectIntegrationService)
        {
            _serviceProvider = serviceProvider;
            _projectIntegrationService = projectIntegrationService;
        }

        public int CreateGeneratorInstance(string wszProgId, out int pbGeneratesDesignTimeSource, out int pbGeneratesSharedDesignTimeSource, out int pbUseTempPEFlag, out IVsSingleFileGenerator ppGenerate)
        {
            // The only user in the project system does not call this method.
            pbGeneratesDesignTimeSource = 0;
            pbGeneratesSharedDesignTimeSource = 0;
            pbUseTempPEFlag = 0;
            ppGenerate = null;
            return VSConstants.E_NOTIMPL;
        }

        public int GetDefaultGenerator(string wszFilename, out string pbstrGenProgID)
        {
            // The only user in the project system does not call this method.
            pbstrGenProgID = null;
            return VSConstants.E_NOTIMPL;
        }

        public int GetGeneratorInformation(string wszProgId, out int pbGeneratesDesignTimeSource, out int pbGeneratesSharedDesignTimeSource, out int pbUseTempPEFlag, out Guid pguidGenerator)
        {
            pbGeneratesDesignTimeSource = 0;
            pbGeneratesSharedDesignTimeSource = 0;
            pbUseTempPEFlag = 0;
            pguidGenerator = Guid.Empty;

            if (wszProgId == null || string.IsNullOrWhiteSpace(wszProgId))
            {
                return VSConstants.E_INVALIDARG;
            }

            // Get the guid of the project
            UIThreadHelper.VerifyOnUIThread();
            Guid projectGuid = _projectIntegrationService.ProjectTypeGuid;

            if (projectGuid.Equals(Guid.Empty))
            {
                return VSConstants.E_FAIL;
            }

            IVsSettingsManager manager = _serviceProvider.GetService<IVsSettingsManager, SVsSettingsManager>();
            HResult hr = manager.GetReadOnlySettingsStore((uint)__VsSettingsScope.SettingsScope_Configuration, out IVsSettingsStore store);
            if (!hr.Succeeded)
            {
                return hr;
            }

            string key = $"Generators\\{projectGuid.ToString("B")}\\{wszProgId}";
            hr = store.CollectionExists(key, out int exists);
            if (!hr.Succeeded)
            {
                return hr;
            }

            if (exists != 1)
            {
                return VSConstants.E_FAIL;
            }

            // The clsid value is the only required value. The other 3 are optional
            hr = store.PropertyExists(key, CLSIDKey, out exists);
            if (!hr.Succeeded)
            {
                return hr;
            }
            if (exists != 1)
            {
                return VSConstants.E_FAIL;
            }

            hr = store.GetString(key, CLSIDKey, out string clsidString);
            if (hr.Failed)
            {
                return hr;
            }
            if (string.IsNullOrWhiteSpace(clsidString) || !Guid.TryParse(clsidString, out pguidGenerator))
            {
                return VSConstants.E_FAIL;
            }

            // Explicitly convert anything that's not 1 to 0. These aren't required keys, so we don't explicitly fail here.
            store.GetIntOrDefault(key, DesignTimeSourceKey, 0, out pbGeneratesDesignTimeSource);
            pbGeneratesDesignTimeSource = pbGeneratesDesignTimeSource == 1 ? 1 : 0;
            store.GetIntOrDefault(key, SharedDesignTimeSourceKey, 0, out pbGeneratesSharedDesignTimeSource);
            pbGeneratesSharedDesignTimeSource = pbGeneratesSharedDesignTimeSource == 1 ? 1 : 0;
            store.GetIntOrDefault(key, DesignTimeCompilationFlagKey, 0, out pbUseTempPEFlag);
            pbUseTempPEFlag = pbUseTempPEFlag == 1 ? 1 : 0;

            return VSConstants.S_OK;
        }
    }
}

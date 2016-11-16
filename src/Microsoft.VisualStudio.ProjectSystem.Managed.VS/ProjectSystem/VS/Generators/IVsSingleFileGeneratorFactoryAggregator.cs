using Microsoft.VisualStudio.ProjectSystem.VS.Utilities;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Linq;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Generators
{
    internal abstract class IVsSingleFileGeneratorFactoryAggregator : OnceInitializedOnceDisposed, IVsSingleFileGeneratorFactory
    {
        // Constants for the generator information registry keys
        private const string CLSIDKey = "CLSID";
        private const string DesignTimeSourceKey = "GeneratesDesignTimeSource";
        private const string SharedDesignTimeSourceKey = "GeneratesSharedDesignTimeSource";
        private const string DesignTimeCompilationFlagKey = "UseDesignTimeCompilationFlag";

        private readonly IServiceProvider _serviceProvider;
        private readonly IVSRegistryHelper _registryHelper;
        private IRegistryKey _settingsRoot;

        public IVsSingleFileGeneratorFactoryAggregator(IServiceProvider serviceProvider, IVSRegistryHelper registryHelper)
        {
            _serviceProvider = serviceProvider;
            _registryHelper = registryHelper;
        }

        public int CreateGeneratorInstance(string wszProgId, out int pbGeneratesDesignTimeSource, out int pbGeneratesSharedDesignTimeSource, out int pbUseTempPEFlag, out IVsSingleFileGenerator ppGenerate)
        {
            // The only user in the project system does not call this method.
            throw new NotImplementedException();
        }

        public int GetDefaultGenerator(string wszFilename, out string pbstrGenProgID)
        {
            // The only user in the project system does not call this method.
            throw new NotImplementedException();
        }

        public int GetGeneratorInformation(string wszProgId, out int pbGeneratesDesignTimeSource, out int pbGeneratesSharedDesignTimeSource, out int pbUseTempPEFlag, out Guid pguidGenerator)
        {
            Requires.NotNullOrEmpty(wszProgId, nameof(wszProgId));
            EnsureInitialized(true);

            pbGeneratesDesignTimeSource = 0;
            pbGeneratesSharedDesignTimeSource = 0;
            pbUseTempPEFlag = 0;
            pguidGenerator = Guid.Empty;

            var generatorKey = GetGeneratorKey(PackageGuid);

            if (!generatorKey.GetSubKeyNames().Contains(wszProgId))
            {
                return VSConstants.E_FAIL;
            }

            var progKey = generatorKey.OpenSubKey(wszProgId, false);

            // The clsid value is the only required value. The other 3 are optional
            if (!progKey.GetValueNames().Contains(CLSIDKey))
            {
                return VSConstants.E_FAIL;
            }

            pguidGenerator = Guid.Parse(progKey.GetValue<string>(CLSIDKey));

            // Explicitly convert anything that's not 1 to 0
            pbGeneratesDesignTimeSource = progKey.GetValue(DesignTimeSourceKey, 0) == 1 ? 1 : 0;
            pbGeneratesSharedDesignTimeSource = progKey.GetValue(SharedDesignTimeSourceKey, 0) == 1 ? 1 : 0;
            pbUseTempPEFlag = progKey.GetValue(DesignTimeCompilationFlagKey, 0) == 1 ? 1 : 0;

            return VSConstants.S_OK;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _settingsRoot.Dispose();
                _settingsRoot = null;
            }
        }

        protected override void Initialize()
        {
            _settingsRoot = _registryHelper.RegistryRoot(_serviceProvider, __VsLocalRegistryType.RegType_Configuration, false);
        }

        private IRegistryKey GetGeneratorKey(Guid package)
        {
            var generatorKey = _settingsRoot.OpenSubKey("Generators", false);
            return generatorKey.OpenSubKey(package.ToString("B").ToUpper(), false);
        }

        protected abstract Guid PackageGuid { get; }
    }
}

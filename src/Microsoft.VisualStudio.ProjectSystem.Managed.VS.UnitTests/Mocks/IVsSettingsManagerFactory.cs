using System;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.Shell.Interop
{
    internal class IVsSettingsManagerFactory : IVsSettingsManager
    {
        public IDictionary<uint, IVsSettingsStore> Stores { get; set; } = new Dictionary<uint, IVsSettingsStore>();

        public int GetApplicationDataFolder(uint folder, out string folderPath)
        {
            throw new NotImplementedException();
        }

        public int GetCollectionScopes(string collectionPath, out uint scopes)
        {
            throw new NotImplementedException();
        }

        public int GetCommonExtensionsSearchPaths(uint paths, string[] commonExtensionsPaths, out uint actualPaths)
        {
            throw new NotImplementedException();
        }

        public int GetPropertyScopes(string collectionPath, string propertyName, out uint scopes)
        {
            throw new NotImplementedException();
        }

        public int GetReadOnlySettingsStore(uint scope, out IVsSettingsStore store)
        {
            store = Stores[scope];
            return Stores.ContainsKey(scope) ? VSConstants.S_OK : VSConstants.E_FAIL;
        }

        public int GetWritableSettingsStore(uint scope, out IVsWritableSettingsStore writableStore)
        {
            throw new NotImplementedException();
        }
    }
}

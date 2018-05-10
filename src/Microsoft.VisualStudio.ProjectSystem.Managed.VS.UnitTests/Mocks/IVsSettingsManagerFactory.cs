using System;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.Shell.Interop
{
    internal class IVsSettingsManagerFactory
    {
        public static IVsSettingsManager Create()
        {
            return Create("");
        }

        public static IVsSettingsManager Create(string path, IDictionary<string, object> vals = null)
        {
            var store = new IVsSettingsStoreTester
            {
                Keys = new Dictionary<string, IDictionary<string, object>>(StringComparer.OrdinalIgnoreCase)
                {
                    { path, vals ?? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase) }
                }
            };
            return new VsSettingsManger
            {
                Stores = new Dictionary<uint, IVsSettingsStore>
                {
                    { (uint)__VsSettingsScope.SettingsScope_Configuration, store }
                }
            };
        }

        private class VsSettingsManger : IVsSettingsManager
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
}

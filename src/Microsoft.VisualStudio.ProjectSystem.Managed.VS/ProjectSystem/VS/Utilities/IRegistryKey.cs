using Microsoft.Win32;
using System;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Utilities
{
    internal interface IRegistryKey : IDisposable
    {
        IRegistryKey OpenSubKey(string key, bool writable);
        string[] GetSubKeyNames();
        string[] GetValueNames();
        T GetValue<T>(string key) where T : class;
        T GetValue<T>(string key, T defaultValue);
    }

    internal class RegistryKeyWrapper : IRegistryKey
    {
        private readonly RegistryKey _wrappedKey;

        public RegistryKeyWrapper(RegistryKey wrappedKey)
        {
            _wrappedKey = wrappedKey;
        }

        public IRegistryKey OpenSubKey(string key, bool writable)
        {
            return new RegistryKeyWrapper(_wrappedKey.OpenSubKey(key, writable));
        }

        public string[] GetSubKeyNames()
        {
            return _wrappedKey.GetSubKeyNames();
        }

        public string[] GetValueNames()
        {
            return _wrappedKey.GetValueNames();
        }

        public T GetValue<T>(string key) where T : class
        {
            return (T)_wrappedKey.GetValue(key);
        }

        public T GetValue<T>(string key, T defaultValue)
        {
            return (T)_wrappedKey.GetValue(key, defaultValue);
        }

        public void Dispose()
        {
            _wrappedKey.Dispose();
        }
    }
}

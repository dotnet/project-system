using Microsoft.Win32;
using System;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Utilities
{
    internal interface IRegistryKey : IDisposable
    {
        IRegistryKey OpenSubKey(string key, bool writable);
        string[] GetSubKeyNames();
        string[] GetValueNames();
        object GetValue(string key);
        object GetValue(string key, object defaultValue);
    }

    internal static class IRegistryKeyExtensions
    {
        public static T GetValue<T>(this IRegistryKey reg, string key)
        {
            return (T)reg.GetValue(key);
        }

        public static T GetValue<T>(this IRegistryKey reg, string key, T defaultValue)
        {
            return (T)reg.GetValue(key, defaultValue);
        }
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

        public object GetValue(string key)
        {
            return _wrappedKey.GetValue(key);
        }

        public object GetValue(string key, object defaultValue)
        {
            return _wrappedKey.GetValue(key, defaultValue);
        }

        public void Dispose()
        {
            _wrappedKey.Dispose();
        }
    }
}

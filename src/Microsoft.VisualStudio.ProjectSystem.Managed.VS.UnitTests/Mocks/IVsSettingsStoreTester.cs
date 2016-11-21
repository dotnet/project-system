using System;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.Shell.Interop
{
    internal class IVsSettingsStoreTester : IVsSettingsStore
    {
        public IDictionary<string, IDictionary<string, object>> Keys { get; set; } = new Dictionary<string, IDictionary<string, object>>();

        public int CollectionExists(string collectionPath, out int pfExists)
        {
            pfExists = Keys.ContainsKey(collectionPath) ? 1 : 0;
            return VSConstants.S_OK;
        }

        public int GetBinary(string collectionPath, string propertyName, uint byteLength, byte[] pBytes, uint[] actualByteLength)
        {
            throw new NotImplementedException();
        }

        public int GetBool(string collectionPath, string propertyName, out int value)
        {
            bool val;
            if (VSConstants.S_OK != GetWithDefault(collectionPath, propertyName, false, out val))
            {
                value = 0;
                return VSConstants.E_FAIL;
            }
            else
            {
                value = val ? 1 : 0;
                return VSConstants.S_OK;
            }
        }


        public int GetBoolOrDefault(string collectionPath, string propertyName, int defaultValue, out int value)
        {
            GetWithDefault(collectionPath, propertyName, 0, out value);
            bool val;
            value = VSConstants.S_OK != GetWithDefault(collectionPath, propertyName, false, out val) ? defaultValue : (val ? 1 : 0);
            return VSConstants.S_OK;
        }

        public int GetInt(string collectionPath, string propertyName, out int value)
        {
            return GetWithDefault(collectionPath, propertyName, 0, out value);
        }

        public int GetInt64(string collectionPath, string propertyName, out long value)
        {
            return GetWithDefault(collectionPath, propertyName, 0, out value);
        }

        public int GetInt64OrDefault(string collectionPath, string propertyName, long defaultValue, out long value)
        {
            GetWithDefault(collectionPath, propertyName, 0, out value);
            return VSConstants.S_OK;
        }

        public int GetIntOrDefault(string collectionPath, string propertyName, int defaultValue, out int value)
        {
            GetWithDefault(collectionPath, propertyName, 0, out value);
            return VSConstants.S_OK;
        }

        public int GetLastWriteTime(string collectionPath, SYSTEMTIME[] lastWriteTime)
        {
            throw new NotImplementedException();
        }

        public int GetPropertyCount(string collectionPath, out uint propertyCount)
        {
            throw new NotImplementedException();
        }

        public int GetPropertyName(string collectionPath, uint index, out string propertyName)
        {
            throw new NotImplementedException();
        }

        public int GetPropertyType(string collectionPath, string propertyName, out uint type)
        {
            throw new NotImplementedException();
        }

        public int GetString(string collectionPath, string propertyName, out string value)
        {
            return GetWithDefault(collectionPath, propertyName, null, out value);
        }

        public int GetStringOrDefault(string collectionPath, string propertyName, string defaultValue, out string value)
        {
            GetWithDefault(collectionPath, propertyName, defaultValue, out value);
            return VSConstants.S_OK;
        }

        public int GetSubCollectionCount(string collectionPath, out uint subCollectionCount)
        {
            throw new NotImplementedException();
        }

        public int GetSubCollectionName(string collectionPath, uint index, out string subCollectionName)
        {
            throw new NotImplementedException();
        }

        public int GetUnsignedInt(string collectionPath, string propertyName, out uint value)
        {
            return GetWithDefault<uint>(collectionPath, propertyName, 0, out value);
        }

        public int GetUnsignedInt64(string collectionPath, string propertyName, out ulong value)
        {
            return GetWithDefault<ulong>(collectionPath, propertyName, 0, out value);
        }

        public int GetUnsignedInt64OrDefault(string collectionPath, string propertyName, ulong defaultValue, out ulong value)
        {
            GetWithDefault(collectionPath, propertyName, defaultValue, out value);
            return VSConstants.S_OK;
        }

        public int GetUnsignedIntOrDefault(string collectionPath, string propertyName, uint defaultValue, out uint value)
        {
            GetWithDefault(collectionPath, propertyName, defaultValue, out value);
            return VSConstants.S_OK;
        }

        public int PropertyExists(string collectionPath, string propertyName, out int pfExists)
        {
            pfExists = Keys.ContainsKey(collectionPath) && Keys[collectionPath].ContainsKey(propertyName) ? 1 : 0;
            return VSConstants.S_OK;
        }

        private int GetWithDefault<T>(string collectionPath, string propertyName, T defaultVal, out T result)
        {
            result = defaultVal;
            if (!Keys.ContainsKey(collectionPath))
            {
                return VSConstants.E_FAIL;
            }

            var vals = Keys[collectionPath];
            if (!vals.ContainsKey(propertyName))
            {
                return VSConstants.E_FAIL;
            }

            result = (T)vals[propertyName];
            return VSConstants.S_OK;
        }
    }
}

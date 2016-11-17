using Microsoft.VisualStudio.Shell.Interop;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Utilities
{
    /// <summary>
    /// Builds happen by having the node that was called work its way back up to the IVSRegistryHelperBuilder that is the root, having it call
    /// the internal build method that actually does a build for all nodes, and then returning that back up the chain.
    /// </summary>
    internal interface IBuildableRegistry
    {
        IVSRegistryHelper Build();
    }

    internal class IVSRegistryHelperBuilder : IBuildableRegistry
    {
        private readonly IDictionary<__VsLocalRegistryType, (IRegistryKeyBuilder builder, bool writable)> _hives =
            new Dictionary<__VsLocalRegistryType, (IRegistryKeyBuilder, bool)>();

        public IRegistryKeyBuilder CreateHive(__VsLocalRegistryType type, bool writable)
        {
            if (_hives.ContainsKey(type)) throw new InvalidOperationException($"Already created registry hive for {type}");
            var builder = new IRegistryKeyBuilder(this);
            _hives[type] = (builder, writable);
            return builder;
        }

        public IVSRegistryHelper Build()
        {
            var mock = new Mock<IVSRegistryHelper>();
            foreach (var hive in _hives)
            {
                mock.Setup(h => h.RegistryRoot(It.IsAny<IServiceProvider>(), hive.Key, hive.Value.writable)).Returns(hive.Value.builder.BuildInternal());
            }

            return mock.Object;
        }
    }

    internal class IRegistryKeyBuilder : IBuildableRegistry
    {
        private readonly IBuildableRegistry _parent;
        private readonly IDictionary<string, Func<string, object, object>> _values = new Dictionary<string, Func<string, object, object>>();
        private readonly IDictionary<string, (IRegistryKeyBuilder key, bool writable)> _subKeys = new Dictionary<string, (IRegistryKeyBuilder, bool)>();

        internal IRegistryKeyBuilder(IBuildableRegistry parent)
        {
            Requires.NotNull(parent, nameof(parent));
            _parent = parent;
        }

        public IRegistryKeyBuilder SetValue(string key, Func<string, object, object> value)
        {
            if (_values.ContainsKey(key)) throw new InvalidOperationException($"Key {key} already exists!");
            _values[key] = value;
            return this;
        }

        public IRegistryKeyBuilder SetValue(string key, object value)
        {
            return SetValue(key, (ignored1, ignored2) => value);
        }

        public IRegistryKeyBuilder CreateSubkey(string key, bool writable)
        {
            if (_subKeys.ContainsKey(key)) throw new InvalidOperationException($"Subkey {key} already exists");
            var subKey = new IRegistryKeyBuilder(this);
            _subKeys[key] = (subKey, writable);
            return subKey;
        }

        public IVSRegistryHelper Build() => _parent.Build();

        /// <summary>
        /// Not intended for use outside of <see cref="IVSRegistryHelperBuilder"/>
        /// </summary>
        internal IRegistryKey BuildInternal()
        {
            var mock = new Mock<IRegistryKey>();
            foreach (var value in _values)
            {
                mock.Setup(r => r.GetValue(value.Key)).Returns<string>(arg => value.Value(arg, null));
                mock.Setup(r => r.GetValue(value.Key, It.IsAny<object>())).Returns(value.Value);
            }

            // If we're not mocking it, GetValue with default should return the default
            mock.Setup(r => r.GetValue(It.IsNotIn<string>(_values.Keys), It.IsAny<object>())).Returns<string, object>((arg, def) => def);

            mock.Setup(r => r.GetValueNames()).Returns(_values.Keys.ToArray());

            foreach(var subKey in _subKeys)
            {
                mock.Setup(r => r.OpenSubKey(subKey.Key, subKey.Value.writable)).Returns(subKey.Value.key.BuildInternal());
            }
            mock.Setup(r => r.GetSubKeyNames()).Returns(_subKeys.Keys.ToArray());

            return mock.Object;
        }
    }
}

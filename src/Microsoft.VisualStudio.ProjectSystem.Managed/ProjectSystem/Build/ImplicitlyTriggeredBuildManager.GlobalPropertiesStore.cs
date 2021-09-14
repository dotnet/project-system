// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Immutable;

namespace Microsoft.VisualStudio.ProjectSystem.Build
{
    internal sealed partial class ImplicitlyTriggeredBuildManager
    {
        /// <summary>
        /// Global properties store for implicitly triggered builds from commands such as Run/Debug Tests, Start Debugging, etc.
        /// </summary>
        private sealed class GlobalPropertiesStore
        {
            private const string IsImplicitlyTriggeredBuildPropertyName = "IsImplicitlyTriggeredBuild";
            private const string FastUpToDateCheckIgnoresKindsGlobalPropertyName = "FastUpToDateCheckIgnoresKinds";
            private const string FastUpToDateCheckIgnoresKindsGlobalPropertyValue = "ImplicitBuild";

            private readonly ImmutableDictionary<string, string>.Builder _properties = ImmutableDictionary.CreateBuilder<string, string>(StringComparer.OrdinalIgnoreCase);
            private readonly object _gate = new();

            public static readonly GlobalPropertiesStore Instance = new();

            private GlobalPropertiesStore()
            {
            }

            internal ImmutableDictionary<string, string> GetProperties()
            {
                lock (_gate)
                {
                    return _properties.ToImmutable();
                }
            }

            public void OnBuildStart()
            {
                SetProperty(IsImplicitlyTriggeredBuildPropertyName, "true");
                SetProperty(FastUpToDateCheckIgnoresKindsGlobalPropertyName, FastUpToDateCheckIgnoresKindsGlobalPropertyValue);
            }

            public void OnBuildEndOrCancel()
            {
                RemoveProperty(IsImplicitlyTriggeredBuildPropertyName);
                RemoveProperty(FastUpToDateCheckIgnoresKindsGlobalPropertyName);
            }

            private void SetProperty(string key, string value)
            {
                lock (_gate)
                {
                    _properties[key] = value;
                }
            }

            private void RemoveProperty(string key)
            {
                lock (_gate)
                {
                    _properties.Remove(key);
                }
            }
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Managed.Build
{
    internal sealed partial class SkipAnalyzersGlobalPropertiesProvider
    {
        /// <summary>
        /// Global properties store for implicitly triggered builds from commands such as Run/Debug Tests, Start Debugging, etc.
        /// </summary>
        private sealed class GlobalPropertiesStore
        {
            private const string IsImplicitlyTriggeredBuildPropertyName = "IsImplicitlyTriggeredBuild";
            private const string FastUpToDateCheckIgnoresKindsGlobalPropertyName = "FastUpToDateCheckIgnoresKinds";
            private const string FastUpToDateCheckIgnoresKindsGlobalPropertyValue = "ImplicitBuild";

            private readonly ImmutableDictionary<string, string> _regularBuildProperties;
            private readonly ImmutableDictionary<string, string> _implicitTriggeredBuildProperties;

            public static readonly GlobalPropertiesStore Instance = new();

            private GlobalPropertiesStore()
            {
                _regularBuildProperties = ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase);

                _implicitTriggeredBuildProperties = _regularBuildProperties
                    .Add(IsImplicitlyTriggeredBuildPropertyName, "true")
                    .Add(FastUpToDateCheckIgnoresKindsGlobalPropertyName, FastUpToDateCheckIgnoresKindsGlobalPropertyValue);
            }

            internal ImmutableDictionary<string, string> GetRegularBuildProperties()
                => _regularBuildProperties;

            internal ImmutableDictionary<string, string> GetImplicitlyTriggeredBuildProperties()
                => _implicitTriggeredBuildProperties;
        }
    }
}

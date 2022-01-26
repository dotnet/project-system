// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Build;

namespace Microsoft.VisualStudio.ProjectSystem.Managed.Build
{
    internal sealed partial class ImplicitlyTriggeredBuildManager
    {
        /// <summary>
        /// Global properties provider for implicitly triggered builds from commands such as Run/Debug Tests, Start Debugging, etc.
        /// This provider does not affect the property collection for design time builds.
        /// </summary>
        /// <remarks>
        /// Currently, the provider is only for CPS based SDK-style projects, not for legacy csproj projects.
        /// https://github.com/dotnet/project-system/issues/7346 tracks implementing the project system support for legacy csproj projects.
        /// </remarks>
        [ExportBuildGlobalPropertiesProvider]
        [AppliesTo(ProjectCapability.DotNet)]
        private sealed class GlobalProviderProvider : StaticGlobalPropertiesProviderBase
        {
            private readonly IImplicitlyTriggeredBuildState _implicitlyTriggeredBuildState;

            /// <summary>
            /// Initializes a new instance of the <see cref="GlobalProviderProvider"/> class.
            /// </summary>
            [ImportingConstructor]
            public GlobalProviderProvider(UnconfiguredProject unconfiguredProject,
                IImplicitlyTriggeredBuildState implicitlyTriggeredBuildState)
                : base(unconfiguredProject.Services)
            {
                _implicitlyTriggeredBuildState = implicitlyTriggeredBuildState;
            }

            /// <summary>
            /// Gets the set of global properties that should apply to the project(s) in this scope.
            /// </summary>
            /// <value>A new dictionary whose keys are case insensitive.  Never null, but may be empty.</value>
            public override Task<IImmutableDictionary<string, string>> GetGlobalPropertiesAsync(CancellationToken cancellationToken)
            {
                ImmutableDictionary<string, string> globalProperties = _implicitlyTriggeredBuildState.IsImplicitlyTriggeredBuild
                    ? GlobalPropertiesStore.Instance.GetImplicitlyTriggeredBuildProperties()
                    : GlobalPropertiesStore.Instance.GetRegularBuildProperties();

                return Task.FromResult<IImmutableDictionary<string, string>>(globalProperties);
            }
        }
    }
}

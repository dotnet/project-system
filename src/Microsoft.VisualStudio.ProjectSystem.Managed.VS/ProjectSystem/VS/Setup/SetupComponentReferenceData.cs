// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Setup;

/// <summary>
/// Static data about VS Setup components.
/// </summary>
internal static class SetupComponentReferenceData
{
    public const string WebComponentId = "Microsoft.VisualStudio.Component.Web";

    private static readonly ImmutableDictionary<string, string> s_componentIdByRuntimeVersion = ImmutableStringDictionary<string>.EmptyOrdinalIgnoreCase
        .Add("v2.0", "Microsoft.Net.Core.Component.SDK.2.1")
        .Add("v2.1", "Microsoft.Net.Core.Component.SDK.2.1")
        .Add("v2.2", "Microsoft.Net.Core.Component.SDK.2.1")
        .Add("v3.0", "Microsoft.NetCore.Component.Runtime.3.1")
        .Add("v3.1", "Microsoft.NetCore.Component.Runtime.3.1")
        .Add("v5.0", "Microsoft.NetCore.Component.Runtime.5.0")
        .Add("v6.0", "Microsoft.NetCore.Component.Runtime.6.0")
        .Add("v7.0", "Microsoft.NetCore.Component.Runtime.7.0")
        .Add("v8.0", "Microsoft.NetCore.Component.Runtime.8.0");

    /// <summary>
    /// Attempts to map a .NET Core <c>TargetFrameworkVersion</c> to the corresponding VS Setup component ID for that version's runtime.
    /// </summary>
    public static bool TryGetComponentIdByNetCoreTargetFrameworkVersion(string netCoreTargetFrameworkVersion, [NotNullWhen(returnValue: true)] out string? runtimeComponentId)
    {
        return s_componentIdByRuntimeVersion.TryGetValue(netCoreTargetFrameworkVersion, out runtimeComponentId);
    }

    /// <summary>
    /// Determines whether a given setup component ID represents a .NET Core runtime.
    /// </summary>
    /// <remarks>
    /// This uses a prefix-based heuristic that might not work on future versions of Visual Studio.
    /// </remarks>
    public static bool IsRuntimeComponentId(string componentId)
    {
        return componentId.StartsWith("Microsoft.NetCore.Component.Runtime.", StringComparisons.VisualStudioSetupComponentIds)
            || componentId.StartsWith("Microsoft.Net.Core.Component.SDK.", StringComparisons.VisualStudioSetupComponentIds);
    }
}

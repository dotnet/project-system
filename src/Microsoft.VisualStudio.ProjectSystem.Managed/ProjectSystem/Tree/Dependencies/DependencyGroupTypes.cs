// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies;

/// <summary>
/// Singleton <see cref="DependencyGroupType"/> instances for the built-in dependency types supported by the .NET Project System.
/// External components may implement their own instances for their own dependency types.
/// </summary>
internal static class DependencyGroupTypes
{
    public static DependencyGroupType Analyzers { get; } = new(
        id: "Analyzer",
        caption: Resources.AnalyzersNodeName,
        normalGroupIcon: KnownProjectImageMonikers.CodeInformation,
        warningGroupIcon: KnownProjectImageMonikers.CodeInformationWarning,
        errorGroupIcon: KnownProjectImageMonikers.CodeInformationError,
        groupNodeFlags: DependencyTreeFlags.AnalyzerDependencyGroup);

    public static DependencyGroupType Assemblies { get; } = new(
        id: "Assembly",
        caption: Resources.AssembliesNodeName,
        normalGroupIcon: KnownProjectImageMonikers.Reference,
        warningGroupIcon: KnownProjectImageMonikers.ReferenceWarning,
        errorGroupIcon: KnownProjectImageMonikers.ReferenceError,
        groupNodeFlags: DependencyTreeFlags.AssemblyDependencyGroup);

    public static DependencyGroupType Com { get; } = new(
        id: "Com",
        caption: Resources.ComNodeName,
        normalGroupIcon: KnownProjectImageMonikers.COM,
        warningGroupIcon: KnownProjectImageMonikers.COMWarning,
        errorGroupIcon: KnownProjectImageMonikers.COMError,
        groupNodeFlags: DependencyTreeFlags.ComDependencyGroup);

    public static DependencyGroupType Frameworks { get; } = new(
        id: "Framework",
        caption: Resources.FrameworkNodeName,
        normalGroupIcon: KnownProjectImageMonikers.Framework,
        warningGroupIcon: KnownProjectImageMonikers.FrameworkWarning,
        errorGroupIcon: KnownProjectImageMonikers.FrameworkError,
        groupNodeFlags: DependencyTreeFlags.FrameworkDependencyGroup);

    public static DependencyGroupType Packages { get; } = new(
        id: "Package",
        caption: Resources.PackagesNodeName,
        normalGroupIcon: KnownProjectImageMonikers.NuGetNoColor,
        warningGroupIcon: KnownProjectImageMonikers.NuGetNoColorWarning,
        errorGroupIcon: KnownProjectImageMonikers.NuGetNoColorError,
        groupNodeFlags: DependencyTreeFlags.PackageDependencyGroup);

    public static DependencyGroupType Projects { get; } = new(
        id: "Project",
        caption: Resources.ProjectsNodeName,
        normalGroupIcon: KnownProjectImageMonikers.Application,
        warningGroupIcon: KnownProjectImageMonikers.ApplicationWarning,
        errorGroupIcon: KnownProjectImageMonikers.ApplicationError,
        groupNodeFlags: DependencyTreeFlags.ProjectDependencyGroup);

    public static DependencyGroupType Sdks { get; } = new(
        id: "SDK",
        caption: Resources.SdkNodeName,
        normalGroupIcon: KnownProjectImageMonikers.SDK,
        warningGroupIcon: KnownProjectImageMonikers.SDKWarning,
        errorGroupIcon: KnownProjectImageMonikers.SDKError,
        groupNodeFlags: DependencyTreeFlags.SdkDependencyGroup);
}

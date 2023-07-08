// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Imaging;

namespace Microsoft.VisualStudio.ProjectSystem;

/// <summary>
/// Provides singleton instances of <see cref="ProjectImageMoniker"/> objects that correspond
/// to instances of <see cref="KnownMonikers"/> images.
/// </summary>
/// <remarks>
/// While the <see cref="ImageMonikerExtensions.ToProjectSystemType"/> extension method caches instances
/// of this type, it takes a lock and performs a collection look up. This type provides faster access to
/// these instances without any possibility for contention, however short-lived.
/// </remarks>
internal static class KnownProjectImageMonikers
{
    public static ProjectImageMoniker CSProjectNode { get; } = KnownMonikers.CSProjectNode.ToProjectSystemType();
    public static ProjectImageMoniker CSSharedProject { get; } = KnownMonikers.CSSharedProject.ToProjectSystemType();
    public static ProjectImageMoniker VBProjectNode { get; } = KnownMonikers.VBProjectNode.ToProjectSystemType();
    public static ProjectImageMoniker VBSharedProject { get; } = KnownMonikers.VBSharedProject.ToProjectSystemType();

    public static ProjectImageMoniker SharedProject { get; } = KnownMonikers.SharedProject.ToProjectSystemType();

    public static ProjectImageMoniker ReferenceGroup { get; } = KnownMonikers.ReferenceGroup.ToProjectSystemType();
    public static ProjectImageMoniker ReferenceGroupWarning { get; } = KnownMonikers.ReferenceGroupWarning.ToProjectSystemType();
    public static ProjectImageMoniker ReferenceGroupError { get; } = KnownMonikers.ReferenceGroupError.ToProjectSystemType();

    public static ProjectImageMoniker CodeInformation  { get; } = KnownMonikers.CodeInformation.ToProjectSystemType();
    public static ProjectImageMoniker CodeInformationWarning { get; } = KnownMonikers.CodeInformationWarning.ToProjectSystemType();
    public static ProjectImageMoniker CodeInformationError { get; } = KnownMonikers.CodeInformationError.ToProjectSystemType();
    public static ProjectImageMoniker CodeInformationPrivate { get; } = KnownMonikers.CodeInformationPrivate.ToProjectSystemType();

    public static ProjectImageMoniker Reference  { get; } = KnownMonikers.Reference.ToProjectSystemType();
    public static ProjectImageMoniker ReferenceWarning { get; } = KnownMonikers.ReferenceWarning.ToProjectSystemType();
    // TODO get a "ReferenceError" icon https://github.com/dotnet/project-system/issues/8946
    public static ProjectImageMoniker ReferenceError { get; } = KnownMonikers.ReferenceWarning.ToProjectSystemType();
    public static ProjectImageMoniker ReferencePrivate { get; } = KnownMonikers.ReferencePrivate.ToProjectSystemType();

    public static ProjectImageMoniker COM { get; } = KnownMonikers.COM.ToProjectSystemType();
    public static ProjectImageMoniker COMWarning { get; } = KnownMonikers.COMWarning.ToProjectSystemType();
    public static ProjectImageMoniker COMError { get; } = KnownMonikers.COMError.ToProjectSystemType();
    public static ProjectImageMoniker COMPrivate { get; } = KnownMonikers.COMPrivate.ToProjectSystemType();

    public static ProjectImageMoniker Library { get; } = KnownMonikers.Library.ToProjectSystemType();
    public static ProjectImageMoniker LibraryWarning { get; } = KnownMonikers.LibraryWarning.ToProjectSystemType();
    // TODO get a "LibraryError" icon https://github.com/dotnet/project-system/issues/8946
    public static ProjectImageMoniker LibraryError { get; } = KnownMonikers.LibraryWarning.ToProjectSystemType();

    public static ProjectImageMoniker Framework { get; } = KnownMonikers.Framework.ToProjectSystemType();
    public static ProjectImageMoniker FrameworkWarning { get; } = KnownMonikers.FrameworkWarning.ToProjectSystemType();
    public static ProjectImageMoniker FrameworkError { get; } = KnownMonikers.FrameworkError.ToProjectSystemType();
    public static ProjectImageMoniker FrameworkPrivate { get; } = KnownMonikers.FrameworkPrivate.ToProjectSystemType();

    public static ProjectImageMoniker NuGetNoColor { get; } = KnownMonikers.NuGetNoColor.ToProjectSystemType();
    public static ProjectImageMoniker NuGetNoColorWarning { get; } = KnownMonikers.NuGetNoColorWarning.ToProjectSystemType();
    public static ProjectImageMoniker NuGetNoColorError { get; } = KnownMonikers.NuGetNoColorError.ToProjectSystemType();
    public static ProjectImageMoniker NuGetNoColorPrivate { get; } = KnownMonikers.NuGetNoColorPrivate.ToProjectSystemType();

    public static ProjectImageMoniker Application { get; } = KnownMonikers.Application.ToProjectSystemType();
    public static ProjectImageMoniker ApplicationWarning { get; } = KnownMonikers.ApplicationWarning.ToProjectSystemType();
    public static ProjectImageMoniker ApplicationError { get; } = KnownMonikers.ApplicationError.ToProjectSystemType();
    public static ProjectImageMoniker ApplicationPrivate { get; } = KnownMonikers.ApplicationPrivate.ToProjectSystemType();

    public static ProjectImageMoniker SDK { get; } = KnownMonikers.SDK.ToProjectSystemType();
    public static ProjectImageMoniker SDKWarning { get; } = KnownMonikers.SDKWarning.ToProjectSystemType();
    public static ProjectImageMoniker SDKError { get; } = KnownMonikers.SDKError.ToProjectSystemType();
    public static ProjectImageMoniker SDKPrivate { get; } = KnownMonikers.SDKPrivate.ToProjectSystemType();
}

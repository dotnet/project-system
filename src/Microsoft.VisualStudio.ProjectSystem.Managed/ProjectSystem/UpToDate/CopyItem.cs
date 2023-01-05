// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.UpToDate;

/// <summary>
/// Models an item that is copied to a project's output directory when that project is built.
/// </summary>
internal readonly struct CopyItem
{
    /// <summary>
    /// Gets the absolute path to the item to copy.
    /// </summary>
    public string AbsoluteSourcePath { get; }

    /// <summary>
    /// Gets path to which this item is copied, relative to the output directory.
    /// </summary>
    public string RelativeTargetPath { get; }

    /// <summary>
    /// Gets a value indicating when the item is copied during build.
    /// </summary>
    public BuildUpToDateCheck.CopyType CopyType { get; }

    /// <summary>
    /// Gets whether this item should only be checked when Build Acceleration is enabled.
    /// </summary>
    public bool IsBuildAccelerationOnly { get; }

    public CopyItem(string path, string targetPath, BuildUpToDateCheck.CopyType copyType, bool isBuildAccelerationOnly)
    {
        Requires.NotNull(targetPath, nameof(targetPath));
        System.Diagnostics.Debug.Assert(Path.IsPathRooted(path), "Path.IsPathRooted(path)");
        System.Diagnostics.Debug.Assert(!Path.IsPathRooted(targetPath), "!Path.IsPathRooted(targetPath)");

        AbsoluteSourcePath = path;
        RelativeTargetPath = targetPath;
        CopyType = copyType;
        IsBuildAccelerationOnly = isBuildAccelerationOnly;
    }

    public CopyItem(string path, IImmutableDictionary<string, string> metadata)
        : this(path, GetTargetPath(metadata), GetCopyType(metadata), GetIsBuildAccelerationOnly(metadata))
    {
    }

    private static string GetTargetPath(IImmutableDictionary<string, string> metadata)
    {
        Assumes.True(metadata.TryGetValue(CopyToOutputDirectoryItem.TargetPathProperty, out string? targetPath));
        return targetPath;
    }

    private static BuildUpToDateCheck.CopyType GetCopyType(IImmutableDictionary<string, string> metadata)
    {
        Assumes.True(metadata.TryGetValue(CopyToOutputDirectoryItem.CopyToOutputDirectoryProperty, out string? value));
        return ParseCopyType(value);

        static BuildUpToDateCheck.CopyType ParseCopyType(string value)
        {
            if (string.Equals(value, CopyToOutputDirectoryItem.CopyToOutputDirectoryValues.Always, StringComparisons.PropertyLiteralValues))
            {
                return BuildUpToDateCheck.CopyType.Always;
            }

            if (string.Equals(value, CopyToOutputDirectoryItem.CopyToOutputDirectoryValues.PreserveNewest, StringComparisons.PropertyLiteralValues))
            {
                return BuildUpToDateCheck.CopyType.PreserveNewest;
            }

            throw Assumes.Fail($"CopyToOutputDirectory should be Always or PreserveNewest, not {value}");
        }
    }

    private static bool GetIsBuildAccelerationOnly(IImmutableDictionary<string, string> metadata)
    {
        Assumes.True(metadata.TryGetBoolProperty(CopyToOutputDirectoryItem.BuildAccelerationOnlyProperty, out bool value));
        return value;
    }

    public void Deconstruct(out string path, out string targetPath, out BuildUpToDateCheck.CopyType copyType, out bool isBuildAccelerationOnly)
    {
        path = AbsoluteSourcePath;
        targetPath = RelativeTargetPath;
        copyType = CopyType;
        isBuildAccelerationOnly = IsBuildAccelerationOnly;
    }

    public override string ToString()
    {
        return (Source: AbsoluteSourcePath, Target: RelativeTargetPath, CopyType).ToString();
    }
}

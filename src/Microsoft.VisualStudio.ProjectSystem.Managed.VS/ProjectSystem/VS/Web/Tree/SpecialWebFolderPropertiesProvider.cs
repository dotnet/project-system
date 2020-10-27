// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Web.Tree
{
    /// <summary>
    ///     Responsible for identifying and marking ASP.NET special folders.
    /// </summary>
    internal class SpecialWebFolderPropertiesProvider : IProjectTreePropertiesProvider
    {
        private static readonly ProjectTreeFlags s_projectRootOrCodeFolder = ProjectTreeFlags.ProjectRoot | SpecialWebFolder.Code.Flag;
        private static readonly IReadOnlyDictionary<string, SpecialWebFolder> s_wellKnownSpecialFolders = CreateWellKnownSpecialFolders();

        private readonly UnconfiguredProject _project;
        private readonly IImmutableSet<string> _codeFolders;

        public SpecialWebFolderPropertiesProvider(UnconfiguredProject project, IImmutableSet<string> codeFolders)
        {
            _project = project;
            _codeFolders = codeFolders;
        }

        public void CalculatePropertyValues(IProjectTreeCustomizablePropertyContext propertyContext, IProjectTreeCustomizablePropertyValues propertyValues)
        {
            if (IsSpecialWebFolder(propertyContext, propertyValues, out SpecialWebFolder? folder))
            {
                propertyValues.Icon = folder.Icon.ToProjectSystemType();
                propertyValues.ExpandedIcon = folder.ExpandedIcon.ToProjectSystemType();
                propertyValues.Flags += folder.Flag;
            }
        }

        private bool IsSpecialWebFolder(IProjectTreeCustomizablePropertyContext propertyContext, IProjectTreeCustomizablePropertyValues propertyValues, [NotNullWhen(true)] out SpecialWebFolder? folder)
        {
            if (propertyContext.IsFolder && propertyValues.Flags.IsIncludedInProject())
            {
                if (IsWellKnownFolder(propertyContext, out folder) || IsCodeFolder(propertyContext, out folder))
                {
                    return true;
                }
            }

            folder = null;
            return false;
        }

        private static bool IsWellKnownFolder(IProjectTreeCustomizablePropertyContext propertyContext, [NotNullWhen(true)] out SpecialWebFolder? folder)
        {
            folder = null;

            // Well-known folders only exist in the root
            if (!propertyContext.ParentNodeFlags.IsProjectRoot())
                return false;

            return s_wellKnownSpecialFolders.TryGetValue(propertyContext.ItemName, out folder);
        }

        private bool IsCodeFolder(IProjectTreeCustomizablePropertyContext propertyContext, [NotNullWhen(true)] out SpecialWebFolder? folder)
        {
            folder = null;

            // Code folders are either in the root, or within another code folder
            if (!propertyContext.ParentNodeFlags.ContainsAny(s_projectRootOrCodeFolder))
                return false;
            
            if (_codeFolders.Count == 0)
                return false;

            if (propertyContext.Metadata == null || !propertyContext.Metadata.TryGetValue(Folder.FullPathProperty, out string fullPath))
                return false;

            Assumes.True(fullPath.EndsWith("\\", StringComparison.Ordinal));

            string relativePath = _project.MakeRelative(fullPath);

            if (!_codeFolders.Contains(relativePath))
                return false;

            folder = SpecialWebFolder.Code;
            return true;
        }

        private static IReadOnlyDictionary<string, SpecialWebFolder> CreateWellKnownSpecialFolders()
        {
            // App_Code is provided explicitly via "code folders"
            return new Dictionary<string, SpecialWebFolder>(StringComparers.Paths)
            {
                { "Bin",                       SpecialWebFolder.Bin},
                { "App_GlobalResources",       SpecialWebFolder.GlobalResources},
                { "App_Data",                  SpecialWebFolder.Data},
                { "App_Themes",                SpecialWebFolder.Themes},
                { "App_Browsers",              SpecialWebFolder.Browsers},
                { "App_LocalResources",        SpecialWebFolder.LocalResources},
            };
        }

        private record SpecialWebFolder
        (
            ProjectTreeFlags Flag,
            ImageMoniker Icon,
            ImageMoniker ExpandedIcon
        )
        {
            // TODO: Correct icons
            public static SpecialWebFolder Code             = new(SpecialWebFolderFlag.CodeFolder,              KnownMonikers.SpecialFolderClosed, KnownMonikers.SpecialFolderOpened);
            public static SpecialWebFolder Bin              = new(SpecialWebFolderFlag.BinFolder,               KnownMonikers.SpecialFolderClosed, KnownMonikers.SpecialFolderOpened);
            public static SpecialWebFolder GlobalResources  = new(SpecialWebFolderFlag.GlobalResourcesFolder,   KnownMonikers.SpecialFolderClosed, KnownMonikers.SpecialFolderOpened);
            public static SpecialWebFolder Data             = new(SpecialWebFolderFlag.DataFolder,              KnownMonikers.SpecialFolderClosed, KnownMonikers.SpecialFolderOpened);
            public static SpecialWebFolder Themes           = new(SpecialWebFolderFlag.ThemesFolder,            KnownMonikers.SpecialFolderClosed, KnownMonikers.SpecialFolderOpened);
            public static SpecialWebFolder Browsers         = new(SpecialWebFolderFlag.BrowsersFolder,          KnownMonikers.SpecialFolderClosed, KnownMonikers.SpecialFolderOpened);
            public static SpecialWebFolder LocalResources   = new(SpecialWebFolderFlag.LocalResourcesFolder,    KnownMonikers.SpecialFolderClosed, KnownMonikers.SpecialFolderOpened);
        }
    }
}

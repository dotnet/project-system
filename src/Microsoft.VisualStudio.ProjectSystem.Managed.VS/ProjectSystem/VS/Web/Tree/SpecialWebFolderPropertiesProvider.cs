// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Web.Tree
{
    /// <summary>
    ///     Responsible for identifying and marking ASP.NET special folders.
    /// </summary>
    internal class SpecialWebFolderPropertiesProvider : IProjectTreePropertiesProvider
    {
        private static readonly Dictionary<string, SpecialWebFolder> s_knownSpecialFolders = CreateKnownSpecialFolders();
        private readonly IImmutableSet<string> _codeFolders;

        public SpecialWebFolderPropertiesProvider(IImmutableSet<string> codeFolders)
        {
            _codeFolders = codeFolders;
        }

        public void CalculatePropertyValues(IProjectTreeCustomizablePropertyContext propertyContext, IProjectTreeCustomizablePropertyValues propertyValues)
        {
            if (propertyContext.ParentNodeFlags.IsProjectRoot() && 
                propertyContext.IsFolder && 
                propertyValues.Flags.IsIncludedInProject())
            {
                if (s_knownSpecialFolders.TryGetValue(propertyContext.ItemName, out SpecialWebFolder folder))
                {
                    propertyValues.Icon = folder.Icon.ToProjectSystemType();
                    propertyValues.ExpandedIcon = folder.ExpandedIcon.ToProjectSystemType();
                    propertyValues.Flags += folder.Flag;
                }

                // TODO: Handle CodeFolders
                Assumes.NotNull(_codeFolders);
            }
        }

        private static Dictionary<string, SpecialWebFolder> CreateKnownSpecialFolders()
        {
            // TODO: Correct icons
            return new Dictionary<string, SpecialWebFolder>(StringComparers.Paths)
            {
                { "App_Code",                  new(SpecialWebFolderFlag.CodeFolder,            KnownMonikers.SpecialFolderClosed, KnownMonikers.SpecialFolderOpened)},
                { "Bin",                       new(SpecialWebFolderFlag.BinFolder,             KnownMonikers.SpecialFolderClosed, KnownMonikers.SpecialFolderOpened)},
                { "App_GlobalResources",       new(SpecialWebFolderFlag.ResourcesFolder,       KnownMonikers.SpecialFolderClosed, KnownMonikers.SpecialFolderOpened)},
                { "App_Data",                  new(SpecialWebFolderFlag.DataFolder,            KnownMonikers.SpecialFolderClosed, KnownMonikers.SpecialFolderOpened)},
                { "App_Themes",                new(SpecialWebFolderFlag.ThemesFolder,          KnownMonikers.SpecialFolderClosed, KnownMonikers.SpecialFolderOpened)},
                { "App_Browsers",              new(SpecialWebFolderFlag.BrowsersFolder,        KnownMonikers.SpecialFolderClosed, KnownMonikers.SpecialFolderOpened)},
                { "App_LocalResources",        new(SpecialWebFolderFlag.LocalResourcesFolder,  KnownMonikers.SpecialFolderClosed, KnownMonikers.SpecialFolderOpened)},
            };
        }

        private record SpecialWebFolder
        (
            ProjectTreeFlags Flag,
            ImageMoniker Icon,
            ImageMoniker ExpandedIcon
        );
    }
}

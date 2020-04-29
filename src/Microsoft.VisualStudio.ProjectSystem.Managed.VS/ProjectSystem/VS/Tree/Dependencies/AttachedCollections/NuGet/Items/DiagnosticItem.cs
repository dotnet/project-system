// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.NuGet.Models;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.AttachedCollections;

namespace Microsoft.VisualStudio.NuGet
{
    /// <summary>
    /// Backing object for diagnostic message items within a package within the dependencies tree.
    /// </summary>
    internal sealed class DiagnosticItem : RelatableItemBase
    {
        public AssetsFileTarget Target { get; private set; }
        public AssetsFileTargetLibrary Library { get; private set; }
        public AssetsFileLogMessage Log { get; private set; }

        public DiagnosticItem(AssetsFileTarget target, AssetsFileTargetLibrary library, AssetsFileLogMessage log)
            : base(log.Message)
        {
            Target = target;
            Library = library;
            Log = log;
        }

        public override object Identity => Tuple.Create(Library.Name, Log.Message);

        public override int Priority => AttachedItemPriority.Diagnostic;

        public override ImageMoniker IconMoniker => Log.Level switch
        {
            global::NuGet.Common.LogLevel.Error => KnownMonikers.StatusError,
            global::NuGet.Common.LogLevel.Warning => KnownMonikers.StatusWarning,
            _ => KnownMonikers.StatusInformation
        };

        public bool TryUpdateState(AssetsFileTarget target, AssetsFileTargetLibrary library, in AssetsFileLogMessage log)
        {
            if (ReferenceEquals(Target, target) && ReferenceEquals(Library, library))
            {
                return false;
            }

            Target = target;
            Library = library;
            Log = log;
            Text = log.Message;
            return true;
        }

        public override object? GetBrowseObject() => new BrowseObject(this);

        private sealed class BrowseObject : BrowseObjectBase
        {
            private readonly DiagnosticItem _item;

            public BrowseObject(DiagnosticItem log) => _item = log;

            public override string GetComponentName() => Code;

            public override string GetClassName() => VSResources.DiagnosticBrowseObjectClassName;

            [BrowseObjectDisplayName(nameof(VSResources.DiagnosticMessageDisplayName))]
            [BrowseObjectDescription(nameof(VSResources.DiagnosticMessageDescription))]
            public string Message => _item.Log.Message;

            [BrowseObjectDisplayName(nameof(VSResources.DiagnosticCodeDisplayName))]
            [BrowseObjectDescription(nameof(VSResources.DiagnosticCodeDescription))]
            public string Code => _item.Log.Code.ToString();

            [BrowseObjectDisplayName(nameof(VSResources.DiagnosticLibraryNameDisplayName))]
            [BrowseObjectDescription(nameof(VSResources.DiagnosticLibraryNameDescription))]
            public string LibraryName => _item.Log.LibraryName;

            [BrowseObjectDisplayName(nameof(VSResources.DiagnosticLevelDisplayName))]
            [BrowseObjectDescription(nameof(VSResources.DiagnosticLevelDescription))]
            public string Level => _item.Log.Level.ToString();

            [BrowseObjectDisplayName(nameof(VSResources.DiagnosticWarningLevelDisplayName))]
            [BrowseObjectDescription(nameof(VSResources.DiagnosticWarningLevelDescription))]
            public string WarningLevel => _item.Log.Level == global::NuGet.Common.LogLevel.Warning ? _item.Log.WarningLevel.ToString() : "";
        }
    }
}

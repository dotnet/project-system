// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Assets.Models;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.AttachedCollections
{
    /// <summary>
    /// Backing object for library (package/project) diagnostic nodes in the dependencies tree.
    /// </summary>
    internal sealed class DiagnosticItem : AttachedCollectionItemBase
    {
        private readonly AssetsFileLogMessage _log;

        public DiagnosticItem(AssetsFileLogMessage log)
            : base(log.Message)
        {
            _log = log;
        }

        public override int Priority => AttachedItemPriority.Diagnostic;

        public override ImageMoniker IconMoniker => _log.Level switch
        {
            NuGet.Common.LogLevel.Error => KnownMonikers.StatusError,
            NuGet.Common.LogLevel.Warning => KnownMonikers.StatusWarning,
            _ => KnownMonikers.StatusInformation
        };

        public override object GetBrowseObject() => new BrowseObject(_log);

        private sealed class BrowseObject : BrowseObjectBase
        {
            private readonly AssetsFileLogMessage _log;

            public BrowseObject(AssetsFileLogMessage log) => _log = log;

            public override string GetComponentName() => Code;

            public override string GetClassName() => VSResources.DiagnosticBrowseObjectClassName;

            [BrowseObjectDisplayName(nameof(VSResources.DiagnosticMessageDisplayName))]
            [BrowseObjectDescription(nameof(VSResources.DiagnosticMessageDescription))]
            public string Message => _log.Message;

            [BrowseObjectDisplayName(nameof(VSResources.DiagnosticCodeDisplayName))]
            [BrowseObjectDescription(nameof(VSResources.DiagnosticCodeDescription))]
            public string Code => _log.Code.ToString();

            [BrowseObjectDisplayName(nameof(VSResources.DiagnosticLibraryDisplayName))]
            [BrowseObjectDescription(nameof(VSResources.DiagnosticLibraryDescription))]
            public string LibraryId => _log.LibraryId;

            [BrowseObjectDisplayName(nameof(VSResources.DiagnosticLevelDisplayName))]
            [BrowseObjectDescription(nameof(VSResources.DiagnosticLevelDescription))]
            public string Level => _log.Level.ToString();

            [BrowseObjectDisplayName(nameof(VSResources.DiagnosticWarningLevelDisplayName))]
            [BrowseObjectDescription(nameof(VSResources.DiagnosticWarningLevelDescription))]
            public string WarningLevel => _log.Level == NuGet.Common.LogLevel.Warning ? _log.WarningLevel.ToString() : "";
        }
    }
}

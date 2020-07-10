// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Models
{
    internal static class IDependencyModelExtensions
    {
        public static DependencyId GetDependencyId(this IDependencyModel self)
        {
            return new DependencyId(self.ProviderType, self.Id);
        }

        public static IDependencyViewModel ToViewModel(this IDependencyModel self, DiagnosticLevel diagnosticLevel)
        {
            return new DependencyModelViewModel(self, diagnosticLevel);
        }

        private sealed class DependencyModelViewModel : IDependencyViewModel
        {
            private readonly IDependencyModel _model;
            private readonly DiagnosticLevel _diagnosticLevel;

            public DependencyModelViewModel(IDependencyModel model, DiagnosticLevel diagnosticLevel)
            {
                _model = model;
                _diagnosticLevel = diagnosticLevel;
            }

            public string Caption => _model.Caption;
            public string? FilePath => _model.Path;
            public string? SchemaName => _model.SchemaName;
            public string? SchemaItemType => _model.SchemaItemType;
            public ImageMoniker Icon => _diagnosticLevel == DiagnosticLevel.None ? _model.Icon : _model.UnresolvedIcon;
            public ImageMoniker ExpandedIcon => _diagnosticLevel == DiagnosticLevel.None ? _model.ExpandedIcon : _model.UnresolvedExpandedIcon;
            public ProjectTreeFlags Flags => _model.Flags;
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Models
{
    internal static class IDependencyModelExtensions
    {
        public static IDependencyViewModel ToViewModel(this IDependencyModel self, bool hasUnresolvedDependency)
        {
            return new DependencyModelViewModel(self, hasUnresolvedDependency);
        }

        private sealed class DependencyModelViewModel : IDependencyViewModel
        {
            private readonly IDependencyModel _model;
            private readonly bool _hasUnresolvedDependency;

            public DependencyModelViewModel(IDependencyModel model, bool hasUnresolvedDependency)
            {
                _model = model;
                _hasUnresolvedDependency = hasUnresolvedDependency;
            }

            public string Caption => _model.Caption;
            public string? SchemaName => _model.SchemaName;
            public string? SchemaItemType => _model.SchemaItemType;
            public ImageMoniker Icon => _hasUnresolvedDependency ? _model.UnresolvedIcon : _model.Icon;
            public ImageMoniker ExpandedIcon => _hasUnresolvedDependency ? _model.UnresolvedExpandedIcon : _model.ExpandedIcon;
            public ProjectTreeFlags Flags => _model.Flags;
        }
    }
}

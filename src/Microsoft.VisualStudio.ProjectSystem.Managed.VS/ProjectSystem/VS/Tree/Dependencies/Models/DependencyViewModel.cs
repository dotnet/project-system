// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models
{
    internal sealed class DependencyViewModel : IDependencyViewModel
    {
        private readonly IDependencyModel _model;
        private readonly bool _hasUnresolvedDependency;

        public DependencyViewModel(IDependency dependency, bool hasUnresolvedDependency)
            : this((IDependencyModel)dependency, hasUnresolvedDependency)
        {
            OriginalModel = dependency;
        }

        public DependencyViewModel(IDependencyModel model, bool hasUnresolvedDependency)
        {
            _model = model;
            _hasUnresolvedDependency = hasUnresolvedDependency;
        }

        public IDependency OriginalModel { get; }

        public string Caption => _model.Caption;
        public string FilePath => _model.Id;
        public string SchemaName => _model.SchemaName;
        public string SchemaItemType => _model.SchemaItemType;
        public int Priority => _model.Priority;
        public ImageMoniker Icon => _hasUnresolvedDependency ? _model.UnresolvedIcon : _model.Icon;
        public ImageMoniker ExpandedIcon => _hasUnresolvedDependency ? _model.UnresolvedExpandedIcon : _model.ExpandedIcon;
        public IImmutableDictionary<string, string> Properties => _model.Properties;
        public ProjectTreeFlags Flags => _model.Flags;
    }
}

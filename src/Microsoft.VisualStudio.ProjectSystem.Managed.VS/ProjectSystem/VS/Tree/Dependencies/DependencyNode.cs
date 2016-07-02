// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    public class DependencyNode : IDependencyNode
    {
        private static ImageMoniker EmptyImageMoniker = new ImageMoniker();

        public DependencyNode(IProjectDependenciesSubTreeProvider provider,
                                         string id,
                                         string caption,
                                         ImageMoniker icon,
                                         ProjectTreeFlags flags,
                                         string itemType,
                                         int priority = 0,
                                         IImmutableDictionary<string, string> properties = null,
                                         ImageMoniker expandedIcon = new ImageMoniker())
        {
            Provider = provider;
            Id = id;
            Caption = caption;
            Icon = icon;
            ExpandedIcon = expandedIcon.Equals(EmptyImageMoniker) ? Icon : expandedIcon;
            Priority = priority;
            Flags = flags;
            Properties = properties;
            ItemType = itemType;
        }

        public string Id { get; private set; }

        public string Caption { get; private set; }

        public ImageMoniker Icon { get; private set; }

        public ImageMoniker ExpandedIcon { get; private set; }

        public int Priority { get; private set; }

        public ProjectTreeFlags Flags { get; private set; }

        public IImmutableDictionary<string, string> Properties { get; private set; }

        public string ItemType { get; private set; }

        private List<IDependencyNode> _children = new List<IDependencyNode>();
        public IEnumerable<IDependencyNode> Children
        {
            get
            {
                return _children;
            }
        }

        public bool HasChildren
        {
            get
            {
                return _children.Count != 0;
            }
        }

        public IProjectDependenciesSubTreeProvider Provider { get; private set; }

        public void AddChild(IDependencyNode childNode)
        {
            Requires.NotNull(childNode, nameof(childNode));

            if (!_children.Any(x => x.Id.Equals(childNode.Id)))
            {
                _children.Add(childNode);
            }
        }

        public void RemoveChild(IDependencyNode childNode)
        {
            Requires.NotNull(childNode, nameof(childNode));

            if (_children.Any(x => x.Id.Equals(childNode.Id)))
            {
                _children.RemoveAll(x => x.Id.Equals(childNode.Id));
            }
        }
    }
}

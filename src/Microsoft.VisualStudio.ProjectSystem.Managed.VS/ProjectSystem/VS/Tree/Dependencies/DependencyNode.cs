// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    internal class DependencyNode : IDependencyNode
    {
        // These priorities are for graph nodes only and are used to group graph nodes 
        // appropriatelly in order groups predefined order instead of alphabetically.
        // Order is not changed for top dependency nodes only for grpah hierarchies.
        public const int DiagnosticsNodePriority = 100; // for any custom nodes like errors or warnings
        public const int UnresolvedReferenceNodePriority = 110;
        public const int ProjectNodePriority = 120;
        public const int PackageNodePriority = 130;
        public const int FrameworkAssemblyNodePriority = 140;
        public const int PackageAssemblyNodePriority = 150;
        public const int AnalyzerNodePriority = 160;
        public const int ComNodePriority = 170;
        public const int SdkNodePriority = 180;
        
        /// <summary>
        /// The set of flags common to all Reference nodes.
        /// </summary>
        private static readonly ProjectTreeFlags BaseReferenceFlags
                = ProjectTreeFlags.Create(ProjectTreeFlags.Common.Reference);

        /// <summary>
        /// The set of flags to assign to unresolvable Reference nodes.
        /// Note: when dependency has ProjectTreeFlags.Common.BrokenReference flag, GraphProvider API are not 
        /// called for that node.
        /// </summary>
        private static readonly ProjectTreeFlags UnresolvedReferenceFlags
                = BaseReferenceFlags.Add(ProjectTreeFlags.Common.BrokenReference);

        /// <summary>
        /// The set of flags to assign to resolved Reference nodes.
        /// </summary>
        private static readonly ProjectTreeFlags ResolvedReferenceFlags
                = BaseReferenceFlags.Add(ProjectTreeFlags.Common.ResolvedReference);

        /// <summary>
        /// These public flags below are to be used with all nodes: default project item
        /// nodes and all custom nodes provided by third party IProjectDependenciesSubTreeProvider
        /// implementations. This is to have a way to distinguish dependency nodes in general.
        /// </summary>
        public static readonly ProjectTreeFlags DependencyFlags
                = ProjectTreeFlags.Create("Dependency")
                                  .Add(ProjectTreeFlags.Common.VirtualFolder.ToString())
                                  .Add(ProjectTreeFlags.Common.BubbleUp);

        public static readonly ProjectTreeFlags UnresolvedDependencyFlags
                = ProjectTreeFlags.Create("Unresolved")
                                  .Union(DependencyFlags);

        public static readonly ProjectTreeFlags ResolvedDependencyFlags
                = ProjectTreeFlags.Create("Resolved")
                                  .Union(DependencyFlags);

        public static readonly ProjectTreeFlags PreFilledFolderNode
                = ProjectTreeFlags.Create("PreFilledFolderNode");

        public static readonly ProjectTreeFlags CustomItemSpec
                = ProjectTreeFlags.Create("CustomItemSpec");

        public static readonly ProjectTreeFlags DoesNotSupportRemove
                = ProjectTreeFlags.Create("DoesNotSupportRemove");

         /// <summary>
         /// This flg indicates that dependency shows a hierarchy or other data that is coming from other sub 
         /// tree provider. This would allow components responsible for data displaying do necessary steps to 
         /// stay in sync with other providers changes.
         /// </summary>
         public static readonly ProjectTreeFlags DependsOnOtherProviders
                 = ProjectTreeFlags.Create("DependsOnOtherProviders");

        /// <summary>
        /// These set of flags is internal and should be used only by standard known
        /// project item nodes, that come from design time build. This is important,
        /// since some of flags enable other functionality, like Add Reference dialog
        /// and standrad context menu commands like Remove or View In Object browser.
        /// Note: Remove for project items is using IRule.Context to get correct ItemSpec
        /// for the item to be removed. Thus all default node types that we provide here
        /// do have some IRule associated with them (otheerwise they can not support 
        /// standard Remove command). If a custom node type needs some kind of Remove
        /// or Uninstall command they just need to create their own command handler etc
        /// (standard Remove command will not show up for them since they would not have 
        /// flags below).
        /// </summary>
        internal static readonly ProjectTreeFlags GenericDependencyFlags
                = DependencyFlags.Union(BaseReferenceFlags);

        internal static readonly ProjectTreeFlags GenericUnresolvedDependencyFlags
                = UnresolvedDependencyFlags.Union(UnresolvedReferenceFlags);

        internal static readonly ProjectTreeFlags GenericResolvedDependencyFlags
                = ResolvedDependencyFlags.Union(ResolvedReferenceFlags);

        internal DependencyNode()
        {
            // for unit tests
        }

        public DependencyNode(DependencyNodeId id,
                              ProjectTreeFlags flags,
                              int priority = 0,
                              IImmutableDictionary<string, string> properties = null,
                              bool resolved = true)
        {
            Requires.NotNull(id, nameof(id));

            Id = id;
            Caption = Id.ItemSpec ?? string.Empty;
            Priority = priority;
            Properties = properties;
            Resolved = resolved;
            Flags = Resolved
                        ? ResolvedDependencyFlags.Union(flags)
                        : UnresolvedDependencyFlags.Union(flags);
        }

        private DependencyNode(IDependencyNode node)
        {
            Id = node.Id;
            Caption = node.Caption;
            Name = node.Name;
            Priority = node.Priority;
            Properties = node.Properties;
            Resolved = node.Resolved;
            Flags = node.Flags;
            Icon = node.Icon;
            ExpandedIcon = node.ExpandedIcon;

            AddChildren(node.Children);
        }

        private readonly object _childrenLock = new object();

        /// <summary>
        /// Unique id of the node, combination of ItemSpec, ItemType, ProviderType and when
        /// needed some unique token.
        /// </summary>
        public DependencyNodeId Id { get; protected set; }

        /// <summary>
        /// Node's caption displayed in the tree
        /// Note: Dependency node has public set for caption, since sometimes providers need
        /// to change captions. All other consumers will use IDependencyNode which has only "get".
        /// </summary>
        public string Caption { get; protected set; }

        private string _name;
        public string Name {
            get
            {
                return _name ?? Caption;
            }
            internal set
            {
                _name = value;
            }
        }

        /// <summary>
        /// Unique name constructed in the form Caption (ItemSpec).
        /// Is used to dedupe dependency nodes when they have same caption but different 
        /// ItemSpec (path)
        /// </summary>
        public virtual string Alias
        {
            get
            {
                if (Caption.Contains(Id.ItemSpec))
                {
                    return Caption;
                }
                else
                {
                    return string.Format(CultureInfo.CurrentCulture, "{0} ({1})", Caption, Id.ItemSpec);
                }
            }
        }

        public bool Resolved { get; protected set; }

        public ImageMoniker Icon { get; protected set; }

        public ImageMoniker ExpandedIcon { get; protected set; }

        public int Priority { get; protected set; }

        public ProjectTreeFlags Flags { get; internal set; }

        public IImmutableDictionary<string, string> Properties { get; protected set; }

        private ImmutableHashSet<IDependencyNode> _children = ImmutableHashSet<IDependencyNode>.Empty;
        public ImmutableHashSet<IDependencyNode> Children
        {
            get
            {
                lock (_childrenLock)
                {
                    return _children;
                }
            }
        }

        public virtual bool HasChildren
        {
            get
            {
                return Children.Count > 0;
            }
        }

        /// <summary>
        /// Adds a child to the node
        /// </summary>
        /// <param name="childNode">child node to be added. Should not be null.</param>
        public void AddChild(IDependencyNode childNode)
        {
            Requires.NotNull(childNode, nameof(childNode));

            lock(_childrenLock)
            {
                _children = _children.Add(childNode);
            }
        }

        /// <summary>
        /// Adds children to the node
        /// </summary>
        /// <param name="children">child nodes to be added. Should not be null.</param>
        public void AddChildren(IEnumerable<IDependencyNode> children)
        {
            Requires.NotNull(children, nameof(children));

            lock (_childrenLock)
            {
                var builder = _children.ToBuilder();
                foreach (var child in children)
                {
                    builder.Add(child);
                }

                _children = builder.ToImmutableHashSet();
            }
        }

        /// <summary>
        /// Removes child from node's children.
        /// </summary>
        /// <param name="childNode">Node to be removed if it belongs to children. Should not be null.</param>
        public void RemoveChild(IDependencyNode childNode)
        {
            Requires.NotNull(childNode, nameof(childNode));

            lock (_childrenLock)
            {
                if (_children.Contains(childNode))
                {
                    _children = _children.Remove(childNode);
                }
            }
        }

        /// <summary>
        /// Removes all children from node
        /// </summary>
        public void RemoveAllChildren()
        {
            lock (_childrenLock)
            {
                _children = _children.Clear();
            }
        }

        /// <summary>
        /// Changes some properties of the node
        /// </summary>
        public void SetProperties(string caption = null)
        {
            if (caption != null)
            {
                Caption = caption;
            }
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is IDependencyNode other)
            {
                return Equals(other);
            }

            return false;
        }

        public bool Equals(IDependencyNode other)
        {
            if (other != null && other.Id.Equals(Id))
            {
                return true;
            }

            return false;
        }

        public static IDependencyNode Clone(IDependencyNode node)
        {
            return new DependencyNode(node);
        }
    }
}

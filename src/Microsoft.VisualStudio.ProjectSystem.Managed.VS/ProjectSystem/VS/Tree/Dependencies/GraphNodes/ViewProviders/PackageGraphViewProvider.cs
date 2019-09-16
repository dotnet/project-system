// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.GraphModel;
using Microsoft.VisualStudio.GraphModel.Schemas;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.GraphNodes.ViewProviders
{
    [Export(typeof(IDependenciesGraphViewProvider))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    [Order(Order)]
    internal class PackageGraphViewProvider : GraphViewProviderBase
    {
        public const int Order = 100;

        [ImportingConstructor]
        public PackageGraphViewProvider(IDependenciesGraphBuilder builder)
            : base(builder)
        {
        }

        public override bool SupportsDependency(IDependency dependency)
        {
            return dependency.IsPackage();
        }

        public override void BuildGraph(
            IGraphContext graphContext,
            string projectPath,
            IDependency dependency,
            GraphNode dependencyGraphNode,
            TargetedDependenciesSnapshot targetedSnapshot)
        {
            // store refreshed dependency info
            dependencyGraphNode.SetValue(DependenciesGraphSchema.DependencyIdProperty, dependency.Id);
            dependencyGraphNode.SetValue(DependenciesGraphSchema.ResolvedProperty, dependency.Resolved);

            ImmutableArray<IDependency> children = targetedSnapshot.GetDependencyChildren(dependency);

            if (children.IsEmpty)
            {
                return;
            }

            var regularChildren = new List<IDependency>();
            var fxAssembliesChildren = new List<IDependency>();
            foreach (IDependency childDependency in children)
            {
                if (!childDependency.Visible)
                {
                    continue;
                }

                if (childDependency.Flags.Contains(DependencyTreeFlags.FxAssemblyDependency))
                {
                    fxAssembliesChildren.Add(childDependency);
                }
                else
                {
                    regularChildren.Add(childDependency);
                }
            }

            bool isFxAssembliesFolder = dependencyGraphNode.GetValue<bool>(DependenciesGraphSchema.IsFrameworkAssemblyFolderProperty);
            if (isFxAssembliesFolder)
            {
                foreach (IDependency fxAssembly in fxAssembliesChildren)
                {
                    Builder.AddGraphNode(
                        graphContext,
                        projectPath,
                        dependencyGraphNode,
                        fxAssembly.ToViewModel(targetedSnapshot));
                }
            }
            else
            {
                foreach (IDependency childDependency in regularChildren)
                {
                    Builder.AddGraphNode(
                        graphContext,
                        projectPath,
                        dependencyGraphNode,
                        childDependency.ToViewModel(targetedSnapshot));
                }

                if (fxAssembliesChildren.Count > 0)
                {
                    GraphNode fxAssembliesNode = Builder.AddGraphNode(graphContext, projectPath, dependencyGraphNode, PackageFrameworkAssembliesViewModel.Instance);
                    fxAssembliesNode.SetValue(DgmlNodeProperties.ContainsChildren, true);
                    fxAssembliesNode.SetValue(DependenciesGraphSchema.IsFrameworkAssemblyFolderProperty, true);
                    fxAssembliesNode.SetValue(DependenciesGraphSchema.DependencyIdProperty, dependency.Id);
                    fxAssembliesNode.SetValue(DependenciesGraphSchema.ResolvedProperty, dependency.Resolved);
                }
            }
        }
    }
}

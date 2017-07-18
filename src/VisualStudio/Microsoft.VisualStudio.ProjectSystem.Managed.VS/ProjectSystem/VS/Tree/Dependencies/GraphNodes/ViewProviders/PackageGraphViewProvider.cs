// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
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
            ITargetedDependenciesSnapshot targetedSnapshot)
        {
            // store refreshed dependency info
            dependencyGraphNode.SetValue(DependenciesGraphSchema.DependencyIdProperty, dependency.Id);
            dependencyGraphNode.SetValue(DependenciesGraphSchema.ResolvedProperty, dependency.Resolved);

            var children = targetedSnapshot.GetDependencyChildren(dependency);
            if (children == null)
            {
                return;
            }

            var regularChildren = new List<IDependency>();
            var fxAssembliesChildren = new List<IDependency>();
            foreach (var childDependency in children)
            {
                if (!childDependency.Visible)
                {
                    continue;
                }

                if (childDependency.Flags.Contains(DependencyTreeFlags.FxAssemblyProjectFlags))
                {
                    fxAssembliesChildren.Add(childDependency);
                }
                else
                {
                    regularChildren.Add(childDependency);
                }
            }

            var isFxAssembliesFolder = dependencyGraphNode.GetValue<bool>(DependenciesGraphSchema.IsFrameworkAssemblyFolderProperty);
            if (isFxAssembliesFolder)
            {
                foreach (var fxAssembly in fxAssembliesChildren)
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
                foreach (var childDependency in regularChildren)
                {
                    Builder.AddGraphNode(
                        graphContext, 
                        projectPath, 
                        dependencyGraphNode, 
                        childDependency.ToViewModel(targetedSnapshot));
                }

                if (fxAssembliesChildren.Count > 0)
                {
                    var fxAssembliesViewModel = new PackageFrameworkAssembliesViewModel();
                    var fxAssembliesNode = Builder.AddGraphNode(graphContext, projectPath, dependencyGraphNode, fxAssembliesViewModel);
                    fxAssembliesNode.SetValue(DgmlNodeProperties.ContainsChildren, true);
                    fxAssembliesNode.SetValue(DependenciesGraphSchema.IsFrameworkAssemblyFolderProperty, true);
                    fxAssembliesNode.SetValue(DependenciesGraphSchema.DependencyIdProperty, dependency.Id);
                    fxAssembliesNode.SetValue(DependenciesGraphSchema.ResolvedProperty, dependency.Resolved);
                }
            }
        }
    }
}

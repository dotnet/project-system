// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal class IProjectDependenciesSubTreeProviderMock: IProjectDependenciesSubTreeProvider
    {
        private string _providerTestType = "MyDefaultTestProvider";
        public string ProviderTestType
        {
            get
            {
                return _providerTestType;
            }
            set
            {
                _providerTestType = value;
            }
        }

        public string ProviderType
        {
            get
            {
                return "MyProvider";
            }
        }

        public IDependencyNode RootNode { get; set; }

        public bool IsInErrorState { get; set; }

        public bool ShouldBeVisibleWhenEmpty { get; set; }

        public IEnumerable<ImageMoniker> Icons { get; set; }

        public IDependencyNode GetDependencyNode(DependencyNodeId nodeId)
        {
            return Nodes.FirstOrDefault(x => x.Id.Equals(nodeId));
        }

        private List<IDependencyNode> SearchResults { get; set; } = new List<IDependencyNode>();
        public Task<IEnumerable<IDependencyNode>> SearchAsync(IDependencyNode node, string searchTerm)
        {
            return Task.FromResult((IEnumerable<IDependencyNode>)SearchResults.First(x => x.Equals(node)).Children);
        }

        /// <summary>
        /// Raised when provider's dependencies changed 
        /// </summary>
        public event EventHandler<DependenciesChangedEventArgs> DependenciesChanged;

        private void FireDependenciesChanged()
        {
            DependenciesChanged?.Invoke(null, null);
        }

        private List<IDependencyNode> Nodes { get; set; } = new List<IDependencyNode>();

        public void AddTestDependencyNodes(IEnumerable<IDependencyNode> nodes)
        {
            foreach(var node in nodes)
            {
                Nodes.Add(node);
            }
        }

        public void AddSearchResults(IEnumerable<IDependencyNode> nodes)
        {
            if (nodes != null)
            {
                SearchResults.AddRange(nodes);
            }
        }
    }
}
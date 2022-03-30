// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem
{
    [Export(typeof(IPhysicalProjectTree))]
    internal class PhysicalProjectTree : IPhysicalProjectTree
    {
        private readonly Lazy<IProjectTreeService> _treeService;
        private readonly Lazy<IProjectTreeProvider> _treeProvider;
        private readonly Lazy<IPhysicalProjectTreeStorage> _treeStorage;

        [ImportingConstructor]
        public PhysicalProjectTree([Import(ExportContractNames.ProjectTreeProviders.PhysicalProjectTreeService)]Lazy<IProjectTreeService> treeService,
                                   [Import(ExportContractNames.ProjectTreeProviders.PhysicalViewTree)]Lazy<IProjectTreeProvider> treeProvider,
                                   Lazy<IPhysicalProjectTreeStorage> treeStorage)
        {
            _treeService = treeService;
            _treeProvider = treeProvider;
            _treeStorage = treeStorage;
        }

        public IProjectTree? CurrentTree
        {
            get { return _treeService.Value.CurrentTree?.Tree; }
        }

        public IProjectTreeService TreeService
        {
            get { return _treeService.Value; }
        }

        public IProjectTreeProvider TreeProvider
        {
            get { return _treeProvider.Value; }
        }

        public IPhysicalProjectTreeStorage TreeStorage
        {
            get { return _treeStorage.Value; }
        }
    }
}

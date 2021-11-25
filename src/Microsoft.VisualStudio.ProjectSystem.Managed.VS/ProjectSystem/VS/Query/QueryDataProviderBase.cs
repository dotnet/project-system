// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal abstract class QueryDataProviderBase
    {
        private readonly Lazy<IProjectService2> _projectService;

        protected QueryDataProviderBase(IProjectServiceAccessor projectServiceAccessor)
        {
            _projectService = new Lazy<IProjectService2>(
                () => (IProjectService2)projectServiceAccessor.GetProjectService(),
                System.Threading.LazyThreadSafetyMode.PublicationOnly);
        }

        protected IProjectService2 ProjectService => _projectService.Value;
    }
}

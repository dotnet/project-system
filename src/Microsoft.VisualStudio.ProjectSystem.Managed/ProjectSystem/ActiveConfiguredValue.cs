// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem
{
    [Export(typeof(IActiveConfiguredValue<>))]
    internal class ActiveConfiguredValue<T> : AbstractActiveConfiguredValue<T>, IActiveConfiguredValue<T>
        where T : class?
    {
        [ImportingConstructor]
        public ActiveConfiguredValue(
            UnconfiguredProject project,
            IActiveConfiguredProjectProvider activeConfiguredProjectProvider,
            IProjectThreadingService threadingService)
            : base(project, activeConfiguredProjectProvider, threadingService)
        {
        }

        protected override T GetValue(ConfiguredProject project)
        {
            return project.Services.ExportProvider.GetExportedValueOrDefault<T>()!;
        }
    }
}

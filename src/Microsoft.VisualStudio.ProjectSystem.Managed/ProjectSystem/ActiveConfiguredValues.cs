// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Composition;

namespace Microsoft.VisualStudio.ProjectSystem
{
    [Export(typeof(IActiveConfiguredValues<>))]
    internal class ActiveConfiguredValues<T> :
        AbstractActiveConfiguredValue<object>,  // NOTE: Typed as 'object' because of https://github.com/microsoft/vs-mef/issues/180
        IActiveConfiguredValues<T>
        where T : class
    {
        [ImportingConstructor]
        public ActiveConfiguredValues(
            UnconfiguredProject project,
            IActiveConfiguredProjectProvider activeConfiguredProjectProvider,
            IProjectThreadingService threadingService)
            : base(project, activeConfiguredProjectProvider, threadingService)
        {
        }

        public IEnumerable<Lazy<T>> Values => (IEnumerable<Lazy<T>>)Value;

        protected override object GetValue(ConfiguredProject project)
        {
            // Get the "natural" (unfiltered) export provider so that can we pull all the possible
            // values, not just the ones that are applicable to the current set of capabilities when
            // we call this.
            //
            // This so that when capabilities change over time, the resulting OrderPrecedenceImportCollection
            // responds to the changes and filters the list based on the new set of capabilities.
            //
            // This basically mimics importing OrderPrecedenceImportCollection directly.
            ExportProvider provider = project.Services.ExportProvider.GetExportedValue<ExportProvider>();

            var values = new OrderPrecedenceImportCollection<T>(projectCapabilityCheckProvider: project);
            foreach (Lazy<T, IOrderPrecedenceMetadataView> value in provider.GetExports<T, IOrderPrecedenceMetadataView>())
            {
                values.Add(value);
            }

            return values;
        }
    }
}

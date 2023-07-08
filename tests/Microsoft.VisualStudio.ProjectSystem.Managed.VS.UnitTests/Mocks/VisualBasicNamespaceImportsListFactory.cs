// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.Automation
{
    internal static class VisualBasicNamespaceImportsListFactory
    {
        public static TestDotNetNamespaceImportsList CreateInstance(params string[] list)
        {
            var newList = new TestDotNetNamespaceImportsList(
                UnconfiguredProjectFactory.Create(),
                IProjectThreadingServiceFactory.Create(),
                IActiveConfiguredProjectSubscriptionServiceFactory.Create());

            newList.VSImports = new Lazy<DotNetVSImports>(() => new TestDotNetVsImports(
                Mock.Of<VSLangProj.VSProject>(),
                IProjectThreadingServiceFactory.Create(),
                IActiveConfiguredValueFactory.ImplementValue(()=> ConfiguredProjectFactory.Create()),
                IProjectAccessorFactory.Create(),
                IUnconfiguredProjectVsServicesFactory.Create(),
                newList));

            newList.TestApply(list);

            newList.ImportsAdded.Clear();
            newList.ImportsRemoved.Clear();

            return newList;
        }

        internal class TestDotNetNamespaceImportsList : DotNetNamespaceImportsList
        {
            public TestDotNetNamespaceImportsList(UnconfiguredProject project,
                                                       IProjectThreadingService threadingService,
                                                       IActiveConfiguredProjectSubscriptionService activeConfiguredProjectSubscriptionService)
                : base(project, threadingService, activeConfiguredProjectSubscriptionService)
            {
                SkipInitialization = true;
            }

            public List<string> ImportsAdded { get; } = new List<string>();

            public List<string> ImportsRemoved { get; } = new List<string>();

            internal void TestApply(IProjectSubscriptionUpdate projectSubscriptionUpdate)
            {
                var projectVersionedValue = IProjectVersionedValueFactory.Create(projectSubscriptionUpdate);

                var result = PreprocessAsync(projectVersionedValue, null);

                _ = ApplyAsync(result.Result!);
            }

            internal void TestApply(string[] list)
            {
                _ = ApplyAsync(IProjectVersionedValueFactory.Create(ImmutableList.CreateRange(list)));
            }
        }

        private class TestDotNetVsImports : DotNetVSImports
        {
            private readonly TestDotNetNamespaceImportsList _testImportsList;
            public TestDotNetVsImports(VSLangProj.VSProject vsProject,
                                            IProjectThreadingService threadingService,
                                            IActiveConfiguredValue<ConfiguredProject> activeConfiguredProject,
                                            IProjectAccessor projectAccessor,
                                            IUnconfiguredProjectVsServices unconfiguredProjectVSServices,
                                            TestDotNetNamespaceImportsList importsList)
                : base(vsProject, threadingService, activeConfiguredProject, projectAccessor, unconfiguredProjectVSServices, importsList)
            {
                _testImportsList = importsList;
            }

            internal override void OnImportAdded(string importNamespace)
            {
                _testImportsList.ImportsAdded.Add(importNamespace);
            }

            internal override void OnImportRemoved(string importNamespace)
            {
                _testImportsList.ImportsRemoved.Add(importNamespace);
            }
        }
    }
}

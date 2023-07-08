// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using VSLangProj;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Automation.VisualBasic
{
    public class VsProjectEventsTests
    {
        [Fact]
        public void VSProjectEvents_Properties()
        {
            var referenceEvents = Mock.Of<ReferencesEvents>();
            var buildManagerEvents = Mock.Of<BuildManagerEvents>();

            var projectEventsMock = new Mock<VSProjectEvents>();
            projectEventsMock.Setup(e => e.ReferencesEvents)
                             .Returns(referenceEvents);
            projectEventsMock.Setup(e => e.BuildManagerEvents)
                             .Returns(buildManagerEvents);

            var innerVSProjectMock = new Mock<VSLangProj.VSProject>();
            innerVSProjectMock.Setup(p => p.Events)
                              .Returns(projectEventsMock.Object);

            var unconfiguredProjectMock = new Mock<UnconfiguredProject>();
            unconfiguredProjectMock.SetupGet<IProjectCapabilitiesScope?>(p => p.Capabilities)
                              .Returns((IProjectCapabilitiesScope?)null);

            var importEvents = Mock.Of<ImportsEvents>();
            var importsEventsImpl = new OrderPrecedenceImportCollection<ImportsEvents>(ImportOrderPrecedenceComparer.PreferenceOrder.PreferredComesFirst, (UnconfiguredProject?)null)
            {
                new Lazy<ImportsEvents, IOrderPrecedenceMetadataView>(() => importEvents, IOrderPrecedenceMetadataViewFactory.Create("VisualBasic"))
            };
            var vsProjectEvents = GetVSProjectEvents(innerVSProjectMock.Object, unconfiguredProjectMock.Object);

            vsProjectEvents.SetImportsEventsImpl(importsEventsImpl);

            Assert.NotNull(vsProjectEvents);
            Assert.Equal(referenceEvents, vsProjectEvents.ReferencesEvents);
            Assert.Equal(buildManagerEvents, vsProjectEvents.BuildManagerEvents);
            Assert.Equal(importEvents, vsProjectEvents.ImportsEvents);
        }

        private static VSProjectEventsTestImpl GetVSProjectEvents(VSLangProj.VSProject vsproject, UnconfiguredProject project)
        {
            return new VSProjectEventsTestImpl(vsproject, project);
        }

        internal class VSProjectEventsTestImpl : VisualBasicVSProjectEvents
        {
            public VSProjectEventsTestImpl(VSLangProj.VSProject vsProject, UnconfiguredProject project)
                : base(vsProject, project)
            {
            }

            internal void SetImportsEventsImpl(OrderPrecedenceImportCollection<ImportsEvents> importsEventsImpl)
            {
                ImportsEventsImpl = importsEventsImpl;
            }
        }
    }
}

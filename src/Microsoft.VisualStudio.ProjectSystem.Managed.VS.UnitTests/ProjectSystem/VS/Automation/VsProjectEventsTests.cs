// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

using Moq;

using VSLangProj;

using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Automation
{
    public class VsProjectEventsTests
    {
        [Fact]
        public void VSProjectEvents_Properties()
        {
            var referenceEvents = Mock.Of<ReferencesEvents>();

            var projectEventsMock = new Mock<VSLangProj.VSProjectEvents>();
            projectEventsMock.Setup(e => e.ReferencesEvents)
                             .Returns(referenceEvents);

            var innerVSProjectMock = new Mock<VSLangProj.VSProject>();
            innerVSProjectMock.Setup(p => p.Events)
                              .Returns(projectEventsMock.Object);

            var unconfiguredProjectMock = new Mock<UnconfiguredProject>();
            unconfiguredProjectMock.Setup(p => p.Capabilities)
                                   .Returns((IProjectCapabilitiesScope)null);

            var buildManagerMock = new Mock<BuildManagerEvents>().As<BuildManager>();

            var importEvents = Mock.Of<ImportsEvents>();
            var importsEventsImpl = new OrderPrecedenceImportCollection<ImportsEvents>(ImportOrderPrecedenceComparer.PreferenceOrder.PreferredComesFirst, (UnconfiguredProject)null)
            {
                new Lazy<ImportsEvents, IOrderPrecedenceMetadataView>(() => importEvents, IOrderPrecedenceMetadataViewFactory.Create("VisualBasic"))
            };
            var vsProjectEvents = GetVSProjectEvents(innerVSProjectMock.Object, unconfiguredProjectMock.Object, buildManagerMock.Object);

            vsProjectEvents.SetImportsEventsImpl(importsEventsImpl);

            Assert.NotNull(vsProjectEvents);
            Assert.Equal(referenceEvents, vsProjectEvents.ReferencesEvents);
            Assert.Equal((BuildManagerEvents)buildManagerMock.Object, vsProjectEvents.BuildManagerEvents);
            Assert.Equal(importEvents, vsProjectEvents.ImportsEvents);
        }

        private VSProjectEventsTestImpl GetVSProjectEvents(
            VSLangProj.VSProject vsproject = null,
            UnconfiguredProject project = null,
            BuildManager buildManager = null)
        {
            return new VSProjectEventsTestImpl(vsproject, project, buildManager);
        }

        internal class VSProjectEventsTestImpl : VSProjectEvents
        {
            public VSProjectEventsTestImpl(VSLangProj.VSProject vsProject, UnconfiguredProject project, BuildManager buildManager)
                : base(vsProject, project, buildManager)
            {
            }

            internal void SetImportsEventsImpl(OrderPrecedenceImportCollection<ImportsEvents> importsEventsImpl)
            {
                ImportsEventsImpl = importsEventsImpl;
            }
        }
    }
}

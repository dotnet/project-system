// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Moq;
using VSLangProj;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Automation
{
    [Trait("UnitTest", "ProjectSystem")]
    public class VsProjectEventsTests
    {
        [Fact]
        public void Constructor_VSProjectAsNull_ThrowsException()
        {
            Assert.Throws<ArgumentNullException>("vsProject", () =>
            {
                GetVSProjectEvents();
            });
        }

        [Fact]
        public void Constructor_UnconfiguredProjectAsNull_ThrowsException()
        {
            Assert.Throws<ArgumentNullException>("project", () =>
            {
                GetVSProjectEvents(Mock.Of<VSLangProj.VSProject>());
            });
        }

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
            unconfiguredProjectMock.Setup(p => p.Capabilities)
                                   .Returns((IProjectCapabilitiesScope)null);

            var importEvents = Mock.Of<ImportsEvents>();
            var importsEventsImpl = new OrderPrecedenceImportCollection<ImportsEvents>(ImportOrderPrecedenceComparer.PreferenceOrder.PreferredComesFirst, (UnconfiguredProject)null)
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

        private VSProjectEventsTestImpl GetVSProjectEvents(
            VSLangProj.VSProject vsproject = null,
            UnconfiguredProject project = null)
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

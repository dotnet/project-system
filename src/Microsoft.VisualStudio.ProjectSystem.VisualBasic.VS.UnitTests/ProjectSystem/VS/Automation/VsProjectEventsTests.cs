// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Moq;
using VSLangProj;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Automation
{
    [ProjectSystemTrait]
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
        public void Constructor_ImportsEventsAsNull_ThrowsException()
        {
            Assert.Throws<ArgumentNullException>("importsEvents", () =>
            {
                GetVSProjectEvents(Mock.Of<VSLangProj.VSProject>());
            });
        }

        [Fact]
        public void VSProjectEvents_Properties()
        {
            var referenceEvents = Mock.Of<ReferencesEvents>();
            var buildManagerEvents = Mock.Of<BuildManagerEvents>();
            var importEvents = Mock.Of<ImportsEvents>();

            var projectEventsMock = new Mock<VSLangProj.VSProjectEvents>();
            projectEventsMock.Setup(e => e.ReferencesEvents)
                             .Returns(referenceEvents);
            projectEventsMock.Setup(e => e.BuildManagerEvents)
                             .Returns(buildManagerEvents);

            var innerVSProjectMock = new Mock<VSLangProj.VSProject>();
            innerVSProjectMock.Setup(p => p.Events)
                              .Returns(projectEventsMock.Object);

            var vsProjectEvents = GetVSProjectEvents(innerVSProjectMock.Object, importEvents);

            Assert.NotNull(vsProjectEvents);
            Assert.Equal(referenceEvents, vsProjectEvents.ReferencesEvents);
            Assert.Equal(buildManagerEvents, vsProjectEvents.BuildManagerEvents);
            Assert.Equal(importEvents, vsProjectEvents.ImportsEvents);
        }

        private VSProjectEvents GetVSProjectEvents(
            VSLangProj.VSProject vsproject = null,
            ImportsEvents events = null)
        {
            return new VSProjectEvents(vsproject, events);
        }
    }
}

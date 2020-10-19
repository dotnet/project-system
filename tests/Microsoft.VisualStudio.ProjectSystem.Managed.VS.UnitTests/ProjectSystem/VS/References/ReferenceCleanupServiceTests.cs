// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using EnvDTE;
using Microsoft.Build.Exceptions;
using Microsoft.VisualStudio.Shell.Interop;
using Moq;
using Xunit;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.References;
using Microsoft.VisualStudio.ProjectSystem.VS.References.Roslyn;

namespace Microsoft.VisualStudio.ProjectSystem.VS.References
{
    public class ReferenceCleanupServiceTests
    {
        private const string _projectPath1 = "C:\\Dev\\Solution\\Project\\Project1.csproj";
        private const string _projectPath2 = "C:\\Dev\\Solution\\Project\\Project2.csproj";
        private const string _projectPath3 = "C:\\Dev\\Solution\\Project\\Project3.csproj";

        private const string _package1 = "package1";
        private const string _package2 = "package2";
        private const string _package3 = "package3";

        private const string _assembly1 = "assembly1";
        private const string _assembly2 = "assembly2";
        private const string _assembly3 = "assembly3";

        private const string _sdk1 = "sdk1";
        private const string _sdk2 = "sdk2";
        private const string _sdk3 = "sdk3";

        private Mock<ConfiguredProject>? _configuredProjectMock1;
        private Mock<ConfiguredProject>? _configuredProjectMock2;
        private Mock<ConfiguredProject>? _configuredProjectMock3;

        [Fact]
        public async Task GetProjectReferencesAsync_NoValidProjectFound_ThrowsException()
        {
            var referenceCleanupService = Setup();

            await Assert.ThrowsAsync<InvalidProjectFileException>(() =>
                referenceCleanupService.GetProjectReferencesAsync("UnknownProject", "", CancellationToken.None)
                );
        }

        [Fact]
        public async Task GetProjectReferencesAsync_FoundZeroReferences_ReturnAllReferences()
        {
            var referenceCleanupService = Setup();

            var references = await referenceCleanupService.GetProjectReferencesAsync(_projectPath2, "", CancellationToken.None);

            Assert.Empty(references);
        }

        [Fact]
        public async Task GetProjectReferencesAsync_FoundReferences_ReturnAllReferences()
        {
            var referenceCleanupService = Setup();

            var references = await referenceCleanupService.GetProjectReferencesAsync(_projectPath1, "", CancellationToken.None);

            Assert.Equal(7, references.Length);
        }

        [Fact]
        public async Task UpdateReferencesAsync_UpdatePackages_NoAction()
        {
            var referenceCleanupService = Setup();

            var referenceUpdate1 =
                new ProjectSystemReferenceUpdate(ProjectSystemUpdateAction.None, new ProjectSystemReferenceInfo(ProjectSystemReferenceType.Package, "", true));

            await referenceCleanupService.TryUpdateReferenceAsync(_projectPath1, "", referenceUpdate1, CancellationToken.None);

            _configuredProjectMock1?.Verify(c => c.Services.PackageReferences.RemoveAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task UpdateReferencesAsync_RemovePackages_RemoveExecutedTwice()
        {
            var referenceCleanupService = Setup();

            var referenceUpdate1 =
                new ProjectSystemReferenceUpdate(ProjectSystemUpdateAction.Remove, new ProjectSystemReferenceInfo(ProjectSystemReferenceType.Package, _package1, true));
            var referenceUpdate2 =
                new ProjectSystemReferenceUpdate(ProjectSystemUpdateAction.Remove, new ProjectSystemReferenceInfo(ProjectSystemReferenceType.Package, _package2, true));
            
            await referenceCleanupService.TryUpdateReferenceAsync(_projectPath1, "", referenceUpdate1, CancellationToken.None);

            _configuredProjectMock1?.Verify(c=> c.Services.PackageReferences.RemoveAsync(It.IsAny<string>()), Times.Once);

            await referenceCleanupService.TryUpdateReferenceAsync(_projectPath1, "", referenceUpdate2, CancellationToken.None);
            _configuredProjectMock1?.Verify(c => c.Services.PackageReferences.RemoveAsync(It.IsAny<string>()), Times.Exactly(2));
        }

        private ReferenceCleanupService Setup()
        {
            CreateReferences(out var projectItems, out var packageItems, out var assemblyItems, out var sdkItems);

            var itemMock = createSnapshots(projectItems, packageItems, assemblyItems, sdkItems);
            CreateEmptyReferences(out var projectItems2, out var packageItems2, out var assemblyItems2, out var sdkItems2);
            var itemEmtpyMock = createSnapshots(projectItems2, packageItems2, assemblyItems2, sdkItems2);

            var configuredProjectMock = new Mock<ConfiguredProject>();
            configuredProjectMock.SetupGet(c => c.UnconfiguredProject).Returns(UnconfiguredProjectFactory.Create(fullPath: _projectPath1));

            var configuredProjectServicesMock = new Mock<ConfiguredProjectServices>();
            var projectServiceMock = new Mock<IProjectService>();

            configuredProjectServicesMock.SetupGet(c => c.ProjectService).Returns(projectServiceMock.Object);

            var unconfiguredProjectMock1 = CreateUnconfiguredProjectMock(_projectPath1, itemMock.Object, out _configuredProjectMock1);
            var unconfiguredProjectMock2 = CreateUnconfiguredProjectMock(_projectPath2, itemEmtpyMock.Object, out _configuredProjectMock2);
            var unconfiguredProjectMock3 = CreateUnconfiguredProjectMock(_projectPath3, itemEmtpyMock.Object, out _configuredProjectMock3);

            UnconfiguredProject[] unconfiguredProjects = new UnconfiguredProject[3]
            {
                unconfiguredProjectMock1.Object, unconfiguredProjectMock2.Object, unconfiguredProjectMock3.Object
            };

            projectServiceMock.SetupGet(c => c.LoadedUnconfiguredProjects).Returns(unconfiguredProjects);
            configuredProjectMock.SetupGet(c => c.Services).Returns(configuredProjectServicesMock.Object);

            var dteMock = new Mock<IVsUIService<SDTE, DTE>>();
            var solutionMock = new Mock<IVsUIService<SVsSolution, IVsSolution>>();

            return new ReferenceCleanupService(configuredProjectMock.Object, dteMock.Object, solutionMock.Object);
        }

        private static Mock<IProjectVersionedValue<IProjectSubscriptionUpdate>> createSnapshots(IImmutableDictionary<string, IImmutableDictionary<string, string>> projectItems, IImmutableDictionary<string, IImmutableDictionary<string, string>> packageItems,
            IImmutableDictionary<string, IImmutableDictionary<string, string>> assemblyItems, IImmutableDictionary<string, IImmutableDictionary<string, string>> sdkItems)
        {
            var itemMock = new Mock<IProjectVersionedValue<IProjectSubscriptionUpdate>>();
            var itemValue = new Mock<IProjectSubscriptionUpdate>();

            var projectRuleSnapshot = new Mock<IProjectRuleSnapshot>();
            projectRuleSnapshot.SetupGet(c => c.Items).Returns(projectItems);

            var packageRuleSnapshot = new Mock<IProjectRuleSnapshot>();
            packageRuleSnapshot.SetupGet(c => c.Items).Returns(packageItems);

            var assemblyRuleSnapshot = new Mock<IProjectRuleSnapshot>();
            assemblyRuleSnapshot.SetupGet(c => c.Items).Returns(assemblyItems);

            var sdkRuleSnapshot = new Mock<IProjectRuleSnapshot>();
            sdkRuleSnapshot.SetupGet(c => c.Items).Returns(sdkItems);

            IImmutableDictionary<string, IProjectRuleSnapshot> currentState =
                new Dictionary<string, IProjectRuleSnapshot>
                    {
                        {ProjectReference.SchemaName, projectRuleSnapshot.Object},
                        {PackageReference.SchemaName, packageRuleSnapshot.Object},
                        {AssemblyReference.SchemaName, assemblyRuleSnapshot.Object},
                        {SdkReference.SchemaName, sdkRuleSnapshot.Object}
                    }
                    .ToImmutableDictionary();

            itemValue.SetupGet(c => c.CurrentState).Returns(currentState);
            itemMock.Setup(c => c.Value).Returns(itemValue.Object);
            return itemMock;
        }

        private static void CreateReferences(out IImmutableDictionary<string, IImmutableDictionary<string, string>> projectItems,
            out IImmutableDictionary<string, IImmutableDictionary<string, string>> packageItems,
            out IImmutableDictionary<string, IImmutableDictionary<string, string>> assemblyItems, out IImmutableDictionary<string, IImmutableDictionary<string, string>> sdkItems)
        {
            projectItems = new Dictionary<string, IImmutableDictionary<string, string>>
            {
                {
                    _projectPath2,
                    new Dictionary<string, string> {{"TreatAsUsed", "True"}, {"Identity", _projectPath2}}
                        .ToImmutableDictionary()
                },
                {
                    _projectPath3,
                    new Dictionary<string, string> {{"TreatAsUsed", "True"}, {"Identity", _projectPath3}}
                        .ToImmutableDictionary()
                }
            }.ToImmutableDictionary();

            packageItems = new Dictionary<string, IImmutableDictionary<string, string>>
            {
                {
                    _package1,
                    new Dictionary<string, string> {{"TreatAsUsed", "True"}, {"Name", _package1}}.ToImmutableDictionary()
                },
                {
                    _package2,
                    new Dictionary<string, string> {{"TreatAsUsed", "False"}, {"Name", _package2}}.ToImmutableDictionary()
                },
                {
                    _package3,
                    new Dictionary<string, string> {{"TreatAsUsed", "False"}, {"Name", _package3}}.ToImmutableDictionary()
                }
            }.ToImmutableDictionary();

            assemblyItems = new Dictionary<string, IImmutableDictionary<string, string>>
            {
                {
                    _assembly1,
                    new Dictionary<string, string> {{"TreatAsUsed", "True"}, {"SDKName", _assembly1}}.ToImmutableDictionary()
                }
            }.ToImmutableDictionary();

            sdkItems = new Dictionary<string, IImmutableDictionary<string, string>>
            {
                {_sdk1, new Dictionary<string, string> {{"TreatAsUsed", "True"}, {"Name", _sdk1}}.ToImmutableDictionary()}
            }.ToImmutableDictionary();
        }

        private static void CreateEmptyReferences(out IImmutableDictionary<string, IImmutableDictionary<string, string>> projectItems,
            out IImmutableDictionary<string, IImmutableDictionary<string, string>> packageItems,
            out IImmutableDictionary<string, IImmutableDictionary<string, string>> assemblyItems, out IImmutableDictionary<string, IImmutableDictionary<string, string>> sdkItems)
        {
            projectItems = new Dictionary<string, IImmutableDictionary<string, string>>
            {
            }.ToImmutableDictionary();

            packageItems = new Dictionary<string, IImmutableDictionary<string, string>>
            {
            }.ToImmutableDictionary();

            assemblyItems = new Dictionary<string, IImmutableDictionary<string, string>>
            {
            }.ToImmutableDictionary();

            sdkItems = new Dictionary<string, IImmutableDictionary<string, string>>
            {
            }.ToImmutableDictionary();
        }

        private static Mock<UnconfiguredProject> CreateUnconfiguredProjectMock(string projectPath, IProjectVersionedValue<IProjectSubscriptionUpdate> item, out Mock<ConfiguredProject> configuredProjectMock)
        {
            Predicate<IProjectVersionedValue<IProjectSubscriptionUpdate>>? predicate = null;

            var receivableSourceBlock2 = new ReceivableSourceBlockMock(item);

            var receivableSourceBlock = new Mock<IReceivableSourceBlock<IProjectVersionedValue<IProjectSubscriptionUpdate>>>();
            receivableSourceBlock.Setup(c => c.TryReceive(predicate, out item));

            var projectRuleSourceMock = new Mock<IProjectValueDataSource<IProjectSubscriptionUpdate>>();
            projectRuleSourceMock.SetupGet(c => c.SourceBlock).Returns(receivableSourceBlock2);

            var projectSubscriptionMock = new Mock<IProjectSubscriptionService>();
            projectSubscriptionMock.SetupGet(c => c.ProjectRuleSource).Returns(projectRuleSourceMock.Object);

            var configuredProjectServicesMock = new Mock<ConfiguredProjectServices>();
            configuredProjectServicesMock.SetupGet(c => c.ProjectSubscription).Returns(projectSubscriptionMock.Object);

            var packageReferenceServicesMock = new Mock<IPackageReferencesService>();
            packageReferenceServicesMock.Setup(c => c.RemoveAsync(It.IsAny<string>()));
            configuredProjectServicesMock.SetupGet(c => c.PackageReferences)
                .Returns(packageReferenceServicesMock.Object);

            var projectReferenceServicesMock = new Mock<IBuildDependencyProjectReferencesService>();
            projectReferenceServicesMock.Setup(c => c.RemoveAsync(It.IsAny<string>()));
            configuredProjectServicesMock.SetupGet(c => c.ProjectReferences)
                .Returns(projectReferenceServicesMock.Object);

            var assemblyReferenceServicesMock = new Mock<IAssemblyReferencesService>();
            assemblyReferenceServicesMock.Setup(c => c.RemoveAsync(null, It.IsAny<string>()));
            configuredProjectServicesMock.SetupGet(c => c.AssemblyReferences)
                .Returns(assemblyReferenceServicesMock.Object);

            var sdkReferenceServicesMock = new Mock<ISdkReferencesService>();
            assemblyReferenceServicesMock.Setup(c => c.RemoveAsync(null, It.IsAny<string>()));
            configuredProjectServicesMock.SetupGet(c => c.SdkReferences)
                .Returns(sdkReferenceServicesMock.Object);

            configuredProjectMock = new Mock<ConfiguredProject>();
            configuredProjectMock.SetupGet(c => c.Services).Returns(configuredProjectServicesMock.Object);

            var unconfiguredProjectMock = new Mock<UnconfiguredProject>();
            unconfiguredProjectMock.Setup(c => c.GetSuggestedConfiguredProjectAsync())
                .ReturnsAsync(configuredProjectMock.Object);
            unconfiguredProjectMock.SetupGet(c => c.FullPath).Returns(projectPath);

            return unconfiguredProjectMock;
        }

        private class ReceivableSourceBlockMock : IReceivableSourceBlock<IProjectVersionedValue<IProjectSubscriptionUpdate>>
        {
            private readonly IProjectVersionedValue<IProjectSubscriptionUpdate> _item;

            public ReceivableSourceBlockMock(IProjectVersionedValue<IProjectSubscriptionUpdate> item)
            {
                _item = item;
            }

            public void Complete() => throw new NotImplementedException();

            public void Fault(Exception exception) => throw new NotImplementedException();

            public Task Completion { get; }

            public IDisposable LinkTo(ITargetBlock<IProjectVersionedValue<IProjectSubscriptionUpdate>> target,
                DataflowLinkOptions linkOptions) => throw new NotImplementedException();

            public IProjectVersionedValue<IProjectSubscriptionUpdate> ConsumeMessage(DataflowMessageHeader messageHeader, ITargetBlock<IProjectVersionedValue<IProjectSubscriptionUpdate>> target,
                out bool messageConsumed) => throw new NotImplementedException();

            public bool ReserveMessage(DataflowMessageHeader messageHeader,
                ITargetBlock<IProjectVersionedValue<IProjectSubscriptionUpdate>> target) => throw new NotImplementedException();


            public void ReleaseReservation(DataflowMessageHeader messageHeader,
                ITargetBlock<IProjectVersionedValue<IProjectSubscriptionUpdate>> target) => throw new NotImplementedException();


            public bool TryReceive(Predicate<IProjectVersionedValue<IProjectSubscriptionUpdate>> filter, out IProjectVersionedValue<IProjectSubscriptionUpdate> item)
            {
                item = _item;
                return true;
            }

            public bool TryReceiveAll(out IList<IProjectVersionedValue<IProjectSubscriptionUpdate>> items) => throw new NotImplementedException();
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Build.Exceptions;
using Microsoft.VisualStudio.LanguageServices.ExternalAccess.ProjectSystem.Api;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.References;

namespace Microsoft.VisualStudio.ProjectSystem.VS.References
{
    public class ReferenceCleanupServiceTests
    {
        private const string _projectPath1 = "C:\\Dev\\Solution\\Project\\Project1.csproj";
        private const string _projectPath2 = "C:\\Dev\\Solution\\Project\\Project2.csproj";
        private const string _projectPath3 = "C:\\Dev\\Solution\\Project\\Project3.csproj";
        private const string _package3 = "package3";

        private const string _assembly1 = "assembly1";
        private const string _assembly2 = "assembly2";
        private const string _assembly3 = "assembly3";

        private static Mock<IUnresolvedPackageReference>? s_item;

        private static Mock<IPackageReferencesService>? _packageServicesMock1;
        private static Mock<IAssemblyReferencesService>? _assemblyServicesMock1;

        private static Mock<IPackageReferencesService>? _packageServicesMock2;
        private static Mock<IAssemblyReferencesService>? _assemblyServicesMock2;

        private static Mock<IPackageReferencesService>? _packageServicesMock3;
        private static Mock<IAssemblyReferencesService>? _assemblyServicesMock3;

        [Fact]
        public async Task GetProjectReferencesAsync_NoValidProjectFound_ThrowsException()
        {
            var referenceCleanupService = Setup();

            await Assert.ThrowsAsync<InvalidProjectFileException>(() =>
                referenceCleanupService.GetProjectReferencesAsync("UnknownProject", CancellationToken.None)
                );
        }

        [Fact]
        public async Task GetProjectReferencesAsync_FoundZeroReferences_ReturnAllReferences()
        {
            var referenceCleanupService = Setup();

            var references = await referenceCleanupService.GetProjectReferencesAsync(_projectPath3, CancellationToken.None);

            Assert.Empty(references);
        }

        [Theory]
        [InlineData(_projectPath1, 5)]
        [InlineData(_projectPath2, 4)]
        [InlineData(_projectPath3, 0)]
        public async Task GetProjectReferencesAsync_FoundReferences_ReturnAllReferences(string projectPath, int numberOfReferences)
        {
            var referenceCleanupService = Setup();

            var references = await referenceCleanupService.GetProjectReferencesAsync(projectPath, CancellationToken.None);

            Assert.Equal(numberOfReferences, references.Length);
        }

        [Fact]
        public async Task UpdateReferenceAsync_TryUpdateReferenceAsync_ShouldThrowException()
        {
            var referenceCleanupService = Setup();
            var referenceUpdate1 =
                new ProjectSystemReferenceUpdate(ProjectSystemUpdateAction.Remove, new ProjectSystemReferenceInfo(ProjectSystemReferenceType.Package, _package3, true));

            await Assert.ThrowsAsync<NotImplementedException>(() =>
                referenceCleanupService.TryUpdateReferenceAsync(_projectPath1, referenceUpdate1, CancellationToken.None)
                );
        }

        [Fact]
        public async Task UpdateReferencesAsync_GetUpdateReferenceOperationAsync_CreateOperation()
        {
            var referenceCleanupService = Setup();
            var referenceUpdate1 =
                new ProjectSystemReferenceUpdate(ProjectSystemUpdateAction.Remove, new ProjectSystemReferenceInfo(ProjectSystemReferenceType.Package, "UnknownPackage", true));

            var operation = await referenceCleanupService.GetUpdateReferenceOperationAsync(_projectPath1, referenceUpdate1, CancellationToken.None);

            Assert.NotNull(operation);
        }

        [Fact]
        public async Task UpdateReferencesAsync_GetUpdateReferenceOperationAsync_FailedToCreateOperation()
        {
            var referenceCleanupService = Setup();
            var referenceUpdate1 =
                new ProjectSystemReferenceUpdate((ProjectSystemUpdateAction)3, new ProjectSystemReferenceInfo(ProjectSystemReferenceType.Package, "UnknownPackage", true));

            var operation = await referenceCleanupService.GetUpdateReferenceOperationAsync(_projectPath1, referenceUpdate1, CancellationToken.None);

            Assert.Equal(ReferenceCleanupService.NullCommand, operation);
        }

        private ReferenceCleanupService Setup()
        {
            var projectServiceAccessorMock = new Mock<IProjectServiceAccessor>();

            var projectServiceMock = new Mock<IProjectService2>();
            AddLoadedProject(_projectPath1, CreateConfiguredProjectServicesForProject1, projectServiceMock);
            AddLoadedProject(_projectPath2, CreateConfiguredProjectServicesForProject2, projectServiceMock);
            AddLoadedProject(_projectPath3, CreateConfiguredProjectServicesForProject3, projectServiceMock);

            projectServiceAccessorMock.Setup(c => c.GetProjectService(ProjectServiceThreadingModel.Multithreaded)).Returns(projectServiceMock.Object);
            return new ReferenceCleanupService(projectServiceAccessorMock.Object);
        }

        private void AddLoadedProject(string projectPath, Func<ConfiguredProjectServices> createConfiguredProjectServicesForProject, Mock<IProjectService2> projectServiceMock)
        {
            var configuredProject = ConfiguredProjectFactory.Create(services: createConfiguredProjectServicesForProject());
            var unconfiguredProject = UnconfiguredProjectFactory.Create(fullPath: projectPath, configuredProject: configuredProject);
            projectServiceMock.Setup(c => c.GetLoadedProject(It.Is<string>(arg => arg == projectPath))).Returns(unconfiguredProject);
        }

        private static ConfiguredProjectServices CreateConfiguredProjectServicesForProject1()
        {
            return createConfiguredProjectServicesForProject(new List<(string, string)>
            {
                (_projectPath2, PropertySerializer.SimpleTypes.ToString(false)),
                (_projectPath3, PropertySerializer.SimpleTypes.ToString(false))
            }, new List<(string, string)>
            {
                (_package3 , PropertySerializer.SimpleTypes.ToString(false))
            }, new List<(string, string)>
            {
                (_assembly1, PropertySerializer.SimpleTypes.ToString(false)),
                (_assembly2, PropertySerializer.SimpleTypes.ToString(false))
            },
                out _packageServicesMock1, out _assemblyServicesMock1);
        }

        private static ConfiguredProjectServices CreateConfiguredProjectServicesForProject2()
        {
            return createConfiguredProjectServicesForProject(
                new List<(string, string)>
                {
                    (_projectPath3, PropertySerializer.SimpleTypes.ToString(true))
                },
                new List<(string, string)> { },
                new List<(string, string)>
                {
                    (_assembly1, PropertySerializer.SimpleTypes.ToString(true)),
                    (_assembly2, PropertySerializer.SimpleTypes.ToString(true)),
                    (_assembly3, PropertySerializer.SimpleTypes.ToString(true))
                },
                    out _packageServicesMock2, out _assemblyServicesMock2);
        }

        private static ConfiguredProjectServices CreateConfiguredProjectServicesForProject3()
        {
            return createConfiguredProjectServicesForProject(
                new List<(string, string)> { },
                new List<(string, string)> { },
                new List<(string, string)> { },
                out _packageServicesMock3, out _assemblyServicesMock3);
        }

        private static ConfiguredProjectServices createConfiguredProjectServicesForProject(
            List<(string, string)> projects, List<(string, string)> packages, List<(string, string)> assemblies,
            out Mock<IPackageReferencesService> packageServicesMock,
            out Mock<IAssemblyReferencesService> assemblyServiceMock)
        {
            var projectReferencesService = new Mock<IBuildDependencyProjectReferencesService>();
            IImmutableSet<IUnresolvedBuildDependencyProjectReference> unresolvedProjectReferences =
                CreateReferences<IUnresolvedBuildDependencyProjectReference>(projects);
            projectReferencesService.Setup(c => c.GetUnresolvedReferencesAsync()).ReturnsAsync(unresolvedProjectReferences);

            var packageReferencesService = new Mock<IPackageReferencesService>();
            IImmutableSet<IUnresolvedPackageReference> unresolvedPackageReferences = CreateReferences<IUnresolvedPackageReference>(packages);
            packageReferencesService.Setup(c => c.GetUnresolvedReferencesAsync()).ReturnsAsync(unresolvedPackageReferences);
            packageServicesMock = packageReferencesService;

            var assemblyReferencesService = new Mock<IAssemblyReferencesService>();
            IImmutableSet<IUnresolvedAssemblyReference> unresolvedAssemblyReferences =
                CreateReferences<IUnresolvedAssemblyReference>(assemblies);
            assemblyReferencesService.Setup(c => c.GetUnresolvedReferencesAsync()).ReturnsAsync(unresolvedAssemblyReferences);
            assemblyServiceMock = assemblyReferencesService;

            var configuredProjectServices = ConfiguredProjectServicesFactory.Create(
                projectReferences: projectReferencesService.Object, packageReferences: packageReferencesService.Object,
                assemblyReferences: assemblyReferencesService.Object);

            return configuredProjectServices;
        }

        private static IImmutableSet<T> CreateReferences<T>(List<(string, string)> assemblies)
            where T : class, IProjectItem
        {
            ISet<T> references = new HashSet<T>();

            foreach ((string include, string evaluatedValue) in assemblies)
            {
                var item = new Mock<T>();
                item.Setup(c => c.EvaluatedInclude).Returns(include);
                item.As<IProjectItem>().Setup(c => c.Metadata.GetEvaluatedPropertyValueAsync(ProjectReference.TreatAsUsedProperty))
                    .ReturnsAsync(evaluatedValue);

                if (s_item is null && item is Mock<IUnresolvedPackageReference> packageItem)
                {
                    s_item = packageItem;
                }
                references.Add(item.Object);
            }

            return references.ToImmutableHashSet();
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.PackageRestore;
using NuGet.SolutionRestoreManager;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PackageRestore;

public class NuGetRestoreServiceTests
{
    [Fact]
    public async Task NominateAsyncCallsThroughToNuGetNominate()
    {
        bool nominateCalled = false;

        var project = UnconfiguredProjectFactory.Create(fullPath: @"C:\Test\Test.csproj");
        var vsNuGetSolutionRestoreService = new IVsSolutionRestoreServiceFactory().WithNominateProjectAsync((path, info, ct) => nominateCalled = true).Build();
        var projectAsynchronousTasksService = IProjectAsynchronousTasksServiceFactory.Create();
        var faultHandlerService = IProjectFaultHandlerServiceFactory.Create();
        var restoreService = new NuGetRestoreService(project, vsNuGetSolutionRestoreService, projectAsynchronousTasksService, faultHandlerService);

        var restoreInfo = ProjectRestoreInfoFactory.Create(msbuildProjectExtensionsPath: @"C:\Alpha\Beta");
        var configuredInputs = PackageRestoreConfiguredInputFactory.Create(restoreInfo);

        var result = await restoreService.NominateAsync(restoreInfo, configuredInputs, default);

        Assert.True(nominateCalled);
    }

    [Fact]
    public async Task UpdateDoesNotCallThroughToNuGetNominate()
    {
        bool nominateCalled = false;

        var project = UnconfiguredProjectFactory.Create(fullPath: @"C:\Test\Test.csproj");
        var vsNuGetSolutionRestoreService = new IVsSolutionRestoreServiceFactory().WithNominateProjectAsync((path, info, ct) => nominateCalled = true).Build();
        var projectAsynchronousTasksService = IProjectAsynchronousTasksServiceFactory.Create();
        var faultHandlerService = IProjectFaultHandlerServiceFactory.Create();
        var restoreService = new NuGetRestoreService(project, vsNuGetSolutionRestoreService, projectAsynchronousTasksService, faultHandlerService);

        var restoreInfo = ProjectRestoreInfoFactory.Create(msbuildProjectExtensionsPath: @"C:\Alpha\Beta");
        var configuredInputs = PackageRestoreConfiguredInputFactory.Create(restoreInfo);

        await restoreService.UpdateWithoutNominationAsync(configuredInputs);

        Assert.False(nominateCalled);
    }

    [Fact]
    public async Task NominateCausesPendingTaskToComplete()
    {
        IVsProjectRestoreInfoSource? restoreSource = null;
        Task? faultHandlerRegisteredTask = null;

        var configuredProject = ConfiguredProjectFactory.Create(projectConfiguration: ProjectConfigurationFactory.Create("Debug|x64"));
        var project = UnconfiguredProjectFactory.Create(fullPath: @"C:\Test\Test.csproj", configuredProject: configuredProject);
        var vsNuGetSolutionRestoreService = new IVsSolutionRestoreServiceFactory().WithRegisterRestoreInfoSourceAsync((source, ct) => restoreSource = source).Build();
        var projectAsynchronousTasksService = IProjectAsynchronousTasksServiceFactory.Create();
        var faultHandlerService = IProjectFaultHandlerServiceFactory.ImplementForget((task, settings, severity, project) => faultHandlerRegisteredTask = task);
        var restoreService = new NuGetRestoreService(project, vsNuGetSolutionRestoreService, projectAsynchronousTasksService, faultHandlerService);

        await restoreService.LoadAsync();

        Assert.NotNull(faultHandlerRegisteredTask);

        await faultHandlerRegisteredTask;

        Assert.NotNull(restoreSource);

        Task nominationTask = restoreSource.WhenNominated(default);
        Assert.False(nominationTask.IsCompleted);

        var restoreInfo = ProjectRestoreInfoFactory.Create(msbuildProjectExtensionsPath: @"C:\Alpha\Beta");
        var configuredInputs = PackageRestoreConfiguredInputFactory.Create(restoreInfo);

        await restoreService.NominateAsync(restoreInfo, configuredInputs, default);

        Assert.True(nominationTask.IsCompleted);
    }

    [Fact]
    public async Task UpdateCausesPendingTaskToComplete()
    {
        IVsProjectRestoreInfoSource? restoreSource = null;
        Task? faultHandlerRegisteredTask = null;

        var configuredProject = ConfiguredProjectFactory.Create(projectConfiguration: ProjectConfigurationFactory.Create("Debug|x64"));
        var project = UnconfiguredProjectFactory.Create(fullPath: @"C:\Test\Test.csproj", configuredProject: configuredProject);
        var vsNuGetSolutionRestoreService = new IVsSolutionRestoreServiceFactory().WithRegisterRestoreInfoSourceAsync((source, ct) => restoreSource = source).Build();
        var projectAsynchronousTasksService = IProjectAsynchronousTasksServiceFactory.Create();
        var faultHandlerService = IProjectFaultHandlerServiceFactory.ImplementForget((task, settings, severity, project) => faultHandlerRegisteredTask = task);
        var restoreService = new NuGetRestoreService(project, vsNuGetSolutionRestoreService, projectAsynchronousTasksService, faultHandlerService);

        await restoreService.LoadAsync();

        Assert.NotNull(faultHandlerRegisteredTask);

        await faultHandlerRegisteredTask;

        Assert.NotNull(restoreSource);

        Task nominationTask = restoreSource.WhenNominated(default);
        Assert.False(nominationTask.IsCompleted);

        var restoreInfo = ProjectRestoreInfoFactory.Create(msbuildProjectExtensionsPath: @"C:\Alpha\Beta");
        var configuredInputs = PackageRestoreConfiguredInputFactory.Create(restoreInfo);

        await restoreService.UpdateWithoutNominationAsync(configuredInputs);

        Assert.True(nominationTask.IsCompleted);
    }
}

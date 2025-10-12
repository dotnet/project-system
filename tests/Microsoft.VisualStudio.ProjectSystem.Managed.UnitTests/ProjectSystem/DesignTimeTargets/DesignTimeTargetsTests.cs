// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Utilities;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Framework;

namespace Microsoft.VisualStudio.ProjectSystem.DesignTimeTargets;

public sealed class DesignTimeTargetsTests
{
    [Theory]
    [InlineData("Microsoft.CSharp.DesignTime.targets")]
    [InlineData("Microsoft.VisualBasic.DesignTime.targets")]
    [InlineData("Microsoft.FSharp.DesignTime.targets")]
    public void ValidateDesignTimeTargetsEvaluateAndBuild(string targetFileName)
    {
        // eg: src\Microsoft.VisualStudio.ProjectSystem.Managed\ProjectSystem\DesignTimeTargets\Microsoft.CSharp.DesignTime.targets

        string path = Path.Combine(RepoUtil.FindRepoRootPath(), "src", "Microsoft.VisualStudio.ProjectSystem.Managed", "ProjectSystem", "DesignTimeTargets", targetFileName);

        // Force an evaluation of the project.
        Project project = new(path);

        Logger logger = new();

        // Build a target. This isn't a particularly interesting target, but it's one of the few
        // that don't require a target defined elsewhere. If we were to try and build all targets
        // in the project (via project.Targets.Keys) we would hit error MSB4057 (target not found)
        // because we are not importing the common targets, etc. here. This is just a smoke test
        // to make sure we can build a target.
        project.Build(["CollectPackageReferences"], [logger]);

        // Unload everything when done.
        project.ProjectCollection.UnloadAllProjects();

        Assert.Empty(logger.Errors);
        Assert.True(logger.Succeeded);
    }

    private sealed class Logger : ILogger
    {
        public LoggerVerbosity Verbosity { get; set; } = LoggerVerbosity.Quiet;
        public string? Parameters { get; set; }

        public bool? Succeeded { get; private set; }

        public ImmutableList<BuildErrorEventArgs> Errors = [];

        public void Initialize(IEventSource eventSource)
        {
            eventSource.ErrorRaised += (s, e) => ImmutableInterlocked.Update(
                ref Errors,
                static (errors, e) => errors.Add(e),
                e);

            eventSource.BuildFinished += (s, e) => Succeeded = e.Succeeded;
        }

        public void Shutdown()
        {
        }
    }
}

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using System;
using System.Threading;
using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;

namespace Microsoft.VisualStudio.ProjectSystem.IntegrationTests
{
    [CaptureTestName]
    public abstract class AbstractIntegrationTest : IDisposable
    {
        public readonly VisualStudioInstance VisualStudio;

        protected const string ProjectName = "TestProj";
        protected readonly ProjectUtils.Project Project = new ProjectUtils.Project(ProjectName);
        protected readonly string SolutionName = "TestSolution";

        private VisualStudioInstanceContext _visualStudioContext;

        protected abstract string DefaultLanuageName { get; }

        protected AbstractIntegrationTest(
            string solutionName,
            string projectTemplate,
            VisualStudioInstanceFactory instanceFactory)
        {
            _visualStudioContext = instanceFactory.GetNewOrUsedInstance(SharedIntegrationHostFixture.RequiredPackageIds);
            VisualStudio = _visualStudioContext.Instance;

            VisualStudio.SolutionExplorer.CreateSolution(solutionName);
            VisualStudio.SolutionExplorer.AddProject(Project, projectTemplate, DefaultLanuageName);

            // wait for restore to complete.
            VisualStudio.WaitForApplicationIdle();
            VisualStudio.WaitForNoErrorsInErrorList();

            VisualStudio.SolutionExplorer.BuildSolution(waitForBuildToFinish: true);
            var path = VisualStudio.SolutionExplorer.SolutionFileFullPath;
            VisualStudio.SolutionExplorer.CloseSolution();
            VisualStudio.SolutionExplorer.OpenSolution(path);

            VisualStudio.WaitForApplicationIdle();
            VisualStudio.WaitForNoErrorsInErrorList();
        }

        public void Dispose() 
            => _visualStudioContext.Dispose();

        protected void Wait(double seconds)
            => Thread.Sleep(TimeSpan.FromMilliseconds(seconds * 1000));

        protected KeyPress Ctrl(VirtualKey virtualKey)
            => new KeyPress(virtualKey, ShiftState.Ctrl);

        protected KeyPress Shift(VirtualKey virtualKey)
            => new KeyPress(virtualKey, ShiftState.Shift);

        protected KeyPress Alt(VirtualKey virtualKey)
            => new KeyPress(virtualKey, ShiftState.Alt);
    }
}

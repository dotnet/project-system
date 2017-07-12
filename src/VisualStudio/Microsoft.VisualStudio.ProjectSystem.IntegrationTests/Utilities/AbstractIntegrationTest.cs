// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using System;
using System.IO;
using System.Threading;
using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;

namespace Microsoft.VisualStudio.ProjectSystem.IntegrationTests
{
    [CaptureTestName]
    public abstract class AbstractIntegrationTest : IDisposable
    {
        static AbstractIntegrationTest()
        {
            string dir = Path.GetDirectoryName(typeof(AbstractIntegrationTest).Assembly.Location);
            Environment.SetEnvironmentVariable("VisualBasicDesignTimeTargetsPath", Path.Combine(dir, "Microsoft.VisualBasic.DesignTime.targets"));
            Environment.SetEnvironmentVariable("CSharpDesignTimeTargetsPath", Path.Combine(dir, "Microsoft.CSharp.DesignTime.targets"));
            Environment.SetEnvironmentVariable("FSharpDesignTimeTargetsPath", Path.Combine(dir, "Microsoft.FSharp.DesignTime.targets"));
        }

        public readonly VisualStudioInstance VisualStudio;

        protected const string ProjectName = "TestProj";
        protected readonly ProjectUtils.Project Project = new ProjectUtils.Project(ProjectName);
        protected readonly string SolutionName = "TestSolution";

        private VisualStudioInstanceContext _visualStudioContext;

        protected abstract string DefaultLanguageName { get; }

        protected AbstractIntegrationTest(
            string solutionName,
            string projectTemplate,
            VisualStudioInstanceFactory instanceFactory)
        {
            _visualStudioContext = instanceFactory.GetNewOrUsedInstance(SharedIntegrationHostFixture.RequiredPackageIds);
            VisualStudio = _visualStudioContext.Instance;

            VisualStudio.SolutionExplorer.CreateSolution(solutionName);
            VisualStudio.SolutionExplorer.AddProject(Project, projectTemplate, DefaultLanguageName);

            // wait for restore to complete.
            VisualStudio.WaitForApplicationIdle();
            VisualStudio.WaitForNoErrorsInErrorList();

            // added to work around https://github.com/dotnet/project-system/issues/2256
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

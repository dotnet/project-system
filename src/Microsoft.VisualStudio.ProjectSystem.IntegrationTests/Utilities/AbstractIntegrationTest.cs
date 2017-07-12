// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using System;
using System.Configuration;
using System.IO;
using System.Threading;
using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;

namespace Microsoft.VisualStudio.ProjectSystem.IntegrationTests
{
    [CaptureTestName]
    public abstract class AbstractIntegrationTest : IDisposable
    {
        private const string XamlRulesDirRelativeToTestAssemblyConfigKey = "ProjectSystem.XamlRulesDirRelativeToTestAssembly";

        static AbstractIntegrationTest()
        {
            string relativePath = ConfigurationManager.AppSettings[XamlRulesDirRelativeToTestAssemblyConfigKey] ?? "";
            string dir = Path.Combine(Path.GetDirectoryName(typeof(AbstractIntegrationTest).Assembly.Location), relativePath);

            void RequireFileExists(string path)
            {
                if (!File.Exists(path))
                {
                    throw new FileNotFoundException($"Design time targets file not found: '{path}'", path);
                }
            }

            void SetVariable(string variableName, string fileName)
            {
                string fullPath = Path.GetFullPath(Path.Combine(dir, fileName));
                RequireFileExists(fullPath);
                Environment.SetEnvironmentVariable(variableName, fullPath);
            }

            SetVariable("VisualBasicDesignTimeTargetsPath", "Microsoft.VisualBasic.DesignTime.targets");
            SetVariable("CSharpDesignTimeTargetsPath", "Microsoft.CSharp.DesignTime.targets");
            SetVariable("FSharpDesignTimeTargetsPath", "Microsoft.FSharp.DesignTime.targets");
            RequireFileExists(Path.Combine(dir, "Microsoft.Managed.DesignTime.targets"));
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

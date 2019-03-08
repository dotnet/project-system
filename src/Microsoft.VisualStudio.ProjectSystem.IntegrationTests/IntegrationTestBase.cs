// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.Test.Apex.VisualStudio;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#nullable disable

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    [TestClass] // AssemblyInitialize won't be found without it
    public abstract class IntegrationTestBase : VisualStudioHostTest
    {
        private readonly bool _shutdownBetweenTests;
        private static string _hiveName;

        protected IntegrationTestBase(bool shutdownBetweenTests = true)
        {
            _shutdownBetweenTests = shutdownBetweenTests;
            // TestCleanup will fire up another instance of Visual Studio to reset 
            // the AutoloadExternalChanges if it thinks the default changed even if
            // that was just caused by settings to be sync'd. Just turn this feature off.
            SuppressReloadPrompt = false;
        }

        protected override VisualStudioHostConfiguration GetHostConfiguration()
        {
            return new VisualStudioHostConfiguration
            {
                CommandLineArguments = $"/rootSuffix {_hiveName}",
                RestoreUserSettings = false,
                InheritProcessEnvironment = true,
                AutomaticallyDismissMessageBoxes = true,
                DelayInitialVsLicenseValidation = true,
                ForceFirstLaunch = true,
                BootstrapInjection = BootstrapInjectionMethod.DteFromROT
            };
        }

        protected override void DoHostTestCleanup()
        {
            base.DoHostTestCleanup();

            // TODO verify that this actually does what it's trying to do
            if (_shutdownBetweenTests)
            {
                TryShutdownVisualStudioInstance();
            }
        }

        [AssemblyInitialize]
        public static void Initialize(TestContext context)
        {
            _hiveName = GetHiveNames().First(name => !string.IsNullOrWhiteSpace(name));

            SetTestInstallationDirectoryIfUnset();

            return;

            IEnumerable<string> GetHiveNames()
            {
                yield return (string)context.Properties["VsRootSuffix"];
                yield return Environment.GetEnvironmentVariable("RootSuffix");
                yield return "Exp";
            }

            void SetTestInstallationDirectoryIfUnset()
            {
                string installationUnderTest = "VisualStudio.InstallationUnderTest.Path";

                if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(installationUnderTest)))
                {
                    string vsDirectory = Environment.GetEnvironmentVariable("VSAPPIDDIR");
                    string devenv = Environment.GetEnvironmentVariable("VSAPPIDNAME");
                    if (!string.IsNullOrEmpty(vsDirectory) && !string.IsNullOrEmpty(devenv))
                    {
                        Environment.SetEnvironmentVariable(installationUnderTest, Path.Combine(vsDirectory, devenv));
                    }
                }
            }
        }
    }
}

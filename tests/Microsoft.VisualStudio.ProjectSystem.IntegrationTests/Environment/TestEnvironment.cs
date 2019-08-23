// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.VisualStudio
{
    internal static class TestEnvironment
    {
        /// <summary>
        ///     Gets the Visual Studio hive to run tests under.
        /// </summary>
        public static string? VisualStudioHive
        {
            get;
            private set;
        }

        public static void OnAssemblyInitialize(TestContext context)
        {
            SetTestInstallationDirectoryIfUnset();

            // Get hive from .runsettings if present (command-line)
            string rootSuffix = (string)context.Properties["VsRootSuffix"];
            if (string.IsNullOrEmpty(rootSuffix))
            {
                // Otherwise, respect the environment, failing that use the default
                rootSuffix = Environment.GetEnvironmentVariable("RootSuffix") ?? "Exp";
            }

            VisualStudioHive = rootSuffix;
        }

        private static void SetTestInstallationDirectoryIfUnset()
        {
            string installationUnderTest = "VisualStudio.InstallationUnderTest.Path";
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(installationUnderTest)))
            {
                string vsDirectory = Environment.GetEnvironmentVariable("VSAPPIDDIR");
                string devenv = Environment.GetEnvironmentVariable("VSAPPIDNAME");
                if (!string.IsNullOrEmpty(vsDirectory) && !string.IsNullOrEmpty(devenv))
                {   // Use the same version we're running inside (Test Explorer)

                    Environment.SetEnvironmentVariable(installationUnderTest, Path.Combine(vsDirectory, devenv));
                }
            }
        }
    }
}

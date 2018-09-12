// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.Test.Apex.VisualStudio;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.VisualStudio.ProjectSystem.IntegrationTests
{
    public abstract class TestBase : VisualStudioHostTest
    {
        private static string _hiveName;

        protected static void Initialize(TestContext context)
        {
            _hiveName = GetVsHiveName(context);
        }

        protected VisualStudioHostConfiguration DefaultHostConfiguration
        {
            get
            {
                var visualStudioHostConfiguration = new VisualStudioHostConfiguration()
                {
                    CommandLineArguments = $"/rootSuffix {_hiveName}",
                    RestoreUserSettings = false,
                    InheritProcessEnvironment = true,
                    AutomaticallyDismissMessageBoxes = true,
                    DelayInitialVsLicenseValidation = true,
                    ForceFirstLaunch = true,
                };

                return visualStudioHostConfiguration;
            }
        }

        protected VisualStudioHost GetVS() => Operations.CreateHost<VisualStudioHost>(DefaultHostConfiguration);

        private static string GetVsHiveName(TestContext context)
        {
            string rootSuffix = (string)context.Properties["VsRootSuffix"];
            if (!string.IsNullOrEmpty(rootSuffix))
                return rootSuffix;

            return Environment.GetEnvironmentVariable("RootSuffix") ?? "Exp";
        }
    }
}

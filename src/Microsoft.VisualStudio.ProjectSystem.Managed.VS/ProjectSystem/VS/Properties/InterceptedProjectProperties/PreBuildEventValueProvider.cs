// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties.InterceptedProjectProperties
{
    [ExportInterceptingPropertyValueProvider(_preBuildEventString, ExportInterceptingPropertyValueProviderFile.ProjectFile)]
    internal class PreBuildEventValueProvider : AbstractBuildEventValueProvider
    {
        private const string _preBuildEventString = "PreBuildEvent";
        private const string _targetNameString = "PreBuild";

        [ImportingConstructor]
        public PreBuildEventValueProvider(
            IProjectLockService projectLockService,
            UnconfiguredProject unconfiguredProject)
            : base(projectLockService,
                   unconfiguredProject,
                   new PreBuildEventHelper())
        { }

        internal class PreBuildEventHelper : Helper
        {
            internal PreBuildEventHelper()
                : base(_preBuildEventString,
                       _targetNameString,
                       target => target.BeforeTargets,
                       target => { target.BeforeTargets = _preBuildEventString; })
            { }
        }
    }
}

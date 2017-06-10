// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties.InterceptedProjectProperties
{
    [ExportInterceptingPropertyValueProvider(_preBuildEvent, ExportInterceptingPropertyValueProviderFile.ProjectFile)]
    internal class PreBuildEventValueProvider : AbstractBuildEventValueProvider
    {
        private const string _preBuildEvent = "PreBuildEvent";
        private const string _targetName = "PreBuild";

        [ImportingConstructor]
        public PreBuildEventValueProvider(
            IProjectLockService projectLockService,
            UnconfiguredProject unconfiguredProject)
            : base(projectLockService,
                   unconfiguredProject,
                   new PreBuildEventHelper())
        { }

        internal class PreBuildEventHelper : AbstractBuildEventHelper
        {
            internal PreBuildEventHelper()
                : base(_preBuildEvent,
                       _targetName,
                       target => target.BeforeTargets,
                       target => { target.BeforeTargets = _preBuildEvent; })
            { }
        }
    }
}

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.Build.Construction;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties.InterceptedProjectProperties
{
    [ExportInterceptingPropertyValueProvider(_preBuildEventString, ExportInterceptingPropertyValueProviderFile.ProjectFile)]
    internal class PreBuildEventValueProvider : AbstractBuildEventValueProvider
    {
        private const string _preBuildEventString = "PreBuildEvent";

        [ImportingConstructor]
        public PreBuildEventValueProvider(
            IProjectLockService projectLockService,
            UnconfiguredProject unconfiguredProject)
            : base(projectLockService, unconfiguredProject)
        {}

        protected override string BuildEventString => _preBuildEventString;

        protected override string TargetNameString => "PreBuild";

        protected override string GetTargetString(ProjectTargetElement target)
            => target.BeforeTargets;

        protected override void SetTargetDependencies(ProjectTargetElement target)
            => target.BeforeTargets = _preBuildEventString;
    }
}

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.Build.Construction;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties.InterceptedProjectProperties
{
    [ExportInterceptingPropertyValueProvider(_postBuildEventString, ExportInterceptingPropertyValueProviderFile.ProjectFile)]
    internal class PostBuildEventValueProvider : AbstractBuildEventValueProvider
    {
        private const string _postBuildEventString = "PostBuildEvent";

        [ImportingConstructor]
        public PostBuildEventValueProvider(
            IProjectLockService projectLockService,
            UnconfiguredProject unconfiguredProject)
            : base(projectLockService, unconfiguredProject)
        {}

        protected override string BuildEventString => _postBuildEventString;

        protected override string TargetNameString => "PostBuild";

        protected override string GetTargetString(ProjectTargetElement target)
            => target.AfterTargets;

        protected override void SetTargetString(ProjectTargetElement target, string targetName)
            => target.AfterTargets = targetName;
    }
}

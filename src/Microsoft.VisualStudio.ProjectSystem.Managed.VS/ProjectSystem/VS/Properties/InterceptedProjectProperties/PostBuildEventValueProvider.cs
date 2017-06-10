// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties.InterceptedProjectProperties
{
    [ExportInterceptingPropertyValueProvider(_postBuildEvent, ExportInterceptingPropertyValueProviderFile.ProjectFile)]
    internal class PostBuildEventValueProvider : AbstractBuildEventValueProvider
    {
        private const string _postBuildEvent = "PostBuildEvent";
        private const string _targetName = "PostBuild";

        [ImportingConstructor]
        public PostBuildEventValueProvider(
            IProjectLockService projectLockService,
            UnconfiguredProject unconfiguredProject)
            : base(projectLockService,
                   unconfiguredProject,
                   new PostBuildEventHelper())
        {}

        internal class PostBuildEventHelper : AbstractBuildEventHelper
        {
            internal PostBuildEventHelper()
                : base(_postBuildEvent,
                       _targetName,
                       target => target.AfterTargets,
                       target => { target.AfterTargets = _postBuildEvent; })
            { }
        }
    }
}

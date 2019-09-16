// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties.InterceptedProjectProperties
{
    [ExportInterceptingPropertyValueProvider(PostBuildEvent, ExportInterceptingPropertyValueProviderFile.ProjectFile)]
    internal class PostBuildEventValueProvider : AbstractBuildEventValueProvider
    {
        private const string PostBuildEvent = "PostBuildEvent";
        private const string TargetName = "PostBuild";

        [ImportingConstructor]
        public PostBuildEventValueProvider(
            IProjectAccessor projectAccessor,
            UnconfiguredProject project)
            : base(projectAccessor,
                   project,
                   new PostBuildEventHelper())
        { }

        internal class PostBuildEventHelper : AbstractBuildEventHelper
        {
            internal PostBuildEventHelper()
                : base(PostBuildEvent,
                       TargetName,
                       target => target.AfterTargets,
                       target => { target.AfterTargets = PostBuildEvent; })
            { }
        }
    }
}

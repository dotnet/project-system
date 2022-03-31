// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

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

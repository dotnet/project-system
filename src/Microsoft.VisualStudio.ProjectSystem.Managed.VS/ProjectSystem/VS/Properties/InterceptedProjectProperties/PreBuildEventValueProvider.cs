// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties.InterceptedProjectProperties
{
    [ExportInterceptingPropertyValueProvider(PreBuildEvent, ExportInterceptingPropertyValueProviderFile.ProjectFile)]
    internal class PreBuildEventValueProvider : AbstractBuildEventValueProvider
    {
        private const string PreBuildEvent = "PreBuildEvent";
        private const string TargetName = "PreBuild";

        [ImportingConstructor]
        public PreBuildEventValueProvider(
            IProjectAccessor projectAccessor,
            UnconfiguredProject project)
            : base(projectAccessor,
                   project,
                   new PreBuildEventHelper())
        { }

        internal class PreBuildEventHelper : AbstractBuildEventHelper
        {
            internal PreBuildEventHelper()
                : base(PreBuildEvent,
                       TargetName,
                       target => target.BeforeTargets,
                       target => { target.BeforeTargets = PreBuildEvent; })
            { }
        }
    }
}

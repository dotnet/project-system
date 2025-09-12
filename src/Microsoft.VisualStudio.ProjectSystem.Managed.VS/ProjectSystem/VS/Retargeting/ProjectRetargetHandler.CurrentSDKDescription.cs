// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.Retargeting;

internal partial class ProjectRetargetHandler
{
    internal class CurrentSDKDescription : TargetDescriptionBase
    {
        internal static CurrentSDKDescription Create(string sdkVersion)
        {
            return new CurrentSDKDescription(sdkVersion);
        }

        private CurrentSDKDescription(string sdkVersion) : base(
            targetId: Guid.NewGuid(),
            displayName: $".NET SDK {sdkVersion}",
            order: 1,
            supported: true,
            description: $".NET SDK {sdkVersion}",
            canRetarget: true,
            guidanceLink: null)
        {
        }
    }
}

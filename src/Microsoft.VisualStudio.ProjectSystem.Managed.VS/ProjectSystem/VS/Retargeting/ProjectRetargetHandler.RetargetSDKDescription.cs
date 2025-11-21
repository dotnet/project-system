// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.Retargeting;

internal partial class ProjectRetargetHandler
{
    internal class RetargetSDKDescription : TargetDescriptionBase
    {
        internal static RetargetSDKDescription Create(string sdkVersion)
        {
            return new RetargetSDKDescription(sdkVersion);
        }

        private RetargetSDKDescription(string sdkVersion) : base(
            targetId: Guid.NewGuid(),
            displayName: $".NET SDK {sdkVersion}",
            order: 1,
            supported: true,
            description: string.Format(VSResources.RetargetingSDKDescription, sdkVersion),
            canRetarget: true, // this means we want to show this option in the retarget dialog
            guidanceLink: $"https://dotnet.microsoft.com/download/dotnet/thank-you/sdk-{sdkVersion}-windows-x64-installer")
        {
        }
    }
}

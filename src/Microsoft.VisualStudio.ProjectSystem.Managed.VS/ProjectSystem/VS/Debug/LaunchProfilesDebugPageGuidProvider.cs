// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.Debug
{
    [Export(typeof(IDebugPageGuidProvider))]
    [AppliesTo(ProjectCapability.LaunchProfiles)]
    [Order(Order.Default)]
    internal class LaunchProfilesDebugPageGuidProvider : IDebugPageGuidProvider
    {
        // This is the Guid of C#, VB and F# Debug property page
        private static readonly Task<Guid> s_guid = Task.FromResult(new Guid("{0273C280-1882-4ED0-9308-52914672E3AA}"));

        public Task<Guid> GetDebugPropertyPageGuidAsync()
        {
            return s_guid;
        }
    }
}

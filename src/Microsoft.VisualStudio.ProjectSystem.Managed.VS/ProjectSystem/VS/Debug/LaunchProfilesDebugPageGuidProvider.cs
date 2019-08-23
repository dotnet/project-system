// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

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

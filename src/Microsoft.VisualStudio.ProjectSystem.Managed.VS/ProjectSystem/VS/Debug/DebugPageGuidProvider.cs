// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Debug
{
    [Export(typeof(IDebugPageGuidProvider))]
    [AppliesTo(ProjectCapability.LaunchProfiles)]
    [Order(Order.Default)]
    internal class DebugPageGuidProvider : IDebugPageGuidProvider
    {
        // This is the Guid of C#, VB and F# Debug property page
        private readonly Guid _guid = new Guid("{0273C280-1882-4ED0-9308-52914672E3AA}");

        public Task<Guid> GetDebugPropertyPageGuidAsync()
        {
            return Task.FromResult(_guid);
        }
    }
}

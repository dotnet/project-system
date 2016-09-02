// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    /// <summary>
    /// Given an ILaunchProfile, it will enumerate the items and do replacement on the each string
    /// entry.
    /// </summary>
    public interface IDebugTokenReplacer
    {
        Task<ILaunchProfile> ReplaceTokensInProfileAsync(ILaunchProfile profile);

        Task<string> ReplaceTokensInStringAsync(string rawString, bool expandEnvironmentVars);
    }
}

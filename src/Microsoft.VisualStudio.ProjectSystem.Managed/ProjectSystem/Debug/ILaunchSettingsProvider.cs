// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{

    /// <summary>
    /// Interface definition for the LaunchSettingsProvider.
    /// </summary>
    public interface ILaunchSettingsProvider
    {
        IReceivableSourceBlock<ILaunchSettings> SourceBlock { get; }

        ILaunchSettings CurrentSnapshot { get; }
        
        string LaunchSettingsFile { get; }
        
        ILaunchProfile ActiveProfile { get; }
            
        // Replaces the current set of profiles with the contents of profiles. If changes were
        // made, the file will be checked out and updated. If the active profile is different, the
        // active profile property is updated.
        Task UpdateAndSaveSettingsAsync(ILaunchSettings profiles);

        // Blocks until at least one snapshot has been generated.
        Task<ILaunchSettings> WaitForFirstSnapshot(int timeout);
    }
}


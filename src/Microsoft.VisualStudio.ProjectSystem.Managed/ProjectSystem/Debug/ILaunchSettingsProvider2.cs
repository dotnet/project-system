// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
  
    public interface ILaunchSettingsProvider2 : ILaunchSettingsProvider
    {
        /// <summary>
        ///     Gets the path to the lauch settings file, typically, "launchSettings.json".
        /// </summary>
        Task<string> GetLaunchSettingsFilePathAsync();
    }
}


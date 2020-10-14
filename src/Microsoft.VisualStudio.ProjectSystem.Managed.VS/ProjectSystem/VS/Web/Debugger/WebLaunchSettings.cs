// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;

namespace Microsoft.WebTools.ProjectSystem.Debugger
{
    internal enum ServerType
    {
        IISExpress,
        IIS,
        Custom
    }

    /// <summary>
    /// Represents the current set of settings used to run the project
    /// </summary>
    internal class WebLaunchSettings
    {
        public WebLaunchSettings(ServerType serverType, List<string> serverUrls, bool use64bitIIS = false, bool useGlobalAppHostCfgFile = false, 
                                 bool overrideAppRootUrl = false, string? iisOverrideAppRootUrl = null) 
        { 
            ServerType = serverType;
            ServerUrls = serverUrls;
            Use64bitIIS = use64bitIIS;
            UseGlobalAppHostCfgFile = useGlobalAppHostCfgFile;
            OverrideAppRootUrl = overrideAppRootUrl;
            IISOverrideAppRootUrl = iisOverrideAppRootUrl;
        }

        public ServerType ServerType { get; private set; }
        public List<string> ServerUrls { get; private set; }
        public bool OverrideAppRootUrl { get; private set; }
        public string? IISOverrideAppRootUrl { get; private set; }
        public bool Use64bitIIS { get; private set; }
        public bool UseGlobalAppHostCfgFile { get; private set; }
    }
}

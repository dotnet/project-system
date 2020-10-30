// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Web
{
    internal class WebProjectProperties : IWebProjectProperties
    {
        public WebProjectProperties(
            string applicationDirectory,
            Uri applicationUrl,
            Uri browseUrl,
            string projectDirectory,
            Uri projectUrl)
        {
            ApplicationDirectory = applicationDirectory;
            ApplicationUrl = applicationUrl;
            BrowseUrl = browseUrl;
            ProjectDirectory = projectDirectory;
            ProjectUrl = projectUrl;
        }

        public string ApplicationDirectory { get; }

        public Uri ApplicationUrl { get; }

        public Uri BrowseUrl { get; }

        public string ProjectDirectory { get; }

        public Uri ProjectUrl { get; }
    }
}

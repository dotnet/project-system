// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.Model
{
    internal enum BuildOperation
    {
        Unknown,
        Clean,
        Build,
        Rebuild,
        Deploy,
        Publish,
        PublishUI,
        DesignTime
    }
}

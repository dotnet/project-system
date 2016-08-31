// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.ProjectSystem.Debug;
using Moq;

namespace Microsoft.VisualStudio.Mocks
{
    public class IDebugLaunchProviderMetadataViewFactory
    {
        public static IDebugLaunchProviderMetadataView CreateInstance()
        {
            return Mock.Of<IDebugLaunchProviderMetadataView>();
        }
    }
}

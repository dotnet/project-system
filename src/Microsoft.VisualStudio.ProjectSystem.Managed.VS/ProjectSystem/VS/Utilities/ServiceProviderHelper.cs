// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Utilities
{
    internal interface IServiceProviderHelper
    {
        IServiceProvider GlobalProvider { get; }
    }

    [Export(typeof(IServiceProviderHelper))]
    internal class VsServiceProviderHelper : IServiceProviderHelper
    {
        public IServiceProvider GlobalProvider => ServiceProvider.GlobalProvider;
    }
}

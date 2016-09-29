// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.ProjectSystem.LanguageServices;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.LanguageServices
{
    [Export(typeof(IProjectHostProvider))]
    internal sealed partial class UnconfiguredProjectHostProvider: IProjectHostProvider
    {
        [ImportingConstructor]
        public UnconfiguredProjectHostProvider()
        {
        }

        public Object GetProjectHostObject(UnconfiguredProject project)
        {
            return new HostObject((IVsHierarchy)project.Services.HostObject);
        }
    }
}

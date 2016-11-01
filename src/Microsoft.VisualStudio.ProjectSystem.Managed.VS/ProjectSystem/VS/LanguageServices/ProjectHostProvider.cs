// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.ProjectSystem.LanguageServices;

namespace Microsoft.VisualStudio.ProjectSystem.VS.LanguageServices
{
    [Export(typeof(IProjectHostProvider))]
    internal sealed partial class ProjectHostProvider : IProjectHostProvider
    {
        [ImportingConstructor]
        public ProjectHostProvider(UnconfiguredProject unconfiguredProject)
        {
            Requires.NotNull(unconfiguredProject, nameof(unconfiguredProject));

            UnconfiguredProjectHostObject = new UnconfiguredProjectHostObject(unconfiguredProject);
        }

        public IUnconfiguredProjectHostObject UnconfiguredProjectHostObject { get; }

        public IConfiguredProjectHostObject GetConfiguredProjectHostObject(IUnconfiguredProjectHostObject unconfiguredProjectHostObject, String projectDisplayName)
        {
            return new ConfiguredProjectHostObject((UnconfiguredProjectHostObject)unconfiguredProjectHostObject, projectDisplayName);
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    /// <summary>
    /// This class is here only to remember whether this is a newly created project or not. CPS will import INewProjectInitializationProvider and Call
    /// InitializeNewProject for new projects. Just set a bool to remember this state.
    ///
    /// </summary>
    [AppliesTo(ProjectCapability.DotNet)]
    [Export(typeof(INewProjectInitializationProvider))]
    [Export(typeof(IProjectCreationState))]
    internal sealed class NewProjectInitializationProvider : INewProjectInitializationProvider, IProjectCreationState
    {
        [ImportingConstructor]
        internal NewProjectInitializationProvider(UnconfiguredProject project)
        {
        }

        public bool WasNewlyCreated { get; private set; }

        public void InitializeNewProject()
        {
            WasNewlyCreated = true;
        }
    }
}

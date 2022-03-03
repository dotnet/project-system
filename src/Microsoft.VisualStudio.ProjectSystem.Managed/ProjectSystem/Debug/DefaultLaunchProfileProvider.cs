// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.IO;
using System.ComponentModel.Composition;
using ExportOrder = Microsoft.VisualStudio.ProjectSystem.OrderAttribute;

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    [Export(typeof(IDefaultLaunchProfileProvider))]
    [AppliesTo(ProjectCapability.LaunchProfiles)]
    [ExportOrder(Order.Default)] 
    internal class DefaultLaunchProfileProvider : IDefaultLaunchProfileProvider
    {
        private readonly UnconfiguredProject _project;

        [ImportingConstructor]
        public DefaultLaunchProfileProvider(UnconfiguredProject project)
        {
            _project = project;
        }

        public ILaunchProfile? CreateDefaultProfile()
        {
            return new LaunchProfile { Name = Path.GetFileNameWithoutExtension(_project.FullPath), CommandName = LaunchSettingsProvider.RunProjectCommandName };
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Retargetting
{
    //[Export(typeof(IVetoProjectPreLoad))]
    //[AppliesTo(ProjectCapability.DotNet)]
    internal class VetoMissingDesktopProperty : IVetoProjectLoad
    {
        private readonly UnconfiguredProject _project;
        private readonly IProjectAccessor _projectAccessor;

        [ImportingConstructor]
        public VetoMissingDesktopProperty(UnconfiguredProject project, IProjectAccessor projectAccessor)
        {
            _project = project;
            _projectAccessor = projectAccessor;
        }

        public async Task<bool> AllowProjectLoadAsync(bool isNewProject, CancellationToken cancellationToken = default)
        {
            return await _projectAccessor.OpenProjectXmlForReadAsync(_project, root =>
            {
                return root.Sdk == "Microsoft.NET.Sdk.WindowsDesktop" &&
                       root.Properties.Any(p => p.Name == "UseWindowsForms" || p.Name == "UseWPF");
            });
        }
    }
}

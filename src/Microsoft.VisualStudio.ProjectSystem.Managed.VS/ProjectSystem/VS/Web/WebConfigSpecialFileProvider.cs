// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.SpecialFileProviders;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Web
{
    /// <summary>
    ///     Provides a <see cref="ISpecialFileProvider"/> that handles the default 'Web.config' file;
    ///     which contains .NET Framework directives for assembly binding, compatibility, runtime
    ///     and web specific settings.
    /// </summary>
    /// <remarks>
    ///     This runs before <see cref="AppConfigSpecialFileProvider"/> to handle ASP.NET projects.
    /// </remarks>
    [ExportSpecialFileProvider(SpecialFiles.AppConfig)]
    [AppliesTo("AspNet")] // TODO
    [Order(Order.BeforeDefault)]    // Before AppConfigSpecialFileProvider
    internal class WebConfigSpecialFileProvider : AbstractFindByNameSpecialFileProvider
    {
        private readonly ICreateFileFromTemplateService _templateFileCreationService;

        [ImportingConstructor]
        public WebConfigSpecialFileProvider(IPhysicalProjectTree projectTree, ICreateFileFromTemplateService templateFileCreationService)
            : base("Web.config", projectTree)
        {
            _templateFileCreationService = templateFileCreationService;
        }

        protected override Task CreateFileAsync(string path)
        {
            return _templateFileCreationService.CreateFileAsync("NeedToFindThisName.zip", path); // TODO: Find template
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.CodeDom.Compiler;
using Microsoft.VisualBasic;
using Microsoft.VisualStudio.Designer.Interfaces;

namespace Microsoft.VisualStudio.ProjectSystem.VS.LanguageServices.VisualBasic
{
    /// <summary>
    ///     Provides the Visual Basic <see cref="CodeDomProvider"/> for use by designers and code generators.
    /// </summary>
    /// <remarks>
    ///     This service is requested by <see cref="IVSMDCodeDomCreator.CreateCodeDomProvider(object, int)"/> and
    ///     returned by <see cref="IVSMDCodeDomProvider.CodeDomProvider"/>.
    /// </remarks>
    [ExportVsProfferedProjectService(typeof(CodeDomProvider))]
    [AppliesTo(ProjectCapability.VisualBasic)]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    internal class VisualBasicCodeDomProvider : VBCodeProvider
    {
        [ImportingConstructor]
        public VisualBasicCodeDomProvider()
        {
        }
    }
}

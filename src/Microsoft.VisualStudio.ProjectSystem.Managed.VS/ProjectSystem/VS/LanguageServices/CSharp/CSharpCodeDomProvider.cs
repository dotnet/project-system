// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.CodeDom.Compiler;
using Microsoft.CSharp;
using Microsoft.VisualStudio.Designer.Interfaces;

namespace Microsoft.VisualStudio.ProjectSystem.VS.LanguageServices.CSharp
{
    /// <summary>
    ///     Provides the C# <see cref="CodeDomProvider"/> for use by designers and code generators.
    /// </summary>
    /// <remarks>
    ///     This service is requested by <see cref="IVSMDCodeDomCreator.CreateCodeDomProvider(object, int)"/> and
    ///     returned by <see cref="IVSMDCodeDomProvider.CodeDomProvider"/>.
    /// </remarks>
    [ExportVsProfferedProjectService(typeof(CodeDomProvider))]
    [AppliesTo(ProjectCapability.CSharp)]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    internal class CSharpCodeDomProvider : CSharpCodeProvider
    {
        [ImportingConstructor]
        public CSharpCodeDomProvider()
        {
        }
    }
}

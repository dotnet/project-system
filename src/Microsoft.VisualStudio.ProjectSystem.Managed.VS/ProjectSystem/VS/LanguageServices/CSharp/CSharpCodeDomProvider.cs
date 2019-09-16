// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.CodeDom.Compiler;
using System.ComponentModel.Composition;
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

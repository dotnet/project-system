// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.CodeDom.Compiler;
using System.ComponentModel.Composition;
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

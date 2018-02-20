// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.CodeDom.Compiler;
using System.ComponentModel.Composition;

using Microsoft.CSharp;

namespace Microsoft.VisualStudio.ProjectSystem.VS.LanguageServices
{
    /// <summary>
    ///     Provides the C# <see cref="CodeDomProvider"/> for use by designers and code generators.
    /// </summary>
    [ExportVsProfferedProjectService(typeof(CodeDomProvider))]
    [AppliesTo(ProjectCapability.CSharp)]
    internal class CSharpCodeDomProvider : CSharpCodeProvider
    {
        [ImportingConstructor]
        public CSharpCodeDomProvider()
        {
        }
        
        [Import]
        public UnconfiguredProject UnconfiguredProject   // Put ourselves in the UnconfiguredProject scope
        {
            get;
            set;
        }
    }
}

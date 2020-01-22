// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.SpecialFileProviders.CSharp
{
    /// <summary>
    ///     Provides a <see cref="ISpecialFileProvider"/> that handles the WPF Application Definition file,
    ///     typically called "App.xaml" in C# projects.
    /// </summary>
    [ExportSpecialFileProvider(SpecialFiles.AppXaml)]
    [AppliesTo(ProjectCapability.CSharp)]
    internal class CSharpAppXamlSpecialFileProvider : AbstractAppXamlSpecialFileProvider
    {
        [ImportingConstructor]
        public CSharpAppXamlSpecialFileProvider(IPhysicalProjectTree projectTree)
            : base("App.xaml", projectTree)
        {
        }

        protected override Task CreateFileAsync(string path)
        {
            // We don't have a template for C# for App.xaml, deliberately 
            // throw NotImplementedException (which gets mapped to E_NOTIMPL) to
            // indicate we don't support this.
            throw new NotImplementedException();
        }
    }
}

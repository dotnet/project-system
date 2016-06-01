// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using static System.Diagnostics.Debug;

namespace Microsoft.VisualStudio.ProjectSystem.SpecialFileProviders
{
    [ExportSpecialFileProvider(SpecialFiles.AssemblyResource)]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
    internal class ResourcesFileSpecialFileProvider : AbstractSpecialFileProvider
    {
        protected override string GetFileNameOfSpecialFile(SpecialFiles fileId)
        {
            Assert(fileId == SpecialFiles.AssemblyResource);
            return "Resources.resx";
        }

        protected override string GetTemplateForSpecialFile(SpecialFiles fileId)
        {
            Assert(fileId == SpecialFiles.AssemblyResource);
            return "ResourceInternal.zip";
        }
    }
}

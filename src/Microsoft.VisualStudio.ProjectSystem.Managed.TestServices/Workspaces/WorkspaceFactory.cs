// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.VisualStudio.Workspaces
{
    public class WorkspaceFactory
    {
        private static readonly MetadataReference s_corlibReference = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);  
        private static readonly MetadataReference s_systemReference = MetadataReference.CreateFromFile(typeof(System.Diagnostics.Debug).Assembly.Location);  
        private static readonly MetadataReference s_systemCoreReference = MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location);  

        public static Workspace Create(string text, string language = LanguageNames.CSharp)
        {
            var workspace = new AdhocWorkspace();

            var projectInfo = ProjectInfo.Create(ProjectId.CreateNewId(), VersionStamp.Create(), "TestProject", "TestProject", language, filePath: "D:\\Test.proj");
            var project = workspace.AddProject(projectInfo)
                                   .AddMetadataReference(s_corlibReference)
                                   .AddMetadataReference(s_systemReference)
                                   .AddMetadataReference(s_systemCoreReference);

            var documentInfo = DocumentInfo.Create(DocumentId.CreateNewId(projectInfo.Id), "TestDocument");
            workspace.AddDocument(project.Id, "TestDocument", SourceText.From(text));

            return workspace;
        }
    }
}

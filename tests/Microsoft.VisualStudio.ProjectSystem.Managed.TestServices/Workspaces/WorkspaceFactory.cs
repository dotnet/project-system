// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.VisualStudio.Workspaces
{
    public static class WorkspaceFactory
    {
        private static readonly MetadataReference s_corlibReference = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
        private static readonly MetadataReference s_systemReference = MetadataReference.CreateFromFile(typeof(System.Diagnostics.Debug).Assembly.Location);
        private static readonly MetadataReference s_systemCoreReference = MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location);

        public static Workspace Create(string text, string language = LanguageNames.CSharp)
        {
            var workspace = new AdhocWorkspace();

            var projectInfo = ProjectInfo.Create(ProjectId.CreateNewId(), VersionStamp.Create(), "TestProject", "TestProject", language,
                                                 filePath: "D:\\Test.proj",
                                                 metadataReferences: new[] { s_corlibReference, s_systemCoreReference, s_systemReference });
            Project project = workspace.AddProject(projectInfo);

            workspace.AddDocument(project.Id, "TestDocument", SourceText.From(text));

            return workspace;
        }
    }
}

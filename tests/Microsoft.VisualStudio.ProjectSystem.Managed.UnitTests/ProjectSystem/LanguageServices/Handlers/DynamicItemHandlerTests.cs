// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices.Handlers
{
    public class DynamicItemHandlerTests : EvaluationHandlerTestBase
    {
        [Fact]
        public void Handle_RazorAndCshtmlFiles_AddsToContext()
        {
            var dynamicFiles = new HashSet<string>(StringComparers.Paths);
            void onDynamicFileAdded(string s) => Assert.True(dynamicFiles.Add(s));

            var project = UnconfiguredProjectFactory.Create(fullPath: @"C:\Myproject.csproj");
            var context = IWorkspaceProjectContextMockFactory.CreateForDynamicFiles(project, onDynamicFileAdded);

            var handler = CreateInstance(project, context);

            var projectChange = IProjectChangeDescriptionFactory.FromJson(
@"{
    ""Difference"": { 
        ""AnyChanges"": true,
        ""AddedItems"": [ ""File1.razor"", ""File1.cshtml"", ""File1.cs"" ]
    }
}");

            Handle(handler, projectChange);

            Assert.Equal(2, dynamicFiles.Count);
            Assert.Contains(@"C:\File1.razor", dynamicFiles);
            Assert.Contains(@"C:\File1.cshtml", dynamicFiles);
        }

        internal override IProjectEvaluationHandler CreateInstance()
        {
            return CreateInstance(null, null);
        }

        private static DynamicItemHandler CreateInstance(UnconfiguredProject? project = null, IWorkspaceProjectContext? context = null)
        {
            project ??= UnconfiguredProjectFactory.Create();

            var handler = new DynamicItemHandler(project);
            if (context != null)
                handler.Initialize(context);

            return handler;
        }
    }
}

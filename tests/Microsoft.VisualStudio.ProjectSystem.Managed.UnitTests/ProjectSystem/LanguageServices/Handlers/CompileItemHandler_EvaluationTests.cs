// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices.Handlers
{
    public class CompileItemHandler_EvaluationTests : EvaluationHandlerTestBase
    {
        [Fact]
        public void Constructor_NullAsProject_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>("project", () =>
            {
                new CompileItemHandler(null!);
            });
        }

        internal override IProjectEvaluationHandler CreateInstance()
        {
            return new CompileItemHandler(UnconfiguredProjectFactory.Create());
        }
    }
}

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Moq;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal static class IRoslynServicesFactory
    {
        public static Mock<IRoslynServices> Create()
        {
            return new Mock<IRoslynServices>();
        }

        public static Mock<IRoslynServices> Implement(Solution solution, bool changesApplied)
        {
            var mock = new Mock<IRoslynServices>();
           
            mock.Setup(h => h.RenameSymbolAsync(It.IsAny<Solution>(), It.IsAny<ISymbol>(), It.IsAny<string>()))
                .Returns(Task.FromResult(solution));

            mock.Setup(h => h.ApplyChangesToSolution(It.IsAny<Workspace>(), It.IsAny<Solution>()))
                .Returns(changesApplied);

            return mock;
        }
    }
}



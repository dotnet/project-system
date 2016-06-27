// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Moq;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal static class IRoslynServicesFactory
    {
        public static IRoslynServices Implement(Func<Solution, ISymbol, string, Task<Solution>> renameSymbolAsync, Func<Workspace,Solution,bool> applyChangesToSolution)
        {
            var mock = new Mock<IRoslynServices>();
           
            mock.Setup(h => h.RenameSymbolAsync(It.IsAny<Solution>(), It.IsAny<ISymbol>(), It.IsAny<string>()))
                .Returns(renameSymbolAsync);

            mock.Setup(h => h.ApplyChangesToSolution(It.IsAny<Workspace>(), It.IsAny<Solution>()))
                .Returns(applyChangesToSolution);

            return mock.Object;
        }
    }
}



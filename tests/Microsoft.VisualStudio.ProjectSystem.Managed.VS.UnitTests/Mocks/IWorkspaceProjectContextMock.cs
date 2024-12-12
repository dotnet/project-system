﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.LanguageServices.ProjectSystem;

internal class IWorkspaceProjectContextMock : AbstractMock<IWorkspaceProjectContext>
{
    public IWorkspaceProjectContextMock()
    {
        SetupAllProperties();

        Mock<IAsyncDisposable> batchScopeDisposable = new();

        batchScopeDisposable.Setup(o => o.DisposeAsync());

        Setup(o => o.CreateBatchScopeAsync(It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<IAsyncDisposable>(batchScopeDisposable.Object));
    }
}

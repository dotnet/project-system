// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Utilities;

public class DisposableBagTests
{
    [Fact]
    public void Dispose_WhenEmpty()
    {
        DisposableBag bag = new();

        bag.Dispose();
        bag.Dispose();
    }

    [Fact]
    public void Dispose_DisposesContents()
    {
        var disposable1 = new Mock<IDisposable>(MockBehavior.Strict);
        var disposable2 = new Mock<IDisposable>(MockBehavior.Strict);

        DisposableBag bag = new()
        {
            disposable1.Object,
            disposable2.Object
        };

        disposable1.Setup(o => o.Dispose());
        disposable2.Setup(o => o.Dispose());

        bag.Dispose();

        disposable1.VerifyAll();
        disposable2.VerifyAll();

        // Subsequent dispose does nothing
        bag.Dispose();

        disposable1.VerifyAll();
        disposable2.VerifyAll();
    }

    [Fact]
    public void Add_WhenAlreadyDisposed_DisposesAddedItem()
    {
        DisposableBag bag = new();

        bag.Dispose();

        var disposable = new Mock<IDisposable>(MockBehavior.Strict);
        disposable.Setup(o => o.Dispose());

        bag.Add(disposable.Object);

        disposable.VerifyAll();
    }
}

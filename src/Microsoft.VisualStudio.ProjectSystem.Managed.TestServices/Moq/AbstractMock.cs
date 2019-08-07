// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Moq
{
    internal abstract class AbstractMock<T> : Mock<T>
        where T : class
    {
        protected AbstractMock(MockBehavior behavior = MockBehavior.Loose)
            : base(behavior)
        {
            Switches = Switches.CollectDiagnosticFileInfoForSetups;
        }
    }
}

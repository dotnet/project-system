// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Query;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IEntityWithIdFactory
    {
        public static IEntityValue Create(string key, string value)
        {
            var mock = new Mock<IEntityWithId>();

            mock.SetupGet(m => m.Id).Returns(new EntityIdentity(key, value));

            var mockWithValue = mock.As<IEntityValue>();
            mockWithValue.SetupGet(m => m.EntityRuntime).Returns(IEntityRuntimeModelFactory.Create());

            return mockWithValue.Object;
        }
    }
}

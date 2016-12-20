// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Moq;

namespace Microsoft.VisualStudio.Shell.Interop
{
    public static class IVsStartupProjectsListServiceFactory
    {
        public static Mock<IVsStartupProjectsListService> CreateMockInstance(Guid projectGuid)
        {
            var mock = new Mock<IVsStartupProjectsListService>();

            // Some random guid from It.IsAny<Guid> cannot be used because the parameter is ref parameter.
            // This is a known restriction in Mock. Hence the object which is called here must be the same 
            // Guid object that is used in the test as well
            mock.Setup(s => s.AddProject(ref projectGuid));
            mock.Setup(s => s.RemoveProject(ref projectGuid));

            return mock;
        }
    }
}

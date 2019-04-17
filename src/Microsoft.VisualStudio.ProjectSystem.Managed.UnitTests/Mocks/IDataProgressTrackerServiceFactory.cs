// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Moq;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IDataProgressTrackerServiceFactory
    {
        public static IDataProgressTrackerService Create()
        {
            var mock = new Mock<IDataProgressTrackerService>();
            mock.Setup(s => s.RegisterOutputDataSource(It.IsAny<IProgressTrackerOutputDataSource>()))
                .Returns(IDataProgressTrackerServiceRegistrationFactory.Create());

            return mock.Object;
        }
    }
}

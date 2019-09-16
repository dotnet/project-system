// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
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

        public static IDataProgressTrackerService ImplementNotifyOutputDataCalculated(Action<IImmutableDictionary<NamedIdentity, IComparable>> action)
        {
            var registration = IDataProgressTrackerServiceRegistrationFactory.ImplementNotifyOutputDataCalculated(action);

            var mock = new Mock<IDataProgressTrackerService>();
            mock.Setup(s => s.RegisterOutputDataSource(It.IsAny<IProgressTrackerOutputDataSource>()))
                .Returns(registration);

            return mock.Object;
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

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

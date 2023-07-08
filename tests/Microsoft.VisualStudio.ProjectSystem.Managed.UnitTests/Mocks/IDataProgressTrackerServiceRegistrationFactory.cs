// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IDataProgressTrackerServiceRegistrationFactory
    {
        public static IDataProgressTrackerServiceRegistration Create()
        {
            return ImplementNotifyOutputDataCalculated(_ => { });
        }

        public static IDataProgressTrackerServiceRegistration ImplementNotifyOutputDataCalculated(Action<IImmutableDictionary<NamedIdentity, IComparable>> action)
        {
            var mock = new Mock<IDataProgressTrackerServiceRegistration>();
            mock.Setup(s => s.NotifyOutputDataCalculated(It.IsAny<IImmutableDictionary<NamedIdentity, IComparable>>()))
               .Callback(action);

            return mock.Object;
        }
    }
}

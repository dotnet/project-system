// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;

using Moq;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IDataProgressTrackerServiceRegistrationFactory
    {
        public static IDataProgressTrackerServiceRegistration Create()
        {
            var mock = new Mock<IDataProgressTrackerServiceRegistration>();
            mock.Setup(s => s.NotifyOutputDataCalculated(It.IsAny<IImmutableDictionary<NamedIdentity, IComparable>>()));

            return mock.Object;
        }
    }
}

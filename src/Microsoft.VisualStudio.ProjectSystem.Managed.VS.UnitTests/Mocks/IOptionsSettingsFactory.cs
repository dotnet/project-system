// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal static class IOptionsSettingsFactory
    {
        public static IOptionsSettings Implement<T>(Func<string, string, string, T, T> optionsSettingsValue)
        {
            var mock = new Mock<IOptionsSettings>();

            mock.Setup(h => h.GetPropertiesValue<T>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<T>()))
                .Returns(optionsSettingsValue);

            return mock.Object;
        }
    }
}
// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Moq;

namespace Microsoft.VisualStudio.Telemetry
{
    internal static class ILanguageServiceTelemetryServiceFactory
    {
        public static ILanguageServiceTelemetryService Create()
        {
            var mock = new Mock<ILanguageServiceTelemetryService>();

            return mock.Object;
        }
    }
}

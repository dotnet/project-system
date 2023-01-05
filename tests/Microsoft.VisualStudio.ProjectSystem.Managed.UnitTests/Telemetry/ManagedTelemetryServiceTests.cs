// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Moq.Protected;

namespace Microsoft.VisualStudio.Telemetry
{
    public class ManagedTelemetryServiceTests
    {
        [Fact]
        public void PostEvent_NullAsEventName_ThrowsArgumentNull()
        {
            var service = CreateInstance();

            Assert.Throws<ArgumentNullException>("eventName", () =>
            {
                service.PostEvent(null!);
            });
        }

        [Fact]
        public void PostEvent_EmptyAsEventName_ThrowsArgument()
        {
            var service = CreateInstance();

            Assert.Throws<ArgumentException>("eventName", () =>
            {
                service.PostEvent(string.Empty);
            });
        }

        [Fact]
        public void PostProperty_NullAsEventName_ThrowArgumentNull()
        {
            var service = CreateInstance();

            Assert.Throws<ArgumentNullException>("eventName", () =>
            {
                service.PostProperty(null!, "propName", "value");
            });
        }

        [Fact]
        public void PostProperty_EmptyAsEventName_ThrowArgument()
        {
            var service = CreateInstance();

            Assert.Throws<ArgumentException>("eventName", () =>
            {
                service.PostProperty(string.Empty, "propName", "value");
            });
        }

        [Fact]
        public void PostProperty_NullAsPropertyName_ThrowArgumentNull()
        {
            var service = CreateInstance();

            Assert.Throws<ArgumentNullException>("propertyName", () =>
            {
                service.PostProperty("event1", null!, "value");
            });
        }

        [Fact]
        public void PostProperty_EmptyAsPropertyName_ThrowArgument()
        {
            var service = CreateInstance();

            Assert.Throws<ArgumentException>("propertyName", () =>
            {
                service.PostProperty("event1", string.Empty, "value");
            });
        }

        [Fact]
        public void PostProperty_NullAsPropertyValue_ThrowArgumentNull()
        {
            var service = CreateInstance();

            Assert.Throws<ArgumentNullException>("propertyValue", () =>
            {
                service.PostProperty("event1", "propName", null!);
            });
        }

        [Fact]
        public void PostProperties_NullAsEventName_ThrowArgumentNull()
        {
            var service = CreateInstance();

            Assert.Throws<ArgumentNullException>("eventName", () =>
            {
                service.PostProperties(null!, new[] { ("propertyName", (object)"propertyValue") });
            });
        }

        [Fact]
        public void PostProperties_EmptyAsEventName_ThrowArgument()
        {
            var service = CreateInstance();

            Assert.Throws<ArgumentException>("eventName", () =>
            {
                service.PostProperties(string.Empty, new[] { ("propertyName", (object)"propertyValue") });
            });
        }

        [Fact]
        public void PostProperties_NullAsPropertyName_ThrowArgumentNull()
        {
            var service = CreateInstance();

            Assert.Throws<ArgumentNullException>("properties", () =>
            {
                service.PostProperties("event1", null!);
            });
        }

        [Fact]
        public void PostProperties_EmptyProperties_ThrowArgument()
        {
            var service = CreateInstance();

            Assert.Throws<ArgumentException>("properties", () =>
            {
                service.PostProperties("event1", Enumerable.Empty<(string propertyName, object propertyValue)>());
            });
        }

        [Fact]
        public void PostEvent_SendsTelemetryEvent()
        {
            TelemetryEvent? result = null;
            var service = CreateInstance((e) => { result = e; });

            service.PostEvent(TelemetryEventName.UpToDateCheckSuccess);

            Assert.NotNull(result);
            Assert.Equal(TelemetryEventName.UpToDateCheckSuccess, result!.Name);
        }

        [Fact]
        public void PostProperty_SendsTelemetryEventWithProperty()
        {
            TelemetryEvent? result = null;
            var service = CreateInstance((e) => { result = e; });

            service.PostProperty(TelemetryEventName.UpToDateCheckFail, TelemetryPropertyName.UpToDateCheck.FailReason, "Reason");

            Assert.NotNull(result);
            Assert.Equal(TelemetryEventName.UpToDateCheckFail, result.Name);
            Assert.Contains(new KeyValuePair<string, object>(TelemetryPropertyName.UpToDateCheck.FailReason, "Reason"), result.Properties);
        }

        [Fact]
        public void PostProperties_SendsTelemetryEventWithProperties()
        {
            TelemetryEvent? result = null;
            var service = CreateInstance((e) => { result = e; });

            service.PostProperties(TelemetryEventName.DesignTimeBuildComplete, new[]
            {
                (TelemetryPropertyName.DesignTimeBuildComplete.Succeeded, (object)true),
                (TelemetryPropertyName.DesignTimeBuildComplete.Targets, "Compile")
            });

            Assert.NotNull(result);
            Assert.Equal(TelemetryEventName.DesignTimeBuildComplete, result.Name);
            Assert.Contains(new KeyValuePair<string, object>(TelemetryPropertyName.DesignTimeBuildComplete.Succeeded, true), result.Properties);
            Assert.Contains(new KeyValuePair<string, object>(TelemetryPropertyName.DesignTimeBuildComplete.Targets, "Compile"), result.Properties);
        }

        private static ManagedTelemetryService CreateInstance(Action<TelemetryEvent>? action = null)
        {
            if (action is null)
                return new ManagedTelemetryService();

            // Override PostEventToSession to avoid actually sending to telemetry
            var mock = new Mock<ManagedTelemetryService>();
            mock.Protected().Setup("PostEventToSession", ItExpr.IsAny<TelemetryEvent>())
                .Callback(action);

            return mock.Object;
        }
    }
}

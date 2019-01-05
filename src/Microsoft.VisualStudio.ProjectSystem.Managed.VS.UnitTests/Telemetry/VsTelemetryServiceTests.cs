// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

using Moq;
using Moq.Protected;

using Xunit;

namespace Microsoft.VisualStudio.Telemetry
{
    public class VsTelemetryServiceTests
    {
        [Fact]
        public void PostFault_NullAsEventName_ThrowsArgumentNull()
        {
            var service = CreateInstance();
            var exception = new Exception();

            Assert.Throws<ArgumentNullException>("eventName", () =>
            {
                service.PostFault(null, exception);
            });
        }

        [Fact]
        public void PostFault_EmptyAsEventName_ThrowsArgumentNull()
        {
            var service = CreateInstance();
            var exception = new Exception();

            Assert.Throws<ArgumentException>("eventName", () =>
            {
                service.PostFault(string.Empty, exception);
            });
        }


        [Fact]
        public void PostFault_NullAsExceptionObject_ThrowsArgumentNull()
        {
            var service = CreateInstance();
            var exception = new Exception();

            Assert.Throws<ArgumentNullException>("exceptionObject", () =>
            {
                service.PostFault("vs/projectsystem/managed/fault", (Exception)null);
            });
        }

        [Fact]
        public void PostEvent_NullAsEventName_ThrowsArgumentNull()
        {
            var service = CreateInstance();

            Assert.Throws<ArgumentNullException>("eventName", () =>
            {
                service.PostEvent(null);
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
                service.PostProperty(null, null, null);
            });
        }

        [Fact]
        public void PostProperty_EmptyAsEventName_ThrowArgument()
        {
            var service = CreateInstance();

            Assert.Throws<ArgumentException>("eventName", () =>
            {
                service.PostProperty(string.Empty, null, null);
            });
        }

        [Fact]
        public void PostProperty_NullAsPropertyName_ThrowArgumentNull()
        {
            var service = CreateInstance();

            Assert.Throws<ArgumentNullException>("propertyName", () =>
            {
                service.PostProperty("event1", null, null);
            });
        }

        [Fact]
        public void PostProperty_EmptyAsPropertyName_ThrowArgument()
        {
            var service = CreateInstance();

            Assert.Throws<ArgumentException>("propertyName", () =>
            {
                service.PostProperty("event1", string.Empty, null);
            });
        }

        [Fact]
        public void PostProperty_NullAsPropertyValue_ThrowArgumentNull()
        {
            var service = CreateInstance();

            Assert.Throws<ArgumentNullException>("propertyValue", () =>
            {
                service.PostProperty("event1", "propName", null);
            });
        }

        [Fact]
        public void PostProperties_NullAsEventName_ThrowArgumentNull()
        {
            var service = CreateInstance();

            Assert.Throws<ArgumentNullException>("eventName", () =>
            {
                service.PostProperties(null, null);
            });
        }

        [Fact]
        public void PostProperties_EmptyAsEventName_ThrowArgument()
        {
            var service = CreateInstance();

            Assert.Throws<ArgumentException>("eventName", () =>
            {
                service.PostProperties(string.Empty, null);
            });
        }

        [Fact]
        public void PostProperties_NullAsPropertyName_ThrowArgumentNull()
        {
            var service = CreateInstance();

            Assert.Throws<ArgumentNullException>("properties", () =>
            {
                service.PostProperties("event1", null);
            });
        }

        [Fact]
        public void PostProperties_EmptyAsPropertyName_ThrowArgument()
        {
            var service = CreateInstance();

            Assert.Throws<ArgumentException>("properties", () =>
            {
                service.PostProperties("event1", new List<(string propertyName, object propertyValue)>());
            });
        }

        [Fact]
        public void PostFault_SendsFaultEvent()
        {
            TelemetryEvent result = null;
            var service = CreateInstance((e) => { result = e; });

            service.PostFault("vs/projectsystem/managed/fault", new Exception());

            Assert.Equal("vs/projectsystem/managed/fault", result.Name);
            Assert.IsType<FaultEvent>(result);
        }

        [Fact]
        public void PostEvent_SendsTelemetryEvent()
        {
            TelemetryEvent result = null;
            var service = CreateInstance((e) => { result = e; });

            service.PostEvent(TelemetryEventName.UpToDateCheckSuccess);

            Assert.Equal(TelemetryEventName.UpToDateCheckSuccess, result.Name);
        }

        [Fact]
        public void PostProperty_SendsTelemetryEventWithProperty()
        {
            TelemetryEvent result = null;
            var service = CreateInstance((e) => { result = e; });

            service.PostProperty(TelemetryEventName.UpToDateCheckFail, TelemetryPropertyName.UpToDateCheckFailReason, "Reason");

            Assert.Equal(TelemetryEventName.UpToDateCheckFail, result.Name);
            Assert.Contains(new KeyValuePair<string, object>(TelemetryPropertyName.UpToDateCheckFailReason, "Reason"), result.Properties);
        }

        [Fact]
        public void PostProperties_SendsTelemetryEventWithProperties()
        {
            TelemetryEvent result = null;
            var service = CreateInstance((e) => { result = e; });

            service.PostProperties(TelemetryEventName.DesignTimeBuildComplete, new[]
            {
                (TelemetryPropertyName.DesignTimeBuildCompleteSucceeded, (object)true),
                (TelemetryPropertyName.DesignTimeBuildCompleteTargets, "Compile")
            });

            Assert.Equal(TelemetryEventName.DesignTimeBuildComplete, result.Name);
            Assert.Contains(new KeyValuePair<string, object>(TelemetryPropertyName.DesignTimeBuildCompleteSucceeded, true), result.Properties);
            Assert.Contains(new KeyValuePair<string, object>(TelemetryPropertyName.DesignTimeBuildCompleteTargets, "Compile"), result.Properties);
        }

        private static VsTelemetryService CreateInstance(Action<TelemetryEvent> action = null)
        {
            if (action == null)
                return new VsTelemetryService();

            // Override PostEventToSession to avoid actually sending to telemetry
            var mock = new Mock<VsTelemetryService>();
            mock.Protected().Setup("PostEventToSession", ItExpr.IsAny<TelemetryEvent>())
                .Callback(action);

            return mock.Object;
        }
    }
}

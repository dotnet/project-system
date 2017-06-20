// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.ProjectSystem;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.Telemetry
{
    public class VsTelemetryServiceTests
    {
        [Fact]
        public void PostEvent_NullAsEventName_ThrowArgumentNull()
        {
            var service = CreateInstance();

            Assert.Throws<ArgumentNullException>("eventName", () => {
                service.PostEvent((string)null);
            });
        }

        [Fact]
        public void PostEvent_EmptyAsEventName_ThrowArgument()
        {
            var service = CreateInstance();

            Assert.Throws<ArgumentException>("eventName", () => {
                service.PostEvent(string.Empty);
            });
        }

        [Fact]
        public void PostProperty_NullAsEventName_ThrowArgumentNull()
        {
            var service = CreateInstance();

            Assert.Throws<ArgumentNullException>("eventName", () => {
                service.PostProperty(null, null, null);
            });
        }

        [Fact]
        public void PostProperty_EmptyAsEventName_ThrowArgument()
        {
            var service = CreateInstance();

            Assert.Throws<ArgumentException>("eventName", () => {
                service.PostProperty(string.Empty, null, null);
            });
        }

        [Fact]
        public void PostProperty_NullAsPropertyName_ThrowArgumentNull()
        {
            var service = CreateInstance();

            Assert.Throws<ArgumentNullException>("propertyName", () => {
                service.PostProperty("event1", null, null);
            });
        }

        [Fact]
        public void PostProperty_EmptyAsPropertyName_ThrowArgument()
        {
            var service = CreateInstance();

            Assert.Throws<ArgumentException>("propertyName", () => {
                service.PostProperty("event1", string.Empty, null);
            });
        }

        [Fact]
        public void PostProperty_NullAsPropertyValue_ThrowArgumentNull()
        {
            var service = CreateInstance();

            Assert.Throws<ArgumentNullException>("propertyValue", () => {
                service.PostProperty("event1", "propName", null);
            });
        }

        [Fact]
        public void PostProperty_EmptyAsPropertyValue_ThrowArgument()
        {
            var service = CreateInstance();

            Assert.Throws<ArgumentException>("propertyValue", () => {
                service.PostProperty("event1", "propName", string.Empty);

            });
        }


        [Fact]
        public void PostProperties_NullAsEventName_ThrowArgumentNull()
        {
            var service = CreateInstance();

            Assert.Throws<ArgumentNullException>("eventName", () => {
                service.PostProperties(null, null);
            });
        }

        [Fact]
        public void PostProperties_EmptyAsEventName_ThrowArgument()
        {
            var service = CreateInstance();

            Assert.Throws<ArgumentException>("eventName", () => {
                service.PostProperties(string.Empty, null);
            });
        }

        [Fact]
        public void PostProperties_NullAsPropertyName_ThrowArgumentNull()
        {
            var service = CreateInstance();

            Assert.Throws<ArgumentNullException>("properties", () => {
                service.PostProperties("event1", null);
            });
        }

        [Fact]
        public void PostProperties_EmptyAsPropertyName_ThrowArgument()
        {
            var service = CreateInstance();

            Assert.Throws<ArgumentException>("properties", () => {
                service.PostProperties("event1", new List<(string propertyName, string propertyValue)>());
            });
        }

        [Fact]
        public void PostOperation_NullAsEventName_ThrowArgumentNull()
        {
            var service = CreateInstance();

            Assert.Throws<ArgumentNullException>("eventName", () => {
                service.PostOperation((string)null, TelemetryResult.Failure, resultSummary:"resultSummary", correlatedWith:null);
            });
        }

        [Fact]
        public void PostOperation_EmptyAsEventName_ThrowArgument()
        {
            var service = CreateInstance();

            Assert.Throws<ArgumentException>("eventName", () => {
                service.PostOperation(string.Empty, TelemetryResult.Failure, resultSummary: "resultSummary", correlatedWith: null);
            });
        }

        [Fact]
        public void Report_NullAsEventName_ThrowArgumentNull()
        {
            var service = CreateInstance();

            Assert.Throws<ArgumentNullException>("eventName", () => {
                service.Report((string)null, "description", new Exception(), null);
            });
        }

        [Fact]
        public void Report_EmptyAsEventName_ThrowArgumentNull()
        {
            var service = CreateInstance();

            Assert.Throws<ArgumentException>("eventName", () => {
                service.Report(string.Empty, "description", new Exception(), null);
            });
        }

        [Fact]
        public void Report_NullAsException_ThrowArgumentNull()
        {
            var service = CreateInstance();

            Assert.Throws<ArgumentNullException>("exception", () => {
                service.Report("EventName", "description", (Exception)null, null);
            });
        }

        private static VsTelemetryService CreateInstance()
        {
            return new VsTelemetryService(Mock.Of<IUnconfiguredProjectCommonServices>());
        }
    }
}

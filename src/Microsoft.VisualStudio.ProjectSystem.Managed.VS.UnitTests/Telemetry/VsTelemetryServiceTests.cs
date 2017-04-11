// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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
            return new VsTelemetryService();
        }
    }
}

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;

using Xunit;

namespace Microsoft.VisualStudio.Threading.Tasks
{
    public sealed class CancellationSeriesTests
    {
        [Fact]
        public void GetToken_ReturnsNonCancelledToken()
        {
            using (var series = new CancellationSeries())
            {
                var token = series.GetToken();

                Assert.False(token.IsCancellationRequested);
                Assert.True(token.CanBeCanceled);
            }
        }

        [Fact]
        public void GetToken_CancelsPreviousToken()
        {
            using (var series = new CancellationSeries())
            {
                var token1 = series.GetToken();

                Assert.False(token1.IsCancellationRequested);

                var token2 = series.GetToken();

                Assert.True(token1.IsCancellationRequested);
                Assert.False(token2.IsCancellationRequested);

                var token3 = series.GetToken();

                Assert.True(token2.IsCancellationRequested);
                Assert.False(token3.IsCancellationRequested);
            }
        }

        [Fact]
        public void GetToken_ThrowsIfDisposed()
        {
            var series = new CancellationSeries();

            series.Dispose();

            Assert.Throws<ObjectDisposedException>(() => series.GetToken());
        }

        [Fact]
        public void GetToken_ReturnsCancelledTokenIfSuperTokenAlreadyCancelled()
        {
            var cts = new CancellationTokenSource();

            using (var series = new CancellationSeries(cts.Token))
            {
                cts.Cancel();

                var token = series.GetToken();

                Assert.True(token.IsCancellationRequested);
            }
        }

        [Fact]
        public void GetToken_ReturnsCancelledTokenIfInputTokenAlreadyCancelled()
        {
            var cts = new CancellationTokenSource();

            using (var series = new CancellationSeries())
            {
                cts.Cancel();

                var token = series.GetToken(cts.Token);

                Assert.True(token.IsCancellationRequested);
            }
        }

        [Fact]
        public void CancellingSuperTokenCancelsIssuedToken()
        {
            var cts = new CancellationTokenSource();

            using (var series = new CancellationSeries(cts.Token))
            {
                var token = series.GetToken();

                Assert.False(token.IsCancellationRequested);

                cts.Cancel();

                Assert.True(token.IsCancellationRequested);
            }
        }

        [Fact]
        public void CancellingInputTokenCancelsIssuedToken()
        {
            var cts = new CancellationTokenSource();

            using (var series = new CancellationSeries())
            {
                var token = series.GetToken(cts.Token);

                Assert.False(token.IsCancellationRequested);

                cts.Cancel();

                Assert.True(token.IsCancellationRequested);
            }
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    public class ProjectLoggingExtensionsTests
    {
        [Fact]
        public void BeginBatch_NullAsLogger_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>("outputService", () =>
            {
                new BatchLogger(null!);
            });
        }

        [Theory]
        [InlineData(int.MinValue)]
        [InlineData(-2)]
        [InlineData(-1)]
        public void BeginBatch_SetIndentLevelToLessThanZero_ThrowsArgumentOutOfRange(int indentLevel)
        {
            var logger = new MockOutputService();
            var batch = new BatchLogger(logger);

            Assert.Throws<ArgumentOutOfRangeException>("value", () =>
            {
                batch.IndentLevel = indentLevel;
            });
        }

        [Theory]
        [InlineData(0, "")]
        [InlineData(1, "    ")]
        [InlineData(2, "        ")]
        [InlineData(4, "                ")]
        public void BeginBatch_IndentLevel_AppendsIndentToWriteLine(int indentLevel, string expected)
        {
            var logger = new MockOutputService();

            using (var batch = new BatchLogger(logger))
            {
                batch.IndentLevel = indentLevel;
                batch.WriteLine(string.Empty);
            }

            Assert.Equal(expected, logger.Text);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void BeginBatch_IsEnabled_ReturnsLoggerIsEnabled(bool isEnabled)
        {
            var logger = new MockOutputService() { IsEnabled = isEnabled };

            var batch = new BatchLogger(logger);

            Assert.Equal(batch.IsEnabled, logger.IsEnabled);
        }

        [Fact]
        public void BeginBatch_WhenUnderlyingLoggerIsNotEnabled_DoesNotLog()
        {
            var logger = new MockOutputService() { IsEnabled = false };

            using (var batch = new BatchLogger(logger))
            {
                batch.WriteLine("Hello World!");
            }

            Assert.Null(logger.Text);
        }

        [Fact]
        public void BeginBatch_WhenUnderlyingLoggerIsEnabled_Logs()
        {
            var logger = new MockOutputService() { IsEnabled = true };

            using (var batch = new BatchLogger(logger))
            {
                batch.WriteLine("Hello World!");
            }

            Assert.Equal("Hello World!", logger.Text);
        }

        [Fact]
        public void BeginBatch_CanLogMultipleWriteLines()
        {
            var logger = new MockOutputService() { IsEnabled = true };

            using (var batch = new BatchLogger(logger))
            {
                batch.WriteLine("Line1");
                batch.IndentLevel = 1;
                batch.WriteLine("Line2");
                batch.IndentLevel = 0;
                batch.WriteLine("Line3");
            }

            // NOTE: No trailing new line, as the logger itself should be adding it
            Assert.Equal("Line1\r\n    Line2\r\nLine3", logger.Text, ignoreLineEndingDifferences: true);
        }

        private class MockOutputService : IManagedProjectDiagnosticOutputService
        {
            public bool IsEnabled { get; set; } = true;

            public string? Text { get; set; }

            public void WriteLine(string outputMessage)
            {
                Text = outputMessage;
            }
        }
    }
}

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.Logging
{
    public class ProjectLoggingExtensionsTests
    {
        [Fact]
        public void BeginBatch_NullAsLogger_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>("logger", () =>
            {
                ProjectLoggerExtensions.BeginBatch(null!);
            });
        }

        [Theory]
        [InlineData(int.MinValue)]
        [InlineData(-2)]
        [InlineData(-1)]
        public void BeginBatch_SetIndentLevelToLessThanZero_ThrowsArgumentOutOfRange(int indentLevel)
        {
            var logger = new ProjectLogger();
            var batch = ProjectLoggerExtensions.BeginBatch(logger);

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
            var logger = new ProjectLogger();

            using (var batch = ProjectLoggerExtensions.BeginBatch(logger))
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
            var logger = new ProjectLogger() { IsEnabled = isEnabled };

            var batch = ProjectLoggerExtensions.BeginBatch(logger);

            Assert.Equal(batch.IsEnabled, logger.IsEnabled);
        }

        [Fact]
        public void BeginBatch_WhenUnderlyingLoggerIsNotEnabled_DoesNotLog()
        {
            var logger = new ProjectLogger() { IsEnabled = false };

            using (var batch = ProjectLoggerExtensions.BeginBatch(logger))
            {
                batch.WriteLine("Hello World!");
            }

            Assert.Null(logger.Text);
        }

        [Fact]
        public void BeginBatch_WhenUnderlyingLoggerIsEnabled_Logs()
        {
            var logger = new ProjectLogger() { IsEnabled = true };

            using (var batch = ProjectLoggerExtensions.BeginBatch(logger))
            {
                batch.WriteLine("Hello World!");
            }

            Assert.Equal("Hello World!", logger.Text);
        }

        [Fact]
        public void BeginBatch_CanLogMultipleWriteLines()
        {
            var logger = new ProjectLogger() { IsEnabled = true };

            using (var batch = ProjectLoggerExtensions.BeginBatch(logger))
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

        private class ProjectLogger : IProjectLogger
        {
            public ProjectLogger()
            {
                IsEnabled = true;
            }

            public bool IsEnabled
            {
                get;
                set;
            }

            public string? Text
            {
                get;
                set;
            }

            public void WriteLine(in StringFormat format)
            {
                Text = format.Text;
            }
        }
    }
}

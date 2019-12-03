// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.ProjectSystem.Logging;
using Microsoft.VisualStudio.Shell.Interop;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Logging
{
    public class ProjectOutputWindowProjectLoggerTests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void IsEnabled_ReturnsReturnOfIsProjectOutputPaneEnabled(bool isProjectOutputPaneEnabled)
        {
            var options = IProjectSystemOptionsFactory.ImplementIsProjectOutputPaneEnabled(() => isProjectOutputPaneEnabled);

            var logger = CreateInstance(options: options);

            Assert.Equal(isProjectOutputPaneEnabled, logger.IsEnabled);
        }

        [Fact]
        public void WriteLine1_WhenNotEnabled_DoesNotLog()
        {
            bool wasCalled = false;
            var pane = IVsOutputWindowPaneFactory.ImplementOutputStringThreadSafe((text) => { wasCalled = true; });
            var logger = CreateDisabledLogger(pane);

            logger.WriteLine("Text");

            Assert.False(wasCalled);
        }

        [Fact]
        public void WriteLine2_WhenNotEnabled_DoesNotLog()
        {
            bool wasCalled = false;
            var pane = IVsOutputWindowPaneFactory.ImplementOutputStringThreadSafe((text) => { wasCalled = true; });
            var logger = CreateDisabledLogger(pane);

            logger.WriteLine("Text", new object());

            Assert.False(wasCalled);
        }

        [Fact]
        public void WriteLine3_WhenNotEnabled_DoesNotLog()
        {
            bool wasCalled = false;
            var pane = IVsOutputWindowPaneFactory.ImplementOutputStringThreadSafe((text) => { wasCalled = true; });
            var logger = CreateDisabledLogger(pane);

            logger.WriteLine("Text", new object(), new object());

            Assert.False(wasCalled);
        }

        [Fact]
        public void WriteLine4_WhenNotEnabled_DoesNotLog()
        {
            bool wasCalled = false;
            var pane = IVsOutputWindowPaneFactory.ImplementOutputStringThreadSafe((text) => { wasCalled = true; });
            var logger = CreateDisabledLogger(pane);

            logger.WriteLine("Text", new object(), new object(), new object());

            Assert.False(wasCalled);
        }

        [Fact]
        public void WriteLine5_WhenNotEnabled_DoesNotLog()
        {
            bool wasCalled = false;
            var pane = IVsOutputWindowPaneFactory.ImplementOutputStringThreadSafe((text) => { wasCalled = true; });
            var logger = CreateDisabledLogger(pane);

            logger.WriteLine("Text", new object[] { });

            Assert.False(wasCalled);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("Text")]
        [InlineData("Text with a placeholder {0}")] // Make we don't call String.Format
        public void WriteLine1_WhenEnabled_LogsToOutputPane(string text)
        {
            string? result = null;
            var pane = IVsOutputWindowPaneFactory.ImplementOutputStringThreadSafe((t) => { result = t; });
            var logger = CreateEnabledLogger(pane);

            logger.WriteLine(text);

            Assert.Equal(text + Environment.NewLine, result);
        }

        [Theory] // Format          Argument                Expected
        [InlineData("",             null,                   "")]
        [InlineData("",             "",                     "")]
        [InlineData("{0}",          "",                     "")]
        [InlineData("{0}",          null,                   "")]
        [InlineData("{0}",          "Hello",                "Hello")]
        [InlineData("{0} World!",   "Hello",                "Hello World!")]
        public void WriteLine2_WhenEnabled_LogsToOutputPane(string format, object? argument, string expected)
        {   // Not looking for exhaustive tests, just enough to indicate we're calling string.Format

            string? result = null;
            var pane = IVsOutputWindowPaneFactory.ImplementOutputStringThreadSafe((text) => { result = text; });
            var logger = CreateEnabledLogger(pane);

            logger.WriteLine(format, argument);

            Assert.Equal(expected + Environment.NewLine, result);
        }

        [Theory] // Format              Argument1    Argument2             Expected
        [InlineData("",                 null,        null,                 "")]
        [InlineData("",                 "",          "",                   "")]
        [InlineData("{0}",              "",          "",                   "")]
        [InlineData("{0}",              null,        null,                 "")]
        [InlineData("{0}",              "Hello",     "Hello",              "Hello")]
        [InlineData("{0} {1}!",         "Hello",     "World",              "Hello World!")]
        [InlineData("{0} {1}!",         "1",         "2",                  "1 2!")]
        public void WriteLine3_WhenEnabled_LogsToOutputPane(string format, object? argument1, object? argument2, string expected)
        {   // Not looking for exhaustive tests, just enough to indicate we're calling string.Format

            string? result = null;
            var pane = IVsOutputWindowPaneFactory.ImplementOutputStringThreadSafe((text) => { result = text; });
            var logger = CreateEnabledLogger(pane);

            logger.WriteLine(format, argument1, argument2);

            Assert.Equal(expected + Environment.NewLine, result);
        }

        [Theory] // Format               Argument1    Argument2     Argument3           Expected
        [InlineData("",                  null,        null,         null,               "")]
        [InlineData("",                  "",          "",           "",                 "")]
        [InlineData("{0}",               "",          "",           "",                 "")]
        [InlineData("{0}",               null,        null,         null,               "")]
        [InlineData("{0}",               "Hello",     "Hello",      "Hello",            "Hello")]
        [InlineData("{0} {1}!",          "Hello",     "World",      "World",            "Hello World!")]
        [InlineData("{0} {1} {2}!",      "Hello",     "Again",      "World",            "Hello Again World!")]
        [InlineData("{0} {1} {2}!",      "1",         "2",          "3",                "1 2 3!")]
        public void WriteLine4_WhenEnabled_LogsToOutputPane(string format, object? argument1, object? argument2, object? argument3, string expected)
        {   // Not looking for exhaustive tests, just enough to indicate we're calling string.Format

            string? result = null;
            var pane = IVsOutputWindowPaneFactory.ImplementOutputStringThreadSafe((text) => { result = text; });
            var logger = CreateEnabledLogger(pane);

            logger.WriteLine(format, argument1, argument2, argument3);

            Assert.Equal(expected + Environment.NewLine, result);
        }

        [Theory] // Format               Arguments                                          Expected
        [InlineData("",                  new object?[] { null },                             "")]
        [InlineData("{0}",               new object?[] { null },                             "")]
        [InlineData("{0}{1}",            new object?[] { null, null },                       "")]
        [InlineData("{0}{1}{2}",         new object?[] { null, null, null },                 "")]
        [InlineData("{0}{1}{2}{3}",      new object?[] { null, null, null, null },           "")]
        [InlineData("{0} {1} {2} {3}!",  new object?[] { "Why", "Hello", "Again", "World"},  "Why Hello Again World!")]
        public void WriteLine5_WhenEnabled_LogsToOutputPane(string format, object?[] arguments, string expected)
        {   // Not looking for exhaustive tests, just enough to indicate we're calling string.Format

            string? result = null;
            var pane = IVsOutputWindowPaneFactory.ImplementOutputStringThreadSafe((text) => { result = text; });
            var logger = CreateEnabledLogger(pane);

            logger.WriteLine(format, arguments);

            Assert.Equal(expected + Environment.NewLine, result);
        }

        [Fact]
        public void WriteLine2_WhenEnabledWithNullFormat_ThrowsArgumentNull()
        {
            string? result = null;
            var pane = IVsOutputWindowPaneFactory.ImplementOutputStringThreadSafe((text) => { result = text; });
            var logger = CreateEnabledLogger(pane);

            Assert.Throws<ArgumentNullException>("format", () =>
            {
                logger.WriteLine(format: null!, (object?)null!);
            });
        }

        [Fact]
        public void WriteLine3_WhenEnabledWithNullFormat_ThrowsArgumentNull()
        {
            string? result = null;
            var pane = IVsOutputWindowPaneFactory.ImplementOutputStringThreadSafe((text) => { result = text; });
            var logger = CreateEnabledLogger(pane);

            Assert.Throws<ArgumentNullException>("format", () =>
            {
                logger.WriteLine(format: null!, null, null);
            });
        }

        [Fact]
        public void WriteLine4_WhenEnabledWithNullFormat_ThrowsArgumentNull()
        {
            string? result = null;
            var pane = IVsOutputWindowPaneFactory.ImplementOutputStringThreadSafe((text) => { result = text; });
            var logger = CreateEnabledLogger(pane);

            Assert.Throws<ArgumentNullException>("format", () =>
            {
                logger.WriteLine(format: null!, null, null, null);
            });
        }

        [Fact]
        public void WriteLine5_WhenEnabledWithNullFormat_ThrowsArgumentNull()
        {
            string? result = null;
            var pane = IVsOutputWindowPaneFactory.ImplementOutputStringThreadSafe((text) => { result = text; });
            var logger = CreateEnabledLogger(pane);

            Assert.Throws<ArgumentNullException>("format", () =>
            {
                logger.WriteLine(format: null!, null, null, null, null);
            });
        }

        [Fact]
        public void WriteLine2_WhenEnabledWithInvalidFormat_ThrowsFormat()
        {
            string? result = null;
            var pane = IVsOutputWindowPaneFactory.ImplementOutputStringThreadSafe((text) => { result = text; });
            var logger = CreateEnabledLogger(pane);

            Assert.Throws<FormatException>(() =>
            {
                logger.WriteLine("{0}{1}", new object());
            });
        }

        [Fact]
        public void WriteLine3_WhenEnabledWithInvalidFormat_ThrowsFormat()
        {
            string? result = null;
            var pane = IVsOutputWindowPaneFactory.ImplementOutputStringThreadSafe((text) => { result = text; });
            var logger = CreateEnabledLogger(pane);

            Assert.Throws<FormatException>(() =>
            {
                logger.WriteLine("{0}{1}{2}", new object(), new object());
            });
        }

        [Fact]
        public void WriteLine4_WhenEnabledWithInvalidFormat_ThrowsFormat()
        {
            string? result = null;
            var pane = IVsOutputWindowPaneFactory.ImplementOutputStringThreadSafe((text) => { result = text; });
            var logger = CreateEnabledLogger(pane);

            Assert.Throws<FormatException>(() =>
            {
                logger.WriteLine("{0}{1}{2}{4}", new object(), new object(), new object());
            });
        }

        [Fact]
        public void WriteLine5_WhenEnabledWithInvalidFormat_ThrowsFormat()
        {
            string? result = null;
            var pane = IVsOutputWindowPaneFactory.ImplementOutputStringThreadSafe((text) => { result = text; });
            var logger = CreateEnabledLogger(pane);

            Assert.Throws<FormatException>(() =>
            {
                logger.WriteLine("{0}{1}{2}{4}{5}", new object(), new object(), new object(), new object());
            });
        }

        [Fact]
        public void WriteLine2_WhenDisabledWithInvalidFormat_DoesNothing()
        {
            string? result = null;
            var pane = IVsOutputWindowPaneFactory.ImplementOutputStringThreadSafe((text) => { result = text; });
            var logger = CreateDisabledLogger(pane);

            logger.WriteLine("{0}{1}", new object());
        }

        [Fact]
        public void WriteLine3_WhenDisabledWithInvalidFormat_DoesNothing()
        {
            string? result = null;
            var pane = IVsOutputWindowPaneFactory.ImplementOutputStringThreadSafe((text) => { result = text; });
            var logger = CreateDisabledLogger(pane);

            logger.WriteLine("{0}{1}{2}", new object(), new object());
        }

        [Fact]
        public void WriteLine4_WhenDisabledWithInvalidFormat_DoesNothing()
        {
            string? result = null;
            var pane = IVsOutputWindowPaneFactory.ImplementOutputStringThreadSafe((text) => { result = text; });
            var logger = CreateDisabledLogger(pane);

            logger.WriteLine("{0}{1}{2}{4}", new object(), new object(), new object());
        }

        [Fact]
        public void WriteLine5_WhenDisabledWithInvalidFormat_DoesNothing()
        {
            string? result = null;
            var pane = IVsOutputWindowPaneFactory.ImplementOutputStringThreadSafe((text) => { result = text; });
            var logger = CreateDisabledLogger(pane);

            logger.WriteLine("{0}{1}{2}{4}{5}", new object(), new object(), new object(), new object());
        }

        private static ProjectOutputWindowProjectLogger CreateEnabledLogger(IVsOutputWindowPane pane)
        {
            return CreateLogger(pane, enabled: true);
        }

        private static ProjectOutputWindowProjectLogger CreateDisabledLogger(IVsOutputWindowPane pane)
        {
            return CreateLogger(pane, enabled: false);
        }

        private static ProjectOutputWindowProjectLogger CreateLogger(IVsOutputWindowPane pane, bool enabled)
        {
            var options = IProjectSystemOptionsFactory.ImplementIsProjectOutputPaneEnabled(() => enabled);
            var outputWindowProvider = IProjectOutputWindowPaneProviderFactory.ImplementGetOutputWindowPaneAsync(pane);

            return CreateInstance(options: options, outputWindowProvider: outputWindowProvider);
        }

        private static ProjectOutputWindowProjectLogger CreateInstance(
            IProjectThreadingService? threadingService = null,
            IProjectSystemOptions? options = null,
            IProjectOutputWindowPaneProvider? outputWindowProvider = null)
        {
            threadingService ??= IProjectThreadingServiceFactory.Create();
            options ??= IProjectSystemOptionsFactory.Create();
            outputWindowProvider ??= IProjectOutputWindowPaneProviderFactory.Create();

            return new ProjectOutputWindowProjectLogger(threadingService, options, outputWindowProvider);
        }
    }
}

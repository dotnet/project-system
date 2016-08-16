// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Shell.Interop;
using Xunit;
using Microsoft.Build.Framework;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Build
{
    [ProjectSystemTrait]
    public class LanguageServiceErrorListProviderTests
    {
        [Fact]
        public void Constructor_NullAsUnconfiguedProject_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>("unconfiguredProject", () => {
                new LanguageServiceErrorListProvider((UnconfiguredProject)null);
            });
        }

        [Fact]
        public void SuspendRefresh_DoesNotThrow()
        {
            var provider = CreateInstance();
            provider.SuspendRefresh();
        }

        [Fact]
        public void ResumeRefresh_DoesNotThrow()
        {
            var provider = CreateInstance();
            provider.ResumeRefresh();
        }

        [Fact]
        public void ClearAllAsync_WhenNoProjectsWithIntellisense_ReturnsCompletedTask()
        {
            var provider = CreateInstance();

            var result = provider.ClearAllAsync();

            Assert.True(result.IsCompleted);
        }

        [Fact]
        public void ClearMessageFromTargetAsync_WhenNoProjectsWithIntellisense_ReturnsCompletedTask()
        {
            var provider = CreateInstance();

            var result = provider.ClearMessageFromTargetAsync("targetName");

            Assert.True(result.IsCompleted);
        }

        [Fact]
        public async void ClearAllAsync_WhenProjectWithIntellisense_CallsClearErrors()
        {
            int callCount = 0;
            var reporter = IVsLanguageServiceBuildErrorReporter2Factory.ImplementClearErrors(() => { callCount++; return 0; });
            var project = IProjectWithIntellisenseFactory.ImplementGetExternalErrorReporter(reporter);
            var task = CreateDefaultTask();
            var provider = CreateInstance(project);

            await provider.AddMessageAsync(task);   // Force initialization

            await provider.ClearAllAsync();

            Assert.Equal(1, callCount);
        }


        [Fact]
        public async void AddMessageAsync_NullAsTask_ThrowsArgumentNull()
        {
            var provider = CreateInstance();

            await Assert.ThrowsAsync<ArgumentNullException>("error", () => {
                return provider.AddMessageAsync((TargetGeneratedError)null);
            });
        }

        [Fact]
        public async void AddMessageAsync_WhenNoProjectsWithIntellisense_ReturnsNotHandled()
        {
            var provider = CreateInstance();

            var task = new TargetGeneratedError("Test", new BuildErrorEventArgs(null, "Code", "File", 1, 1, 1, 1, "Message", "HelpKeyword", "Sender"));

            var result = await provider.AddMessageAsync(task);

            Assert.Equal(result, AddMessageResult.NotHandled);
        }

        [Fact]
        public async void AddMessageAsync_UnrecognizedArgsAsTaskBuildEventArgs_ReturnsNotHandled()
        {
            var provider = CreateInstance();

            var task = new TargetGeneratedError("Test", new LazyFormattedBuildEventArgs("Message", "HelpKeyword", "SenderName"));

            var result = await provider.AddMessageAsync(task);

            Assert.Equal(result, AddMessageResult.NotHandled);
        }


        [Fact]
        public async void AddMessageAsync_ArgsWithNoCodeAsTask_ReturnsNotHandled()
        {
            var provider = CreateInstance();

            var task = new TargetGeneratedError("Test", new BuildErrorEventArgs(null, "" /* Code */, "File", 1, 1, 1, 1, "Message", "HelpKeyword", "Sender"));

            var result = await provider.AddMessageAsync(task);

            Assert.Equal(result, AddMessageResult.NotHandled);
        }

        [Fact]
        public async void AddMessageAsync_WhenReporterThrowsNotImplemented_ReturnsNotHandled()
        {
            var reporter = IVsLanguageServiceBuildErrorReporter2Factory.ImplementReportError((string bstrErrorMessage, string bstrErrorId, VSTASKPRIORITY nPriority, int iLine, int iColumn, int iEndLine, int iEndColumn, string bstrFileName) =>
            {
                throw new NotImplementedException();
            });

            var project = IProjectWithIntellisenseFactory.ImplementGetExternalErrorReporter(reporter);
            var provider = CreateInstance(project);
            var task = CreateDefaultTask();

            var result = await provider.AddMessageAsync(task);

            Assert.Equal(result, AddMessageResult.NotHandled);
        }

        [Fact]
        public async void AddMessageAsync_WhenReporterThrows_Throws()
        {
            var reporter = IVsLanguageServiceBuildErrorReporter2Factory.ImplementReportError((string bstrErrorMessage, string bstrErrorId, VSTASKPRIORITY nPriority, int iLine, int iColumn, int iEndLine, int iEndColumn, string bstrFileName) =>
            {
                throw new Exception();
            });

            var project = IProjectWithIntellisenseFactory.ImplementGetExternalErrorReporter(reporter);
            var provider = CreateInstance(project);
            var task = CreateDefaultTask();

            await Assert.ThrowsAsync<Exception>(() => {
                return provider.AddMessageAsync(task);
            });
        }

        [Fact]
        public async void AddMessageAsync_ReturnsHandledAndStopProcessing()
        {
            var reporter = IVsLanguageServiceBuildErrorReporter2Factory.ImplementReportError((string bstrErrorMessage, string bstrErrorId, VSTASKPRIORITY nPriority, int iLine, int iColumn, int iEndLine, int iEndColumn, string bstrFileName) => { });
            var project = IProjectWithIntellisenseFactory.ImplementGetExternalErrorReporter(reporter);
            var provider = CreateInstance(project);
            var task = CreateDefaultTask();

            var result = await provider.AddMessageAsync(task);

            Assert.Equal(result, AddMessageResult.HandledAndStopProcessing);
        }

        [Fact]
        public async void AddMessageAsync_WarningTaskAsTask_PassesTP_NORMALAsPriority()
        {
            VSTASKPRIORITY? result = null;
            var reporter = IVsLanguageServiceBuildErrorReporter2Factory.ImplementReportError((string bstrErrorMessage, string bstrErrorId, VSTASKPRIORITY nPriority, int iLine, int iColumn, int iEndLine, int iEndColumn, string bstrFileName) => {
                result = nPriority;
            });
            var project = IProjectWithIntellisenseFactory.ImplementGetExternalErrorReporter(reporter);
            var provider = CreateInstance(project);

            await provider.AddMessageAsync(new TargetGeneratedError("Test", new BuildWarningEventArgs(null, "Code", "File", 1, 1, 1, 1, "Message", "HelpKeyword", "Sender")));

            Assert.Equal(result, VSTASKPRIORITY.TP_NORMAL);
        }

        [Fact]
        public async void AddMessageAsync_ErrorTaskAsTask_PassesTP_HIGHAsPriority()
        {
            VSTASKPRIORITY? result = null;
            var reporter = IVsLanguageServiceBuildErrorReporter2Factory.ImplementReportError((string bstrErrorMessage, string bstrErrorId, VSTASKPRIORITY nPriority, int iLine, int iColumn, int iEndLine, int iEndColumn, string bstrFileName) => {
                result = nPriority;
            });
            var project = IProjectWithIntellisenseFactory.ImplementGetExternalErrorReporter(reporter);
            var provider = CreateInstance(project);

            await provider.AddMessageAsync(new TargetGeneratedError("Test", new BuildErrorEventArgs(null, "Code", "File", 1, 1, 1, 1, "Message", "HelpKeyword", "Sender")));

            Assert.Equal(result, VSTASKPRIORITY.TP_HIGH);
        }

        [Fact]
        public async void AddMessageAsync_CriticalBuildMessageTaskAsTask_PassesTP_LOWAsPriority()
        {
            VSTASKPRIORITY? result = null;
            var reporter = IVsLanguageServiceBuildErrorReporter2Factory.ImplementReportError((string bstrErrorMessage, string bstrErrorId, VSTASKPRIORITY nPriority, int iLine, int iColumn, int iEndLine, int iEndColumn, string bstrFileName) => {
                result = nPriority;
            });
            var project = IProjectWithIntellisenseFactory.ImplementGetExternalErrorReporter(reporter);
            var provider = CreateInstance(project);

            await provider.AddMessageAsync(new TargetGeneratedError("Test", new CriticalBuildMessageEventArgs(null, "Code", "File", 1, 1, 1, 1, "Message", "HelpKeyword", "Sender")));

            Assert.Equal(result, VSTASKPRIORITY.TP_LOW);
        }

        //          ErrorMessage                                    Code         
        [Theory]
        [InlineData(null,                                           "A")]
        [InlineData("",                                             "0000")]
        [InlineData(" ",                                            "1000")]          
        [InlineData("This is an error message.",                    "CA1000")]       
        [InlineData("This is an error message\r\n",                 "CS1000")]
        [InlineData("This is an error message.\r\n",                "BC1000")]
        [InlineData("This is an error message.\r\n.And another",    "BC1000\r\n")]
        public async void AddMessageAsync_BuildErrorAsTask_CallsReportErrorSettingErrorMessageAndCode(string errorMessage, string code)
        {
            string errorMessageResult = "NotSet";
            string errorIdResult = "NotSet";
            var reporter = IVsLanguageServiceBuildErrorReporter2Factory.ImplementReportError((string bstrErrorMessage, string bstrErrorId, VSTASKPRIORITY nPriority, int iLine, int iColumn, int iEndLine, int iEndColumn, string bstrFileName) =>
            {
                errorMessageResult = bstrErrorMessage;
                errorIdResult = bstrErrorId;
            });

            var project = IProjectWithIntellisenseFactory.ImplementGetExternalErrorReporter(reporter);

            var provider = CreateInstance(project);
            await provider.AddMessageAsync(new TargetGeneratedError("Test", new BuildErrorEventArgs(null, code, "File", 0, 0, 0, 0, errorMessage, "HelpKeyword", "Sender")));


            Assert.Equal(errorMessage, errorMessageResult);
            Assert.Equal(code, errorIdResult);
        }

        //          Line        Column          Expected Line  Expected Column
        [Theory]
        [InlineData(   0,            0,                     0,               0)]       // Is this the right behavior? See https://github.com/dotnet/roslyn-project-system/issues/145
        [InlineData(   0,           -1,                     0,               0)]       // Is this the right behavior?
        [InlineData(  -1,            0,                     0,               0)]       // Is this the right behavior?
        [InlineData(   1,            0,                     0,               0)]
        [InlineData(   0,            1,                     0,               0)]
        [InlineData(   2,            2,                     1,               1)]
        [InlineData(  10,          100,                     9,              99)]
        [InlineData( 100,           10,                    99,               9)]
        [InlineData( 100,          100,                    99,              99)]
        public async void AddMessageAsync_BuildErrorAsTask_CallsReportErrorSettingLineAndColumnAdjustingBy1(int lineNumber, int columnNumber, int expectedLineNumber, int expectedColumnNumber)
        {
            int? lineResult = null;
            int? columnResult = null;
            var reporter = IVsLanguageServiceBuildErrorReporter2Factory.ImplementReportError((string bstrErrorMessage, string bstrErrorId, VSTASKPRIORITY nPriority, int iLine, int iColumn, int iEndLine, int iEndColumn, string bstrFileName) =>
            {
                lineResult = iLine;
                columnResult = iColumn;
            });

            var project = IProjectWithIntellisenseFactory.ImplementGetExternalErrorReporter(reporter);

            var provider = CreateInstance(project);
            await provider.AddMessageAsync(new TargetGeneratedError("Test", new BuildErrorEventArgs(null, "Code", "File", lineNumber, columnNumber, 0, 0, "ErrorMessage", "HelpKeyword", "Sender")));


            Assert.Equal(expectedLineNumber, lineResult);
            Assert.Equal(expectedColumnNumber, columnResult);
        }

        //          Line        Column      End Line     End Column             Expected End Line  Expected End Column
        [Theory]
        [InlineData(   0,            0,            0,             0,                            0,                   0)]       // Is this the right behavior? See https://github.com/dotnet/roslyn-project-system/issues/145
        [InlineData(   0,           -1,            0,             0,                            0,                   0)]       // Is this the right behavior?
        [InlineData(  -1,            0,            0,             0,                            0,                   0)]       // Is this the right behavior?
        [InlineData(   1,            0,            0,             0,                            0,                   0)]
        [InlineData(   0,            1,            0,             0,                            0,                   0)]
        [InlineData(  10,          100,            0,             0,                            9,                  99)]
        [InlineData( 100,           10,            0,             0,                           99,                   9)]
        [InlineData( 100,          100,          100,           100,                           99,                  99)]
        [InlineData( 100,          100,          101,           102,                          100,                 101)]
        [InlineData( 100,          101,            1,             1,                           99,                 100)]       //  Roslyn's ProjectExternalErrorReporter throws if end is less than start
        public async void AddMessageAsync_BuildErrorAsTask_CallsReportErrorSettingEndLineAndColumn(int lineNumber, int columnNumber, int endLineNumber, int endColumnNumber, int expectedEndLineNumber, int expectedEndColumnNumber)
        {
            int? endLineResult = null;
            int? endColumnResult = null;
            var reporter = IVsLanguageServiceBuildErrorReporter2Factory.ImplementReportError((string bstrErrorMessage, string bstrErrorId, VSTASKPRIORITY nPriority, int iLine, int iColumn, int iEndLine, int iEndColumn, string bstrFileName) =>
            {
                endLineResult = iEndLine;
                endColumnResult = iEndColumn;
            });

            var project = IProjectWithIntellisenseFactory.ImplementGetExternalErrorReporter(reporter);

            var provider = CreateInstance(project);
            await provider.AddMessageAsync(new TargetGeneratedError("Test", new BuildErrorEventArgs(null, "Code", "File", lineNumber, columnNumber, endLineNumber, endColumnNumber, "ErrorMessage", "HelpKeyword", "Sender")));


            Assert.Equal(expectedEndLineNumber, endLineResult);
            Assert.Equal(expectedEndColumnNumber, endColumnResult);
        }

        //          File                                        ProjectFile                             ExpectedFileName
        [Theory]                                                                                        
        [InlineData(null,                                       null,                                   @"")]
        [InlineData(@"",                                        @"",                                    @"")]
        [InlineData(@"Foo.txt",                                 @"",                                    @"")]                // Is this the right behavior?  See https://github.com/dotnet/roslyn-project-system/issues/146
        [InlineData(@"C:\Foo.txt",                              @"",                                    @"")]                // Is this the right behavior?  See https://github.com/dotnet/roslyn-project-system/issues/146
        [InlineData(@"C:\Foo.txt",                              @"C:\MyProject.csproj",                 @"C:\Foo.txt")]
        [InlineData(@"Foo.txt",                                 @"C:\MyProject.csproj",                 @"C:\Foo.txt")]
        [InlineData(@"..\Foo.txt",                              @"C:\Bar\MyProject.csproj",             @"C:\Foo.txt")]
        [InlineData(@"..\Foo.txt",                              @"MyProject.csproj",                    @"")]
        [InlineData(@"..\Foo.txt",                              @"<>",                                  @"")]
        [InlineData(@"<>",                                      @"C:\MyProject.csproj",                 @"C:\<>")]
        [InlineData(@"C:\MyProject.csproj",                     @"C:\MyProject.csproj",                 @"C:\MyProject.csproj")]
        [InlineData(@"C:\Foo\..\MyProject.csproj",              @"C:\MyProject.csproj",                 @"C:\MyProject.csproj")]
        [InlineData(@"C:\Foo\Foo.txt",                          @"C:\Bar\MyProject.csproj",             @"C:\Foo\Foo.txt")]
        [InlineData(@"Foo.txt",                                 @"C:\Bar\MyProject.csproj",             @"C:\Bar\Foo.txt")]
        public async void AddMessageAsync_BuildErrorAsTask_CallsReportErrorSettingFileName(string file, string projectFile, string expectedFileName)
        {
            string fileNameResult = "NotSet";
            var reporter = IVsLanguageServiceBuildErrorReporter2Factory.ImplementReportError((string bstrErrorMessage, string bstrErrorId, VSTASKPRIORITY nPriority, int iLine, int iColumn, int iEndLine, int iEndColumn, string bstrFileName) =>
            {
                fileNameResult = bstrFileName;
            });

            var project = IProjectWithIntellisenseFactory.ImplementGetExternalErrorReporter(reporter);

            var provider = CreateInstance(project);

            var args = new BuildErrorEventArgs(null, "Code", file, 0, 0, 0, 0, "ErrorMessage", "HelpKeyword", "Sender");
            args.ProjectFile = projectFile;
            await provider.AddMessageAsync(new TargetGeneratedError("Test", args));

            Assert.Equal(expectedFileName, fileNameResult);
        }

        private static TargetGeneratedError CreateDefaultTask()
        {
            return new TargetGeneratedError("Test", new BuildErrorEventArgs(null, "Code", "File", 1, 1, 1, 1, "Message", "HelpKeyword", "Sender"));
        }
        
        private static LanguageServiceErrorListProvider CreateInstance()
        {
            return CreateInstance(null);
        }

        private static LanguageServiceErrorListProvider CreateInstance(IProjectWithIntellisense project)
        {
            var provider = new LanguageServiceErrorListProvider(IUnconfiguredProjectFactory.Create("CSharp"));

            if (project != null)
                provider.ProjectsWithIntellisense.Add(project, "CSharp");

            return provider;
        }
    }
}

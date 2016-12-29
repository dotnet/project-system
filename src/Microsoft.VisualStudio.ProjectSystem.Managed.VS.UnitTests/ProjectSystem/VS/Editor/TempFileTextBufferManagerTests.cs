// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.IO;
using Microsoft.VisualStudio.ProjectSystem.VS.Build;
using Microsoft.VisualStudio.ProjectSystem.VS.Utilities;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.TextManager.Interop;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Editor
{
    [ProjectSystemTrait]
    public class TempFileTextBufferManagerTests
    {
        [Fact]
        public void TempFileTextBufferManager_NullProject_Throws()
        {
            Assert.Throws<ArgumentNullException>("unconfiguredProject", () => new TempFileTextBufferManager(
                null,
                IMsBuildAccessorFactory.Create(),
                IVsEditorAdaptersFactoryServiceFactory.Create(),
                ITextDocumentFactoryServiceFactory.Create(),
                new TestShellUtilitiesHelper(),
                IFileSystemFactory.Create(),
                IProjectThreadingServiceFactory.Create(),
                IServiceProviderFactory.Create()));
        }

        [Fact]
        public void TempFileTextBufferManager_NullMsBuildAccessor_Throws()
        {
            Assert.Throws<ArgumentNullException>("msbuildAccessor", () => new TempFileTextBufferManager(
                UnconfiguredProjectFactory.Create(),
                null,
                IVsEditorAdaptersFactoryServiceFactory.Create(),
                ITextDocumentFactoryServiceFactory.Create(),
                new TestShellUtilitiesHelper(),
                IFileSystemFactory.Create(),
                IProjectThreadingServiceFactory.Create(),
                IServiceProviderFactory.Create()));
        }

        [Fact]
        public void TempFileTextBufferManager_NullEditorService_Throws()
        {
            Assert.Throws<ArgumentNullException>("editorAdaptersService", () => new TempFileTextBufferManager(
                UnconfiguredProjectFactory.Create(),
                IMsBuildAccessorFactory.Create(),
                null,
                ITextDocumentFactoryServiceFactory.Create(),
                new TestShellUtilitiesHelper(),
                IFileSystemFactory.Create(),
                IProjectThreadingServiceFactory.Create(),
                IServiceProviderFactory.Create()));
        }

        [Fact]
        public void TempFileTextBufferManager_NullTextDocumentService_Throws()
        {
            Assert.Throws<ArgumentNullException>("textDocumentService", () => new TempFileTextBufferManager(
                UnconfiguredProjectFactory.Create(),
                IMsBuildAccessorFactory.Create(),
                IVsEditorAdaptersFactoryServiceFactory.Create(),
                null,
                new TestShellUtilitiesHelper(),
                IFileSystemFactory.Create(),
                IProjectThreadingServiceFactory.Create(),
                IServiceProviderFactory.Create()));
        }

        [Fact]
        public void TempFileTextBufferManager_NullShellUtilities_Throws()
        {
            Assert.Throws<ArgumentNullException>("shellUtilities", () => new TempFileTextBufferManager(
                UnconfiguredProjectFactory.Create(),
                IMsBuildAccessorFactory.Create(),
                IVsEditorAdaptersFactoryServiceFactory.Create(),
                ITextDocumentFactoryServiceFactory.Create(),
                null,
                IFileSystemFactory.Create(),
                IProjectThreadingServiceFactory.Create(),
                IServiceProviderFactory.Create()));
        }

        [Fact]
        public void TempFileTextBufferManager_NullFileSystem_Throws()
        {
            Assert.Throws<ArgumentNullException>("fileSystem", () => new TempFileTextBufferManager(
                UnconfiguredProjectFactory.Create(),
                IMsBuildAccessorFactory.Create(),
                IVsEditorAdaptersFactoryServiceFactory.Create(),
                ITextDocumentFactoryServiceFactory.Create(),
                new TestShellUtilitiesHelper(),
                null,
                IProjectThreadingServiceFactory.Create(),
                IServiceProviderFactory.Create()));
        }

        [Fact]
        public void TempFileTextBufferManager_NullThreadingService_Throws()
        {
            Assert.Throws<ArgumentNullException>("threadingService", () => new TempFileTextBufferManager(
                UnconfiguredProjectFactory.Create(),
                IMsBuildAccessorFactory.Create(),
                IVsEditorAdaptersFactoryServiceFactory.Create(),
                ITextDocumentFactoryServiceFactory.Create(),
                new TestShellUtilitiesHelper(),
                IFileSystemFactory.Create(),
                null,
                IServiceProviderFactory.Create()));
        }

        [Fact]
        public void TempFileTextBufferManager_NullServiceProvider_Throws()
        {
            Assert.Throws<ArgumentNullException>("serviceProvider", () => new TempFileTextBufferManager(
                UnconfiguredProjectFactory.Create(),
                IMsBuildAccessorFactory.Create(),
                IVsEditorAdaptersFactoryServiceFactory.Create(),
                ITextDocumentFactoryServiceFactory.Create(),
                new TestShellUtilitiesHelper(),
                IFileSystemFactory.Create(),
                IProjectThreadingServiceFactory.Create(),
                null));
        }

        [Fact]
        public async Task TempFileTextBufferManager_Initialize_CreatesTempFile()
        {
            var projectFilePath = @"C:\ConsoleApp\ConsoleApp1\ConsoleApp1.csproj";
            var unconfiguredProject = UnconfiguredProjectFactory.Create(filePath: projectFilePath, projectEncoding: Encoding.Default);

            var msbuildAccessor = IMsBuildAccessorFactory.ImplementGetProjectXml("<Project />");

            var fileSystem = new IFileSystemMock();
            var tempFilePath = @"C:\Temp\asdf.1234";
            fileSystem.SetTempFile(tempFilePath);

            var textBufferManager = new TempFileTextBufferManager(unconfiguredProject,
                msbuildAccessor,
                IVsEditorAdaptersFactoryServiceFactory.Create(),
                ITextDocumentFactoryServiceFactory.Create(),
                new TestShellUtilitiesHelper(),
                fileSystem,
                new IProjectThreadingServiceMock(),
                IServiceProviderFactory.Create());

            await textBufferManager.InitializeBufferAsync();
            var tempProjectPath = Path.Combine(tempFilePath, "ConsoleApp1.csproj");
            Assert.True(fileSystem.FileExists(tempProjectPath));
            Assert.Equal("<Project />", fileSystem.ReadAllText(tempProjectPath));
        }

        public async Task TempFileTextBufferManager_NonStandardEncoding_UsesProjectEncoding()
        {
            var projectFilePath = @"C:\ConsoleApp\ConsoleApp1\ConsoleApp1.csproj";
            // Use some file encoding that's not the default
            var encoding = Encoding.Default.Equals(Encoding.UTF8) ? Encoding.UTF32 : Encoding.UTF8;
            var unconfiguredProject = UnconfiguredProjectFactory.Create(filePath: projectFilePath, projectEncoding: encoding);

            var msbuildAccessor = IMsBuildAccessorFactory.ImplementGetProjectXml("<Project />");

            var fileSystem = new IFileSystemMock();
            var tempFilePath = @"C:\Temp\asdf.1234";
            fileSystem.SetTempFile(tempFilePath);

            var textBufferManager = new TempFileTextBufferManager(unconfiguredProject,
                msbuildAccessor,
                IVsEditorAdaptersFactoryServiceFactory.Create(),
                ITextDocumentFactoryServiceFactory.Create(),
                new TestShellUtilitiesHelper(),
                fileSystem,
                new IProjectThreadingServiceMock(),
                IServiceProviderFactory.Create());

            await textBufferManager.InitializeBufferAsync();
            var tempProjectPath = Path.Combine(tempFilePath, "ConsoleApp1.csproj");
            var tempFile = fileSystem.Files.First(data => StringComparers.Paths.Equals(tempProjectPath, data.Key));
            Assert.Equal(encoding, tempFile.Value.FileEncoding);
        }

        [Fact]
        public async Task TempFileTextBufferManager_ResetBufferCleanDocData_UpdatesBufferNotFileSystem()
        {
            var projectFilePath = @"C:\ConsoleApp\ConsoleApp1\ConsoleApp1.csproj";
            // Use some file encoding that's not the default
            var encoding = Encoding.Default.Equals(Encoding.UTF8) ? Encoding.UTF32 : Encoding.UTF8;
            var unconfiguredProject = UnconfiguredProjectFactory.Create(filePath: projectFilePath, projectEncoding: encoding);

            var xml = "<Project />";

            var msbuildAccessor = IMsBuildAccessorFactory.ImplementGetProjectXml(() => xml);

            var docData = IVsPersistDocDataFactory.ImplementAsIVsTextBufferIsDocDataDirty(false, VSConstants.S_OK);
            var textBuffer = ITextBufferFactory.ImplementSnapshot("<Project />");
            var textDocument = ITextDocumentFactory.ImplementTextBuffer(textBuffer);

            var shellUtility = new TestShellUtilitiesHelper((sp, fullPath) => (null, 0, docData, 0),
                (sp, fullPath, editorType, logicalView) => null);
            var editorAdapterFactory = IVsEditorAdaptersFactoryServiceFactory.ImplementGetDocumentBuffer(textBuffer);
            var textDocumentService = ITextDocumentFactoryServiceFactory.ImplementGetTextDocument(textDocument, true);

            var fileSystem = new IFileSystemMock();
            var tempFilePath = @"C:\Temp\asdf.1234";
            fileSystem.SetTempFile(tempFilePath);

            var textBufferManager = new TempFileTextBufferManager(unconfiguredProject,
                msbuildAccessor,
                editorAdapterFactory,
                textDocumentService,
                shellUtility,
                fileSystem,
                new IProjectThreadingServiceMock(),
                IServiceProviderFactory.Create());

            await textBufferManager.InitializeBufferAsync();
            var tempProjectPath = Path.Combine(tempFilePath, "ConsoleApp1.csproj");
            var tempFile = fileSystem.Files.First(data => StringComparers.Paths.Equals(tempProjectPath, data.Key));
            Assert.Equal("<Project />", fileSystem.ReadAllText(tempProjectPath));

            xml = "<Project></Project>";
            await textBufferManager.ResetBufferAsync();
            Mock.Get(textBuffer).Verify(t => t.Replace(It.IsAny<Span>(), xml), Times.Once);
            Assert.Equal("<Project />", fileSystem.ReadAllText(tempProjectPath));
        }

        [Fact]
        public async Task TempFileTextBufferManager_ResetBufferDirtyDocData_UpdatesFileNotBuffer()
        {
            var projectFilePath = @"C:\ConsoleApp\ConsoleApp1\ConsoleApp1.csproj";
            // Use some file encoding that's not the default
            var encoding = Encoding.Default.Equals(Encoding.UTF8) ? Encoding.UTF32 : Encoding.UTF8;
            var unconfiguredProject = UnconfiguredProjectFactory.Create(filePath: projectFilePath, projectEncoding: encoding);

            var xml = "<Project />";

            var msbuildAccessor = IMsBuildAccessorFactory.ImplementGetProjectXml(() => xml);

            var docData = IVsPersistDocDataFactory.ImplementAsIVsTextBufferIsDocDataDirty(true, VSConstants.S_OK);
            var textBuffer = ITextBufferFactory.ImplementSnapshot("<Project />");
            var textDocument = ITextDocumentFactory.ImplementTextBuffer(textBuffer);

            var shellUtility = new TestShellUtilitiesHelper((sp, fullPath) => (null, 0, docData, 0),
                (sp, fullPath, editorType, logicalView) => null);
            var editorAdapterFactory = IVsEditorAdaptersFactoryServiceFactory.ImplementGetDocumentBuffer(textBuffer);
            var textDocumentService = ITextDocumentFactoryServiceFactory.ImplementGetTextDocument(textDocument, true);

            var fileSystem = new IFileSystemMock();
            var tempFilePath = @"C:\Temp\asdf.1234";
            fileSystem.SetTempFile(tempFilePath);

            var textBufferManager = new TempFileTextBufferManager(unconfiguredProject,
                msbuildAccessor,
                editorAdapterFactory,
                textDocumentService,
                shellUtility,
                fileSystem,
                new IProjectThreadingServiceMock(),
                IServiceProviderFactory.Create());

            await textBufferManager.InitializeBufferAsync();
            var tempProjectPath = Path.Combine(tempFilePath, "ConsoleApp1.csproj");
            var tempFile = fileSystem.Files.First(data => StringComparers.Paths.Equals(tempProjectPath, data.Key));
            Assert.Equal("<Project />", fileSystem.ReadAllText(tempProjectPath));

            xml = "<Project></Project>";
            await textBufferManager.ResetBufferAsync();
            Mock.Get(textBuffer).Verify(t => t.Replace(It.IsAny<Span>(), It.IsAny<string>()), Times.Never);
            Assert.Equal("<Project></Project>", fileSystem.ReadAllText(tempProjectPath));
        }

        [Fact]
        public async Task TempFileTextBufferManager_SaveAsync_SavesXmlToProject()
        {
            var projectFilePath = @"C:\ConsoleApp\ConsoleApp1\ConsoleApp1.csproj";
            // Use some file encoding that's not the default
            var encoding = Encoding.Default.Equals(Encoding.UTF8) ? Encoding.UTF32 : Encoding.UTF8;
            var unconfiguredProject = UnconfiguredProjectFactory.Create(filePath: projectFilePath, projectEncoding: encoding);

            var xml = "<Project />";

            var msbuildAccessor = IMsBuildAccessorFactory.ImplementGetProjectXml(xml);

            var docData = IVsPersistDocDataFactory.ImplementAsIVsTextBufferIsDocDataDirty(true, VSConstants.S_OK);
            var textBuffer = ITextBufferFactory.ImplementSnapshot("<Project></Project>");
            var textDocument = ITextDocumentFactory.ImplementTextBuffer(textBuffer);

            var shellUtility = new TestShellUtilitiesHelper((sp, fullPath) => (null, 0, docData, 0),
                (sp, fullPath, editorType, logicalView) => null);
            var editorAdapterFactory = IVsEditorAdaptersFactoryServiceFactory.ImplementGetDocumentBuffer(textBuffer);
            var textDocumentService = ITextDocumentFactoryServiceFactory.ImplementGetTextDocument(textDocument, true);

            var fileSystem = new IFileSystemMock();
            var tempFilePath = @"C:\Temp\asdf.1234";
            fileSystem.SetTempFile(tempFilePath);

            var textBufferManager = new TempFileTextBufferManager(unconfiguredProject,
                msbuildAccessor,
                editorAdapterFactory,
                textDocumentService,
                shellUtility,
                fileSystem,
                new IProjectThreadingServiceMock(),
                IServiceProviderFactory.Create());

            await textBufferManager.InitializeBufferAsync();
            var tempProjectPath = Path.Combine(tempFilePath, "ConsoleApp1.csproj");
            var tempFile = fileSystem.Files.First(data => StringComparers.Paths.Equals(tempProjectPath, data.Key));
            Assert.Equal("<Project />", fileSystem.ReadAllText(tempProjectPath));

            await textBufferManager.SaveAsync();
            Mock.Get(msbuildAccessor).Verify(m => m.SaveProjectXmlAsync("<Project></Project>"));
        }

        [Fact]
        public async Task TempFileTextBufferManager_SetReadonly_SetsOnlyReadonlyFlag()
        {
            var projectFilePath = @"C:\ConsoleApp\ConsoleApp1\ConsoleApp1.csproj";
            // Use some file encoding that's not the default
            var encoding = Encoding.Default.Equals(Encoding.UTF8) ? Encoding.UTF32 : Encoding.UTF8;
            var unconfiguredProject = UnconfiguredProjectFactory.Create(filePath: projectFilePath, projectEncoding: encoding);

            var xml = "<Project />";

            var msbuildAccessor = IMsBuildAccessorFactory.ImplementGetProjectXml(xml);

            var docData = IVsPersistDocDataFactory.ImplementAsIVsTextBufferGetStateFlags(~(uint)BUFFERSTATEFLAGS.BSF_USER_READONLY);
            var textBuffer = ITextBufferFactory.ImplementSnapshot("<Project></Project>");
            var textDocument = ITextDocumentFactory.ImplementTextBuffer(textBuffer);

            var shellUtility = new TestShellUtilitiesHelper((sp, fullPath) => (null, 0, docData, 0),
                (sp, fullPath, editorType, logicalView) => null);
            var editorAdapterFactory = IVsEditorAdaptersFactoryServiceFactory.ImplementGetDocumentBuffer(textBuffer);
            var textDocumentService = ITextDocumentFactoryServiceFactory.ImplementGetTextDocument(textDocument, true);

            var fileSystem = new IFileSystemMock();
            var tempFilePath = @"C:\Temp\asdf.1234";
            fileSystem.SetTempFile(tempFilePath);

            var textBufferManager = new TempFileTextBufferManager(unconfiguredProject,
                msbuildAccessor,
                editorAdapterFactory,
                textDocumentService,
                shellUtility,
                fileSystem,
                new IProjectThreadingServiceMock(),
                IServiceProviderFactory.Create());

            await textBufferManager.InitializeBufferAsync();

            await textBufferManager.SetReadOnlyAsync(true);
            Mock.Get(docData).As<IVsTextBuffer>().Verify(t =>
                t.SetStateFlags(~(uint)BUFFERSTATEFLAGS.BSF_USER_READONLY | (uint)BUFFERSTATEFLAGS.BSF_USER_READONLY));
            await textBufferManager.SetReadOnlyAsync(false);
            Mock.Get(docData).As<IVsTextBuffer>().Verify(t => t.SetStateFlags(~(uint)BUFFERSTATEFLAGS.BSF_USER_READONLY));
        }

        [Fact]
        public async Task TempFileTextBufferManager_Dispose_RemovesTempFile()
        {
            var projectFilePath = @"C:\ConsoleApp\ConsoleApp1\ConsoleApp1.csproj";
            // Use some file encoding that's not the default
            var encoding = Encoding.Default.Equals(Encoding.UTF8) ? Encoding.UTF32 : Encoding.UTF8;
            var unconfiguredProject = UnconfiguredProjectFactory.Create(filePath: projectFilePath, projectEncoding: encoding);

            var xml = "<Project />";

            var msbuildAccessor = IMsBuildAccessorFactory.ImplementGetProjectXml(xml);

            var docData = IVsPersistDocDataFactory.ImplementAsIVsTextBufferGetStateFlags(~(uint)BUFFERSTATEFLAGS.BSF_USER_READONLY);
            var textBuffer = ITextBufferFactory.ImplementSnapshot("<Project></Project>");
            var textDocument = ITextDocumentFactory.ImplementTextBuffer(textBuffer);

            var shellUtility = new TestShellUtilitiesHelper((sp, fullPath) => (null, 0, docData, 0),
                (sp, fullPath, editorType, logicalView) => null);
            var editorAdapterFactory = IVsEditorAdaptersFactoryServiceFactory.ImplementGetDocumentBuffer(textBuffer);
            var textDocumentService = ITextDocumentFactoryServiceFactory.ImplementGetTextDocument(textDocument, true);

            var fileSystem = new IFileSystemMock();
            var tempFilePath = @"C:\Temp\asdf.1234";
            fileSystem.SetTempFile(tempFilePath);

            var textBufferManager = new TempFileTextBufferManager(unconfiguredProject,
                msbuildAccessor,
                editorAdapterFactory,
                textDocumentService,
                shellUtility,
                fileSystem,
                new IProjectThreadingServiceMock(),
                IServiceProviderFactory.Create());

            await textBufferManager.InitializeBufferAsync();
            Assert.True(fileSystem.DirectoryExists(tempFilePath));
            Assert.True(fileSystem.FileExists(Path.Combine(tempFilePath, "ConsoleApp1.csproj")));

            await textBufferManager.DisposeAsync();
            Assert.False(fileSystem.DirectoryExists(tempFilePath));
        }
    }
}

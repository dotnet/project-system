// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.IO;
using Microsoft.VisualStudio.ProjectSystem.VS.UI;
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
                IProjectXmlAccessorFactory.Create(),
                IVsEditorAdaptersFactoryServiceFactory.Create(),
                ITextDocumentFactoryServiceFactory.Create(),
                IVsShellUtilitiesHelperFactory.Create(),
                IFileSystemFactory.Create(),
                IProjectThreadingServiceFactory.Create(),
                IServiceProviderFactory.Create()));
        }

        [Fact]
        public void TempFileTextBufferManager_NullMsBuildAccessor_Throws()
        {
            Assert.Throws<ArgumentNullException>("projectXmlAccessor", () => new TempFileTextBufferManager(
                UnconfiguredProjectFactory.Create(),
                null,
                IVsEditorAdaptersFactoryServiceFactory.Create(),
                ITextDocumentFactoryServiceFactory.Create(),
                IVsShellUtilitiesHelperFactory.Create(),
                IFileSystemFactory.Create(),
                IProjectThreadingServiceFactory.Create(),
                IServiceProviderFactory.Create()));
        }

        [Fact]
        public void TempFileTextBufferManager_NullEditorService_Throws()
        {
            Assert.Throws<ArgumentNullException>("editorAdaptersService", () => new TempFileTextBufferManager(
                UnconfiguredProjectFactory.Create(),
                IProjectXmlAccessorFactory.Create(),
                null,
                ITextDocumentFactoryServiceFactory.Create(),
                IVsShellUtilitiesHelperFactory.Create(),
                IFileSystemFactory.Create(),
                IProjectThreadingServiceFactory.Create(),
                IServiceProviderFactory.Create()));
        }

        [Fact]
        public void TempFileTextBufferManager_NullTextDocumentService_Throws()
        {
            Assert.Throws<ArgumentNullException>("textDocumentService", () => new TempFileTextBufferManager(
                UnconfiguredProjectFactory.Create(),
                IProjectXmlAccessorFactory.Create(),
                IVsEditorAdaptersFactoryServiceFactory.Create(),
                null,
                IVsShellUtilitiesHelperFactory.Create(),
                IFileSystemFactory.Create(),
                IProjectThreadingServiceFactory.Create(),
                IServiceProviderFactory.Create()));
        }

        [Fact]
        public void TempFileTextBufferManager_NullShellUtilities_Throws()
        {
            Assert.Throws<ArgumentNullException>("shellUtilities", () => new TempFileTextBufferManager(
                UnconfiguredProjectFactory.Create(),
                IProjectXmlAccessorFactory.Create(),
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
                IProjectXmlAccessorFactory.Create(),
                IVsEditorAdaptersFactoryServiceFactory.Create(),
                ITextDocumentFactoryServiceFactory.Create(),
                IVsShellUtilitiesHelperFactory.Create(),
                null,
                IProjectThreadingServiceFactory.Create(),
                IServiceProviderFactory.Create()));
        }

        [Fact]
        public void TempFileTextBufferManager_NullThreadingService_Throws()
        {
            Assert.Throws<ArgumentNullException>("threadingService", () => new TempFileTextBufferManager(
                UnconfiguredProjectFactory.Create(),
                IProjectXmlAccessorFactory.Create(),
                IVsEditorAdaptersFactoryServiceFactory.Create(),
                ITextDocumentFactoryServiceFactory.Create(),
                IVsShellUtilitiesHelperFactory.Create(),
                IFileSystemFactory.Create(),
                null,
                IServiceProviderFactory.Create()));
        }

        [Fact]
        public void TempFileTextBufferManager_NullServiceProvider_Throws()
        {
            Assert.Throws<ArgumentNullException>("serviceProvider", () => new TempFileTextBufferManager(
                UnconfiguredProjectFactory.Create(),
                IProjectXmlAccessorFactory.Create(),
                IVsEditorAdaptersFactoryServiceFactory.Create(),
                ITextDocumentFactoryServiceFactory.Create(),
                IVsShellUtilitiesHelperFactory.Create(),
                IFileSystemFactory.Create(),
                IProjectThreadingServiceFactory.Create(),
                null));
        }

        [Fact]
        public async Task TempFileTextBufferManager_Initialize_CreatesTempFile()
        {
            var projectFilePath = @"C:\ConsoleApp\ConsoleApp1\ConsoleApp1.csproj";
            var unconfiguredProject = UnconfiguredProjectFactory.Create(filePath: projectFilePath, projectEncoding: Encoding.Default);

            var msbuildAccessor = IProjectXmlAccessorFactory.ImplementGetProjectXml("<Project />");

            var fileSystem = new IFileSystemMock();
            var tempFilePath = @"C:\Temp\asdf.1234";
            fileSystem.SetTempFile(tempFilePath);

            var textBufferManager = new TempFileTextBufferManager(unconfiguredProject,
                msbuildAccessor,
                IVsEditorAdaptersFactoryServiceFactory.Create(),
                ITextDocumentFactoryServiceFactory.Create(),
                IVsShellUtilitiesHelperFactory.Create(),
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

            var msbuildAccessor = IProjectXmlAccessorFactory.ImplementGetProjectXml("<Project />");

            var fileSystem = new IFileSystemMock();
            var tempFilePath = @"C:\Temp\asdf.1234";
            fileSystem.SetTempFile(tempFilePath);

            var textBufferManager = new TempFileTextBufferManager(unconfiguredProject,
                msbuildAccessor,
                IVsEditorAdaptersFactoryServiceFactory.Create(),
                ITextDocumentFactoryServiceFactory.Create(),
                IVsShellUtilitiesHelperFactory.Create(),
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

            var msbuildAccessor = IProjectXmlAccessorFactory.Implement(() => xml, s => { });

            var docData = IVsPersistDocDataFactory.ImplementAsIVsTextBufferIsDocDataDirty(false, VSConstants.S_OK);
            var textBuffer = ITextBufferFactory.ImplementSnapshot("<Project />");
            var textDocument = ITextDocumentFactory.ImplementTextBuffer(textBuffer);

            var fileSystem = new IFileSystemMock();
            var tempFilePath = @"C:\Temp\asdf.1234";
            fileSystem.SetTempFile(tempFilePath);

            var shellUtility = IVsShellUtilitiesHelperFactory.ImplementGetRDTInfo(Path.Combine(tempFilePath, "ConsoleApp1.csproj"), docData);
            var editorAdapterFactory = IVsEditorAdaptersFactoryServiceFactory.ImplementGetDocumentBuffer(textBuffer);
            var textDocumentService = ITextDocumentFactoryServiceFactory.ImplementGetTextDocument(textDocument, true);

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
            Mock.Get(unconfiguredProject).Verify(u => u.SaveAsync(null), Times.Once);
        }

        [Fact]
        public async Task TempFileTextBufferManager_ResetBufferDirtyDocData_UpdatesFileNotBuffer()
        {
            var projectFilePath = @"C:\ConsoleApp\ConsoleApp1\ConsoleApp1.csproj";
            // Use some file encoding that's not the default
            var encoding = Encoding.Default.Equals(Encoding.UTF8) ? Encoding.UTF32 : Encoding.UTF8;
            var unconfiguredProject = UnconfiguredProjectFactory.Create(filePath: projectFilePath, projectEncoding: encoding);

            var xml = "<Project />";

            var msbuildAccessor = IProjectXmlAccessorFactory.Implement(() => xml, s => { });

            var docData = IVsPersistDocDataFactory.ImplementAsIVsTextBufferIsDocDataDirty(true, VSConstants.S_OK);
            var textBuffer = ITextBufferFactory.ImplementSnapshot("<Project />");
            var textDocument = ITextDocumentFactory.ImplementTextBuffer(textBuffer);

            var fileSystem = new IFileSystemMock();
            var tempFilePath = @"C:\Temp\asdf.1234";
            fileSystem.SetTempFile(tempFilePath);

            var shellUtility = IVsShellUtilitiesHelperFactory.ImplementGetRDTInfo(Path.Combine(tempFilePath, "ConsoleApp1.csproj"), docData);
            var editorAdapterFactory = IVsEditorAdaptersFactoryServiceFactory.ImplementGetDocumentBuffer(textBuffer);
            var textDocumentService = ITextDocumentFactoryServiceFactory.ImplementGetTextDocument(textDocument, true);

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
            Mock.Get(unconfiguredProject).Verify(u => u.SaveAsync(null), Times.Once);
        }

        [Fact]
        public async Task TempFileTextBufferManager_SaveAsync_SavesXmlToProject()
        {
            var projectFilePath = @"C:\ConsoleApp\ConsoleApp1\ConsoleApp1.csproj";
            // Use some file encoding that's not the default
            var encoding = Encoding.Default.Equals(Encoding.UTF8) ? Encoding.UTF32 : Encoding.UTF8;
            var unconfiguredProject = UnconfiguredProjectFactory.Create(filePath: projectFilePath, projectEncoding: encoding);

            var xml = "<Project />";

            var msbuildAccessor = IProjectXmlAccessorFactory.ImplementGetProjectXml(xml);

            var docData = IVsPersistDocDataFactory.ImplementAsIVsTextBufferIsDocDataDirty(true, VSConstants.S_OK);
            var textBuffer = ITextBufferFactory.ImplementSnapshot("<Project></Project>");
            var textDocument = ITextDocumentFactory.ImplementTextBuffer(textBuffer);

            var fileSystem = new IFileSystemMock();
            var tempFilePath = @"C:\Temp\asdf.1234";
            fileSystem.SetTempFile(tempFilePath);

            var shellUtility = IVsShellUtilitiesHelperFactory.ImplementGetRDTInfo(Path.Combine(tempFilePath, "ConsoleApp1.csproj"), docData);
            var editorAdapterFactory = IVsEditorAdaptersFactoryServiceFactory.ImplementGetDocumentBuffer(textBuffer);
            var textDocumentService = ITextDocumentFactoryServiceFactory.ImplementGetTextDocument(textDocument, true);

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
            Mock.Get(msbuildAccessor).Verify(m => m.ClearProjectDirtyFlagAsync(), Times.Once);
            Mock.Get(msbuildAccessor).Verify(m => m.SaveProjectXmlAsync("<Project></Project>"), Times.Once);
        }

        [Fact]
        public async Task TempFileTextBufferManager_SetReadonly_SetsOnlyReadonlyFlag()
        {
            var projectFilePath = @"C:\ConsoleApp\ConsoleApp1\ConsoleApp1.csproj";
            // Use some file encoding that's not the default
            var encoding = Encoding.Default.Equals(Encoding.UTF8) ? Encoding.UTF32 : Encoding.UTF8;
            var unconfiguredProject = UnconfiguredProjectFactory.Create(filePath: projectFilePath, projectEncoding: encoding);

            var xml = "<Project />";

            var msbuildAccessor = IProjectXmlAccessorFactory.ImplementGetProjectXml(xml);

            var docData = IVsPersistDocDataFactory.ImplementAsIVsTextBufferGetStateFlags(~(uint)BUFFERSTATEFLAGS.BSF_USER_READONLY);
            var textBuffer = ITextBufferFactory.ImplementSnapshot("<Project></Project>");
            var textDocument = ITextDocumentFactory.ImplementTextBuffer(textBuffer);

            var fileSystem = new IFileSystemMock();
            var tempFilePath = @"C:\Temp\asdf.1234";
            fileSystem.SetTempFile(tempFilePath);

            var shellUtility = IVsShellUtilitiesHelperFactory.ImplementGetRDTInfo(Path.Combine(tempFilePath, "ConsoleApp1.csproj"), docData);
            var editorAdapterFactory = IVsEditorAdaptersFactoryServiceFactory.ImplementGetDocumentBuffer(textBuffer);
            var textDocumentService = ITextDocumentFactoryServiceFactory.ImplementGetTextDocument(textDocument, true);

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

            var msbuildAccessor = IProjectXmlAccessorFactory.ImplementGetProjectXml(xml);

            var docData = IVsPersistDocDataFactory.ImplementAsIVsTextBufferGetStateFlags(~(uint)BUFFERSTATEFLAGS.BSF_USER_READONLY);
            var textBuffer = ITextBufferFactory.ImplementSnapshot("<Project></Project>");
            var textDocument = ITextDocumentFactory.ImplementTextBuffer(textBuffer);

            var fileSystem = new IFileSystemMock();
            var tempFilePath = @"C:\Temp\asdf.1234";
            fileSystem.SetTempFile(tempFilePath);

            var shellUtility = IVsShellUtilitiesHelperFactory.ImplementGetRDTInfo(Path.Combine(tempFilePath, "ConsoleApp1.csproj"), docData);
            var editorAdapterFactory = IVsEditorAdaptersFactoryServiceFactory.ImplementGetDocumentBuffer(textBuffer);
            var textDocumentService = ITextDocumentFactoryServiceFactory.ImplementGetTextDocument(textDocument, true);

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

        [Fact]
        public async Task TempFileTextBufferManager_NoChangesFrom_DoesNotOverwriteNewChanges()
        {
            var projectFilePath = @"C:\ConsoleApp\ConsoleApp1\ConsoleApp1.csproj";
            // Use some file encoding that's not the default
            var unconfiguredProject = UnconfiguredProjectFactory.Create(filePath: projectFilePath, projectEncoding: Encoding.UTF8);

            var projectXml = "<Project />";
            var bufferXml = "<Project></Project>";


            var docData = IVsPersistDocDataFactory.ImplementAsIVsTextBufferIsDocDataDirty(true, VSConstants.S_OK);
            var textBuffer = ITextBufferFactory.ImplementSnapshot(() => bufferXml);
            var textDocument = ITextDocumentFactory.ImplementTextBuffer(textBuffer);

            var fileSystem = new IFileSystemMock();
            var tempFilePath = @"C:\Temp\asdf.1234";
            var tempProjectPath = Path.Combine(tempFilePath, "ConsoleApp1.csproj");
            fileSystem.SetTempFile(tempFilePath);

            var msbuildAccessor = IProjectXmlAccessorFactory.Implement(() => projectXml, xml => fileSystem.WriteAllText(tempProjectPath, xml));

            var shellUtility = IVsShellUtilitiesHelperFactory.ImplementGetRDTInfo(Path.Combine(tempFilePath, "ConsoleApp1.csproj"), docData);
            var editorAdapterFactory = IVsEditorAdaptersFactoryServiceFactory.ImplementGetDocumentBuffer(textBuffer);
            var textDocumentService = ITextDocumentFactoryServiceFactory.ImplementGetTextDocument(textDocument, true);

            var textBufferManager = new TempFileTextBufferManager(unconfiguredProject,
                msbuildAccessor,
                editorAdapterFactory,
                textDocumentService,
                shellUtility,
                fileSystem,
                new IProjectThreadingServiceMock(),
                IServiceProviderFactory.Create());

            await textBufferManager.InitializeBufferAsync();
            var tempFile = fileSystem.Files.First(data => StringComparers.Paths.Equals(tempProjectPath, data.Key));

            // First save. File system should be "<Project></Project>", last saved is also "<Project></Project>"
            await textBufferManager.SaveAsync();
            Assert.Equal("<Project></Project>", fileSystem.ReadAllText(tempProjectPath));

            // Now we simulate some changes to the buffer and call Reset. Both the last saved and current project xml should be "<Project></Project>", so
            // the buffer shouldn't be reset
            projectXml = bufferXml;
            bufferXml = "<Project>asdf</Project>";
            await textBufferManager.ResetBufferAsync();
            Mock.Get(textBuffer).Verify(t => t.Replace(It.IsAny<Span>(), It.IsAny<string>()), Times.Never);
            Assert.Equal("<Project></Project>", fileSystem.ReadAllText(tempProjectPath));
        }
    }
}

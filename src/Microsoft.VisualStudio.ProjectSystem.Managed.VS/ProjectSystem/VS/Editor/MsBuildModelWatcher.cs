// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.Evaluation;
using Microsoft.VisualStudio.IO;
using Microsoft.VisualStudio.ProjectSystem.VS.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Editor
{
    [Export(typeof(IMsBuildModelWatcher))]
    internal class MsBuildModelWatcher : OnceInitializedOnceDisposedAsync, IMsBuildModelWatcher
    {
        private readonly IFileSystem _fileSystem;
        private readonly IMsBuildAccessor _accessor;
        private readonly UnconfiguredProject _unconfiguredProject;
        private string _tempFile;
        private string _lastWrittenText;

        [ImportingConstructor]
        public MsBuildModelWatcher(IProjectThreadingService threadingService,
            IFileSystem fileSystem,
            IMsBuildAccessor accessor,
            UnconfiguredProject unconfiguredProject) :
            base(threadingService.JoinableTaskContext)
        {
            _fileSystem = fileSystem;
            _accessor = accessor;
            _unconfiguredProject = unconfiguredProject;
        }

        public async Task InitializeAsync(string tempFile, string lastWrittenText)
        {
            _tempFile = tempFile;
            _lastWrittenText = Regex.Replace(lastWrittenText, @"\s", "");
            await InitializeAsync().ConfigureAwait(false);
        }

        public void ProjectXmlHandler(object sender, ProjectXmlChangedEventArgs args)
        {
            XmlHandler(args.ProjectXml.RawXml);
        }

        /// <summary>
        /// Because we can't construct an instance of ProjectxmlChangedEventArgs for testing purposes, I've extracted this functionality, and set up ProjectXmlHandler
        /// as a forwarder for the actual project xml.
        /// </summary>
        internal void XmlHandler(string xml)
        {
            // Dedup writes if the XML hasn't changed between now and the last write. We normalize the xml to remove all whitespace, and only compare the actual
            // xml content.
            var normalizedXml = Regex.Replace(xml, @"\s", "");
            if (!_lastWrittenText.Equals(normalizedXml))
            {
                _lastWrittenText = normalizedXml;
                _fileSystem.WriteAllText(_tempFile, xml);
            }
        }

        protected override async Task InitializeCoreAsync(CancellationToken cancellationToken)
        {
            // TODO: We should instead subscribe to the CPS ReleasingWriteLock events.
            // https://github.com/dotnet/roslyn-project-system/issues/738
            await _accessor.SubscribeProjectXmlChangedEventAsync(_unconfiguredProject, ProjectXmlHandler).ConfigureAwait(false);
        }

        protected override async Task DisposeCoreAsync(bool initialized)
        {
            if (initialized)
                await _accessor.UnsubscribeProjectXmlChangedEventAsync(_unconfiguredProject, ProjectXmlHandler).ConfigureAwait(false);
        }
    }
}

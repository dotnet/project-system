// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Reflection;
using Microsoft.VisualStudio.ProjectSystem.Logging;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices.Handlers
{
    /// <summary>
    ///     Handles changes to the  &lt;EditorConfigFiles/&gt; items during design-time builds.
    /// </summary>
    [Export(typeof(IWorkspaceContextHandler))]
    internal class AnalyzerConfigItemHandler : AbstractWorkspaceContextHandler, ICommandLineHandler
    {
        // WORKAROUND: To avoid Roslyn throwing when we add duplicate additional files, we remember what 
        // sent to them and avoid sending on duplicates.
        // See: https://github.com/dotnet/project-system/issues/2230

        private readonly UnconfiguredProject _project;
        private readonly HashSet<string> _paths = new HashSet<string>(StringComparers.Paths);

        private bool _triedToRetrieveAddAnalyzerConfigFileMethod = false;
        private bool _triedToRetrieveRemoveAnalyzerConfigFileMethod = false;
        private MethodInfo _addAnalyzerConfigFileMethod;
        private MethodInfo _removeAnalyzerConfigFileMethod;

        [ImportingConstructor]
        public AnalyzerConfigItemHandler(UnconfiguredProject project)
        {
            _project = project;
        }

        public void Handle(IComparable version, BuildOptions added, BuildOptions removed, bool isActiveContext, IProjectLogger logger)
        {
            Requires.NotNull(version, nameof(version));
            Requires.NotNull(added, nameof(added));
            Requires.NotNull(removed, nameof(removed));
            Requires.NotNull(logger, nameof(logger));

            VerifyInitialized();

            foreach (string analyzerConfigFile in removed.AnalyzerConfigFiles)
            {
                string fullPath = _project.MakeRooted(analyzerConfigFile);

                RemoveFromContextIfPresent(fullPath, logger);
            }

            foreach (string analyzerConfigFile in added.AnalyzerConfigFiles)
            {
                string fullPath = _project.MakeRooted(analyzerConfigFile);

                AddToContextIfNotPresent(fullPath, logger);
            }
        }

        private void AddToContextIfNotPresent(string fullPath, IProjectLogger logger)
        {
            if (!_paths.Contains(fullPath))
            {
                logger.WriteLine("Adding analyzer config file '{0}'", fullPath);
                AddToContext(fullPath);
                bool added = _paths.Add(fullPath);
                Assumes.True(added);
            }
        }

        private void RemoveFromContextIfPresent(string fullPath, IProjectLogger logger)
        {
            if (_paths.Contains(fullPath))
            {
                logger.WriteLine("Removing analyzer config file '{0}'", fullPath);
                RemoveFromContext(fullPath);
                bool removed = _paths.Remove(fullPath);
                Assumes.True(removed);
            }
        }

        private void AddToContext(string fullPath)
        {
            if (!_triedToRetrieveAddAnalyzerConfigFileMethod)
            {
                _addAnalyzerConfigFileMethod = Context.GetType().GetMethod("AddAnalyzerConfigFile");
                _triedToRetrieveAddAnalyzerConfigFileMethod = true;
            }

            if (_addAnalyzerConfigFileMethod != null)
            {
                _addAnalyzerConfigFileMethod.Invoke(Context, new[] { fullPath });
            }
        }

        private void RemoveFromContext(string fullPath)
        {
            if (!_triedToRetrieveRemoveAnalyzerConfigFileMethod)
            {
                _removeAnalyzerConfigFileMethod = Context.GetType().GetMethod("RemoveAnalyzerConfigFile");
                _triedToRetrieveRemoveAnalyzerConfigFileMethod = true;
            }

            if (_removeAnalyzerConfigFileMethod != null)
            {
                _removeAnalyzerConfigFileMethod.Invoke(Context, new[] { fullPath });
            }
        }
    }
}

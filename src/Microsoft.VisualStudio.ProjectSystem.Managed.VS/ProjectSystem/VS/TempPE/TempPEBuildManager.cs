// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.Shell.Design.Serialization;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.TempPE
{
    [Export(typeof(ITempPEBuildManager))]
    internal class TempPEBuildManager : ITempPEBuildManager, IDisposable
    {
        private readonly IUnconfiguredProjectCommonServices _unconfiguredProjectServices;
        private readonly ILanguageServiceHost _languageServiceHost;
        private readonly ITempPECompiler _compiler;
        private readonly CancellationSeries _cancellationSeries;

        [ImportingConstructor]
        public TempPEBuildManager(
            IUnconfiguredProjectCommonServices unconfiguredProjectServices,
            ILanguageServiceHost languageServiceHost,
            ITempPECompiler compiler)
        {
            _unconfiguredProjectServices = unconfiguredProjectServices;
            _languageServiceHost = languageServiceHost;
            _compiler = compiler;

            _cancellationSeries = new CancellationSeries();
        }

        /// <summary>
        /// Cancels any pending tasks and disposes this object.
        /// </summary>
        public void Dispose()
        {
            _cancellationSeries.Dispose();
        }

        public async Task<string[]> GetDesignTimeOutputFilenamesAsync(bool shared)
        {
            var propertyToCheck = shared ? Compile.DesignTimeSharedInputProperty : Compile.DesignTimeProperty;

            var project = _unconfiguredProjectServices.ActiveConfiguredProject;
            var ruleSource = project.Services.ProjectSubscription.ProjectRuleSource;
            var update = await ruleSource.GetLatestVersionAsync(project, new string[] { Compile.SchemaName });
            var snapshot = update[Compile.SchemaName];

            var fileNames = new List<string>();
            foreach (var item in snapshot.Items.Values)
            {
                bool isLink = GetBooleanPropertyValue(item, Compile.LinkProperty);
                bool designTime = GetBooleanPropertyValue(item, propertyToCheck);

                if (!isLink && designTime)
                {
                    if (item.TryGetValue(Compile.FullPathProperty, out string path))
                    {
                        fileNames.Add(path);
                    }
                }
            }

            return fileNames.ToArray();

            bool GetBooleanPropertyValue(IImmutableDictionary<string, string> item, string propName)
            {
                return item.TryGetValue(propName, out string value) && StringComparers.PropertyValues.Equals(value, "true");
            }
        }

        public async Task<string> GetTempPEBlobAsync(string fileName)
        {
            CancellationToken token = _cancellationSeries.CreateNext();

            await _unconfiguredProjectServices.ThreadingService.SwitchToUIThread(token);

            token.ThrowIfCancellationRequested();

            var files = new HashSet<string>(await GetDesignTimeOutputFilenamesAsync(true), StringComparers.Paths);
            files.Add(fileName);

            var property = await _unconfiguredProjectServices.ActiveConfiguredProjectProperties.GetConfiguredBrowseObjectPropertiesAsync();

            var objPath = await property.IntermediatePath.GetValueAsPathAsync(false, false);
            var basePath = await property.FullPath.GetValueAsPathAsync(false, false);
            var inputFileName = Path.GetFileName(fileName);
            var outputFileName = inputFileName + ".dll";
            var outputPath = Path.Combine(basePath, objPath, "TempPE");

            var result = await _compiler.CompileAsync(_languageServiceHost.ActiveProjectContext, Path.Combine(outputPath, outputFileName), files, token);

            // VSTypeResolutionService is the only consumer, and it only uses the codebase element so just default most of them
            return $@"<root>  
  <Application private_binpath = ""{outputPath}""/>  
  <Assembly  
    codebase = ""{outputFileName}""  
    name = ""{inputFileName}""  
    version = ""0.0.0.0""  
    snapshot_id = ""1""
    replaceable = ""True""  
  />  
</root>";
        }
    }
}

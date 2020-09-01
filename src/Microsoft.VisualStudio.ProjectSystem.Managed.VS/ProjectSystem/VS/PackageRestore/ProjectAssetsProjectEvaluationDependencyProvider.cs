// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.Build.Execution;
using Microsoft.VisualStudio.IO;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PackageRestore
{
    /// <summary>
    ///     Provides the project.assets.json as an input into evaluation.
    /// </summary>
    /// <remarks>
    ///     Typically inputs into evaluation are provided declaratively by the AdditionalDesignTimeBuildInput 
    ///     item, however, we want to lightup package restore in projects where we don't have control over 
    ///     the targets they input, so we imperatively provide this input.
    /// </remarks>
    [Export(typeof(IProjectEvaluationDependencyProvider))]
    [AppliesTo(ProjectCapability.PackageReferences)]
    internal class ProjectAssetsProjectEvaluationDependencyProvider : IProjectEvaluationDependencyProvider
    {
        private readonly IFileSystem _fileSystem;

        [ImportingConstructor]
        public ProjectAssetsProjectEvaluationDependencyProvider(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public IEnumerable<KeyValuePair<string, bool>> GetContentIrrelevantProjectDependentFiles(ProjectInstance projectInstance)
        {
            return Enumerable.Empty<KeyValuePair<string, bool>>();
        }

        public IEnumerable<KeyValuePair<string, DateTime?>> GetContentSensitiveProjectDependentFileTimes(ProjectInstance projectInstance)
        {
            Requires.NotNull(projectInstance, nameof(projectInstance));

            string assetsFile = projectInstance.GetPropertyValue(ConfigurationGeneral.ProjectAssetsFileProperty);

            if (assetsFile.Length == 0)
                yield break;

            _ = _fileSystem.TryGetLastFileWriteTimeUtc(assetsFile, out DateTime? lastWriteTime);

            // Equivalent of <AdditionalDesignTimeBuildInput Include = "$(ProjectAssetsFile)" ContentSensitive = "true" />
            yield return new KeyValuePair<string, DateTime?>(assetsFile, lastWriteTime);
        }
    }
}

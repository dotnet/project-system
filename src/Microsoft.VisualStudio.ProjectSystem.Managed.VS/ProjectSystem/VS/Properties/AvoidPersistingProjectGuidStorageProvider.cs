// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.VisualStudio.Build;
using Microsoft.VisualStudio.ProjectSystem.Build;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties
{
    /// <summary>
    ///     Implementation of <see cref="IProjectGuidStorageProvider"/> that avoids persisting the
    ///     project GUID property to the project file if isn't already present in the file.
    /// </summary>
    [Export(typeof(IProjectGuidStorageProvider))]
    [AppliesTo(ProjectCapability.DotNet)]
    [Order(Order.Default)]
    internal class AvoidPersistingProjectGuidStorageProvider : IProjectGuidStorageProvider
    {
        private readonly IProjectAccessor _projectAccessor;
        private readonly UnconfiguredProject _project;
        private bool? _isPersistedInProject;

        [ImportingConstructor]
        internal AvoidPersistingProjectGuidStorageProvider(IProjectAccessor projectAccessor, UnconfiguredProject project)
        {
            _projectAccessor = projectAccessor;
            _project = project;
        }

        public Task<Guid> GetProjectGuidAsync()
        {
            // We use the construction model to avoid evaluating during asynchronous project load
            return _projectAccessor.OpenProjectXmlForReadAsync(_project, projectXml =>
            {
                ProjectPropertyElement? property = FindProjectGuidProperty(projectXml);
                if (property is not null)
                {
                    _isPersistedInProject = true;

                    TryParseGuid(property, out Guid result);
                    return result;
                }
                else
                {
                    _isPersistedInProject = false;
                }

                return Guid.Empty;
            });
        }

        public Task SetProjectGuidAsync(Guid value)
        {
            // Avoid searching for the <ProjectGuid/> if we've already checked previously in GetProjectGuidAsync.
            // This handles project open, avoids us needed to take another read-lock during setting of it.
            //
            // Technically a project could add a <ProjectGuid/> latter down the track by editing the project or 
            // reloading from disk, however, both the solution, CPS and other components within Visual Studio
            // do not handle the GUID changing underneath them.
            if (_isPersistedInProject == false)
                return Task.CompletedTask;

            return _projectAccessor.OpenProjectXmlForUpgradeableReadAsync(_project, async (projectXml, cancellationToken) =>
            {
                ProjectPropertyElement property = FindProjectGuidProperty(projectXml);
                if (property is not null)
                {
                    _isPersistedInProject = true;

                    // Avoid touching the project file unless the actual GUID has changed, regardless of format
                    if (!TryParseGuid(property, out Guid result) || value != result)
                    {
                        await _projectAccessor.OpenProjectXmlForWriteAsync(_project, (root) =>
                        {
                            property.Value = ProjectCollection.Escape(value.ToString("B").ToUpperInvariant());
                        }, cancellationToken);
                    }
                }
                else
                {
                    _isPersistedInProject = false;
                }
            });
        }

        private static ProjectPropertyElement FindProjectGuidProperty(ProjectRootElement projectXml)
        {
            // NOTE: Unlike evaluation, we return the first <ProjectGuid /> to mimic legacy project system behavior
            return projectXml.PropertyGroups.SelectMany(group => group.Properties)
                                            .FirstOrDefault(p => StringComparers.PropertyNames.Equals(BuildProperty.ProjectGuid, p.Name));
        }

        private static bool TryParseGuid(ProjectPropertyElement property, out Guid result)
        {
            string unescapedValue = property.GetUnescapedValue();

            return Guid.TryParse(unescapedValue, out result);
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    [Export("LaunchProfiles", typeof(IProjectPropertiesProvider))]
    [Export(typeof(IProjectPropertiesProvider))]
    [ExportMetadata("Name", "LaunchProfiles")]
    [AppliesTo(ProjectCapability.LaunchProfiles)]
    internal class LaunchProfilesProjectPropertiesProvider : IProjectPropertiesProvider
    {
        private readonly UnconfiguredProject _project;

        [ImportingConstructor]
        public LaunchProfilesProjectPropertiesProvider(UnconfiguredProject project)
        {
            _project = project;
        }

        /// <remarks>
        /// There is a 1:1 mapping between launchSettings.json and the related project, and
        /// launch profiles can't come from anywhere else (i.e., there's no equivalent of
        /// imported .props and .targets files for launch settings). As such, launch profiles
        /// are stored in the launchSettings.json file, but the logical context is the
        /// project file.
        /// </remarks>
        public string DefaultProjectPath => _project.FullPath;

#pragma warning disable CS0067 // Unused events
        // Currently nothing consumes these, so we don't need to worry about firing them.
        // This is great because they are supposed to offer guarantees to the event
        // handlers about what locks are held--but the launch profiles don't use such
        // locks.
        // If in the future we determine that we need to fire these we will either need to
        // work through the implications on the project locks, or we will need to decide
        // if we can only fire a subset of these events.
        public event AsyncEventHandler<ProjectPropertyChangedEventArgs>? ProjectPropertyChanging;
        public event AsyncEventHandler<ProjectPropertyChangedEventArgs>? ProjectPropertyChangedOnWriter;
        public event AsyncEventHandler<ProjectPropertyChangedEventArgs>? ProjectPropertyChanged;
#pragma warning restore CS0067

        /// <remarks>
        /// <para>
        /// There are no "project-level" properties supported by the launch profiles. We
        /// return an implementation of <see cref="IProjectProperties"/> for the sake of
        /// completeness but there isn't much the consumer can do with it. We could also
        /// consider just throwing a <see cref="NotSupportedException"/> for this method,
        /// but the impact of that isn't clear.
        /// </para>
        /// <para>
        /// Note that the launch settings do support the concept of global properties, but
        /// we're choosing to expose those as if they were properties on the individual
        /// launch profiles.
        /// </para>
        /// </remarks>
        public IProjectProperties GetCommonProperties()
        {
            return new CommonProperties(_project.FullPath);
        }

        public IProjectProperties GetItemProperties(string? itemType, string? item)
        {
            throw new NotImplementedException();
        }

        // This should also return an empty IProjectProperties--there are no properties that
        // are defined _to have the same value_ for all launch profiles. There are some properties
        // that exist on each LaunchProfile, but each one will have different values.
        public IProjectProperties GetItemTypeProperties(string? itemType)
        {
            throw new NotImplementedException();
        }

        // This should just end up deferring to the other Get*Properties methods.
        public IProjectProperties GetProperties(string file, string? itemType, string? item)
        {
            if (StringComparers.Paths.Equals(file, _project.FullPath)
                && itemType is null
                && item is null)
            {
                return GetCommonProperties();
            }

            throw new NotImplementedException();
        }

        /// <summary>
        /// A no-op implementation of <see cref="IProjectProperties"/> to represent the
        /// (non-existent) project-level launch settings.
        /// </summary>
        private class CommonProperties : IProjectProperties
        {
            private static readonly Task<IEnumerable<string>> s_emptyNames = Task.FromResult(Enumerable.Empty<string>());
            
            public CommonProperties(string fileFullPath)
            {
                FileFullPath = fileFullPath;
                Context = new Context(isProjectFile: true, FileFullPath, itemType: null, itemName: null);
            }

            public IProjectPropertiesContext Context { get; }

            public string FileFullPath { get; }

            public PropertyKind PropertyKind => PropertyKind.PropertyGroup;

            /// <remarks>
            /// Throws a <see cref="NotSupportedException"/> as there are no properties we can delete.
            /// </remarks>
            public Task DeleteDirectPropertiesAsync()
            {
                throw new NotSupportedException();
            }

            /// <remarks>
            /// Throws a <see cref="NotSupportedException"/> as there are no properties we can delete.
            /// </remarks>
            public Task DeletePropertyAsync(string propertyName, IReadOnlyDictionary<string, string>? dimensionalConditions = null)
            {
                throw new NotSupportedException();
            }

            /// <remarks>
            /// There are no common (that is, project-level) properties for a launch profile.
            /// </remarks>
            public Task<IEnumerable<string>> GetDirectPropertyNamesAsync()
            {
                return s_emptyNames;
            }

            /// <remarks>
            /// Throws a <see cref="NotSupportedException"/> as there are no properties that can provide a value.
            /// </remarks>
            public Task<string> GetEvaluatedPropertyValueAsync(string propertyName)
            {
                throw new NotSupportedException();
            }

            /// <remarks>
            /// There are no common (that is, project-level) properties for a launch profile.
            /// </remarks>
            public Task<IEnumerable<string>> GetPropertyNamesAsync()
            {
                return s_emptyNames;
            }

            /// <remarks>
            /// Throws a <see cref="NotSupportedException"/> as there are no properties that can provide a value.
            /// </remarks>
            public Task<string?> GetUnevaluatedPropertyValueAsync(string propertyName)
            {
                throw new NotSupportedException();
            }

            /// <remarks>
            /// Always returns <c>false</c> as there are no project-level properties for launch profiles.
            /// </remarks> 
            public Task<bool> IsValueInheritedAsync(string propertyName)
            {
                return TaskResult.False;
            }

            /// <remarks>
            /// Throws a <see cref="NotSupportedException"/> as there are no properties for which you can provide a value;
            /// </remarks>
            public Task SetPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IReadOnlyDictionary<string, string>? dimensionalConditions = null)
            {
                throw new NotSupportedException();
            }
        }

        private class Context : IProjectPropertiesContext
        {
            public Context(bool isProjectFile, string file, string? itemType, string? itemName)
            {
                IsProjectFile = isProjectFile;
                File = file;
                ItemType = itemType;
                ItemName = itemName;
            }

            public bool IsProjectFile { get; }

            public string File { get; }

            public string? ItemType { get; }

            public string? ItemName { get; }
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel.Composition;
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
        // Launch profiles are stored in the launchSettings.json file, but the logical
        // context is the project file.
        public string DefaultProjectPath => throw new NotImplementedException();

#pragma warning disable CS0067 // Unused events
        // Currently nothing consumes these, so we don't need to worry about firing them.
        // This is great, because they are supposed to offer guarantees to the event
        // handlers about what locks are held--but the launch profiles don't use such
        // locks.
        public event AsyncEventHandler<ProjectPropertyChangedEventArgs>? ProjectPropertyChanging;
        public event AsyncEventHandler<ProjectPropertyChangedEventArgs>? ProjectPropertyChangedOnWriter;
        public event AsyncEventHandler<ProjectPropertyChangedEventArgs>? ProjectPropertyChanged;
#pragma warning restore CS0067

        // This should return an empty IProjectProperties--we're going to treat the global
        // properties in the launch settings as though they belonged to each individual
        // profile.
        public IProjectProperties GetCommonProperties()
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }
    }
}

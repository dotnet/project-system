// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    /// <summary>
    /// Provides the IEnumValue's for the ActiveDebugProfile property. This is what is used to drive
    /// the debug target selection dropdown.
    /// </summary>

    internal class DebugProfileEnumValuesGenerator : IDynamicEnumValuesGenerator
    {
        private readonly AsyncLazy<ICollection<IEnumValue>> _listedValues;

        /// <summary>
        /// Create a new instance of the class.
        /// </summary>
        internal DebugProfileEnumValuesGenerator(
            ILaunchSettingsProvider profileProvider,
            IProjectThreadingService threadingService)
        {
            Requires.NotNull(profileProvider, nameof(profileProvider));
            Requires.NotNull(threadingService, nameof(threadingService));

            this._listedValues = new AsyncLazy<ICollection<IEnumValue>>(delegate
            {
                var curSnapshot = profileProvider.CurrentSnapshot;
                if (curSnapshot != null)
                {
                    return Task.FromResult(GetEnumeratorEnumValues(curSnapshot));
                }

                ICollection<IEnumValue> emptyCollection = new List<IEnumValue>();
                return Task.FromResult(emptyCollection);
            }, threadingService.JoinableTaskFactory);
        }

        /// <summary>
        /// See <see cref="IDynamicEnumValuesGenerator"/>
        /// </summary>
        public async Task<ICollection<IEnumValue>> GetListedValuesAsync()
        {
            return await _listedValues.GetValueAsync().ConfigureAwait(true);
        }

        /// <summary>
        /// See <see cref="IDynamicEnumValuesGenerator"/>
        /// </summary>
        public bool AllowCustomValues
        {
            get { return false; }
        }

        /// <summary>
        /// See <see cref="IDynamicEnumValuesGenerator"/>
        /// </summary>
        public async Task<IEnumValue> TryCreateEnumValueAsync(string userSuppliedValue)
        {
            return (await _listedValues.GetValueAsync().ConfigureAwait(true))
            .FirstOrDefault(v => LaunchProfile.IsSameProfileName(v.Name, userSuppliedValue));
        }

        internal static ICollection<IEnumValue> GetEnumeratorEnumValues(ILaunchSettings profiles)
        {
            Collection<IEnumValue> result = new Collection<IEnumValue>(
            (
                from profile in profiles.Profiles
                let value = new EnumValue { Name = profile.Name, DisplayName = profile.Name }
                select ((IEnumValue)new PageEnumValue(value))).ToList()
            );

            return result;

        }
    }
}

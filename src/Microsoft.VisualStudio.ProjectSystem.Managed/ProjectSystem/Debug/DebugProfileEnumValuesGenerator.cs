// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.Threading;

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

            _listedValues = new AsyncLazy<ICollection<IEnumValue>>(delegate
            {
                ILaunchSettings? curSnapshot = profileProvider.CurrentSnapshot;
                if (curSnapshot is not null)
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
        public Task<ICollection<IEnumValue>> GetListedValuesAsync()
        {
            return _listedValues.GetValueAsync();
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
        public async Task<IEnumValue?> TryCreateEnumValueAsync(string userSuppliedValue)
        {
            return (await _listedValues.GetValueAsync())
            .FirstOrDefault(v => LaunchProfile.IsSameProfileName(v.Name, userSuppliedValue));
        }

        internal static ICollection<IEnumValue> GetEnumeratorEnumValues(ILaunchSettings profiles)
        {
            var result = new Collection<IEnumValue>(
            (
                from profile in profiles.Profiles
                let value = new EnumValue { Name = profile.Name, DisplayName = EscapeMnemonics(profile.Name) }
                select (IEnumValue)new PageEnumValue(value)).ToList()
            );

            return result;
        }

        [return: NotNullIfNotNull("text")]
        private static string? EscapeMnemonics(string? text)
        {
            return text?.Replace("&", "&&");
        }
    }
}

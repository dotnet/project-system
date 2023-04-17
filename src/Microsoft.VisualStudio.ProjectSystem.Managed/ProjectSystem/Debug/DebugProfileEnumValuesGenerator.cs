// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

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

        internal DebugProfileEnumValuesGenerator(
            ILaunchSettingsProvider profileProvider,
            IProjectThreadingService threadingService)
        {
            Requires.NotNull(profileProvider);
            Requires.NotNull(threadingService);

            _listedValues = new AsyncLazy<ICollection<IEnumValue>>(
                () =>
                {
                    ILaunchSettings? snapshot = profileProvider.CurrentSnapshot;

                    ICollection<IEnumValue> values = snapshot is null
                        ? Array.Empty<IEnumValue>()
                        : GetEnumeratorEnumValues(snapshot);

                    return Task.FromResult(values);
                },
                threadingService.JoinableTaskFactory);
        }

        public Task<ICollection<IEnumValue>> GetListedValuesAsync()
        {
            return _listedValues.GetValueAsync();
        }

        public bool AllowCustomValues
        {
            get { return false; }
        }

        public async Task<IEnumValue?> TryCreateEnumValueAsync(string userSuppliedValue)
        {
            ICollection<IEnumValue> enumValues = await _listedValues.GetValueAsync();

            return enumValues.FirstOrDefault(v => LaunchProfile.IsSameProfileName(v.Name, userSuppliedValue));
        }

        internal static ImmutableArray<IEnumValue> GetEnumeratorEnumValues(ILaunchSettings launchSettings)
        {
            ImmutableArray<IEnumValue>.Builder builder = ImmutableArray.CreateBuilder<IEnumValue>(initialCapacity: launchSettings.Profiles.Count);

            builder.AddRange(launchSettings.Profiles.Select(ToEnumValue));

            return builder.MoveToImmutable();

            static IEnumValue ToEnumValue(ILaunchProfile profile)
            {
                var enumValue = new EnumValue { Name = profile.Name, DisplayName = EscapeMnemonics(profile.Name) };

                return new PageEnumValue(enumValue);
            }
        }

        [return: NotNullIfNotNull(nameof(text))]
        private static string? EscapeMnemonics(string? text)
        {
            return text?.Replace("&", "&&");
        }
    }
}

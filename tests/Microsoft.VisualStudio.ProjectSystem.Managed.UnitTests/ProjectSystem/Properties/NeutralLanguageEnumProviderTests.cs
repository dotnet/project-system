// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Properties.Package;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    public class NeutralLanguageEnumProviderTests
    {
        [Fact]
        public async Task GetProviderAsync_ReturnsNonNullGenerator()
        {
            var provider = new NeutralLanguageEnumProvider();
            var generator = await provider.GetProviderAsync(options: null);

            Assert.NotNull(generator);
        }

        [Fact]
        public async Task GetListedValuesAsync_ReturnsSpecialNoneValueAsFirstItem()
        {
            var provider = new NeutralLanguageEnumProvider();
            var generator = await provider.GetProviderAsync(options: null);
            var values = await generator.GetListedValuesAsync();
            var firstValue = values.First();

            Assert.Equal(expected: NeutralLanguageValueProvider.NoneValue, actual: firstValue.Name);
        }

        [Fact]
        public async Task TryCreateEnumValueAsync_ReturnsNull()
        {
            var provider = new NeutralLanguageEnumProvider();
            var generator = await provider.GetProviderAsync(options: null);

            Assert.Null(await generator.TryCreateEnumValueAsync("abc-abc"));
        }
    }
}

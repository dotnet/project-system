// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Properties.Package;

namespace Microsoft.VisualStudio.ProjectSystem.Properties.InterceptingProjectProperties
{
    public class NeutralLanguageValueProviderTests
    {
        [Fact]
        public async Task OnSetPropertyValueAsync_ToNoneValue_DeletesTheProperty()
        {
            var provider = new NeutralLanguageValueProvider();

            var projectProperties = IProjectPropertiesFactory.CreateWithPropertyAndValue(NeutralLanguageValueProvider.NeutralLanguagePropertyName, "en-GB");
            var updatedValue = await provider.OnSetPropertyValueAsync(NeutralLanguageValueProvider.NeutralLanguagePropertyName, NeutralLanguageValueProvider.NoneValue, projectProperties);
            var valueInProjectProperties = await projectProperties.GetUnevaluatedPropertyValueAsync(NeutralLanguageValueProvider.NeutralLanguagePropertyName);

            Assert.Null(updatedValue);
            Assert.Null(valueInProjectProperties);
        }

        [Fact]
        public async Task OnSetPropertyValueAsync_ToAnythingOtherThanNone_ReturnsSameValue()
        {
            var provider = new NeutralLanguageValueProvider();

            var projectProperties = IProjectPropertiesFactory.CreateWithPropertyAndValue(NeutralLanguageValueProvider.NeutralLanguagePropertyName, "en-GB");
            var updatedValue = await provider.OnSetPropertyValueAsync(NeutralLanguageValueProvider.NeutralLanguagePropertyName, "pt-BR", projectProperties);
            var valueInProjectProperties = await projectProperties.GetUnevaluatedPropertyValueAsync(NeutralLanguageValueProvider.NeutralLanguagePropertyName);

            Assert.Equal(expected: "pt-BR", actual: updatedValue);
            Assert.Equal(expected: "en-GB", actual: valueInProjectProperties);
        }

        [Fact]
        public async Task OnGetEvaluatedPropertyValueAsync_WhenPropertyValueIsEmpty_ReturnsNoneValue()
        {
            var provider = new NeutralLanguageValueProvider();

            var projectProperties = Mock.Of<IProjectProperties>();
            var value = await provider.OnGetEvaluatedPropertyValueAsync(NeutralLanguageValueProvider.NeutralLanguagePropertyName, evaluatedPropertyValue: string.Empty, projectProperties);

            Assert.Equal(expected: NeutralLanguageValueProvider.NoneValue, actual: value);
        }

        [Fact]
        public async Task OnGetEvaluatedPropertyValueAsync_WhenPropertyValueIsNotEmpty_ReturnsSameValue()
        {
            var provider = new NeutralLanguageValueProvider();

            var projectProperties = Mock.Of<IProjectProperties>();
            var value = await provider.OnGetEvaluatedPropertyValueAsync(NeutralLanguageValueProvider.NeutralLanguagePropertyName, evaluatedPropertyValue: "en-GB", projectProperties);

            Assert.Equal(expected: "en-GB", actual: value);
        }
    }
}

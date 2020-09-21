// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class PropertiesAvailableStatusFactory
    {
        public static IUIEditorMetadataPropertiesAvailableStatus CreateUIEditorMetadataAvailableStatus(
            bool includeName = true,
            bool includeValue = true)
        {
            var mock = new Mock<IUIEditorMetadataPropertiesAvailableStatus>();

            mock.SetupGet(m => m.Name).Returns(includeName);
            mock.SetupGet(m => m.Value).Returns(includeValue);

            return mock.Object;
        }

        public static IUIEditorMetadataPropertiesAvailableStatus CreateUIEditorMetadataAvailableStatus(
            bool includeProperties)
        {
            return CreateUIEditorMetadataAvailableStatus(
                includeName: includeProperties,
                includeValue: includeProperties);
        }

        public static IUIPropertyPropertiesAvailableStatus CreateUIPropertyPropertiesAvailableStatus(
            bool includeName = true,
            bool includeDisplayName = true,
            bool includeDescription = true,
            bool includeConfigurationIndependent = true,
            bool includeHelpUrl = true,
            bool includeCategoryName = true,
            bool includeOrder = true,
            bool includeType = true,
            bool includeSearchTerms = true)
        {
            var mock = new Mock<IUIPropertyPropertiesAvailableStatus>();

            mock.SetupGet(m => m.Name).Returns(includeName);
            mock.SetupGet(m => m.DisplayName).Returns(includeDisplayName);
            mock.SetupGet(m => m.Description).Returns(includeDescription);
            mock.SetupGet(m => m.ConfigurationIndependent).Returns(includeConfigurationIndependent);
            mock.SetupGet(m => m.HelpUrl).Returns(includeHelpUrl);
            mock.SetupGet(m => m.CategoryName).Returns(includeCategoryName);
            mock.SetupGet(m => m.Order).Returns(includeOrder);
            mock.SetupGet(m => m.Type).Returns(includeType);
            mock.SetupGet(m => m.SearchTerms).Returns(includeSearchTerms);

            return mock.Object;
        }

        public static IUIPropertyPropertiesAvailableStatus CreateUIPropertyPropertiesAvailableStatus(
            bool includeProperties)
        {
            return CreateUIPropertyPropertiesAvailableStatus(
                includeName: includeProperties,
                includeDisplayName: includeProperties,
                includeDescription: includeProperties,
                includeConfigurationIndependent: includeProperties,
                includeHelpUrl: includeProperties,
                includeCategoryName: includeProperties,
                includeOrder: includeProperties,
                includeType: includeProperties,
                includeSearchTerms: includeProperties);
        }

        public static ISupportedValuePropertiesAvailableStatus CreateSupportedValuesPropertiesAvailableStatus(
            bool includeDisplayName = true,
            bool includeValue = true)
        {
            var mock = new Mock<ISupportedValuePropertiesAvailableStatus>();

            mock.SetupGet(m => m.DisplayName).Returns(includeDisplayName);
            mock.SetupGet(m => m.Value).Returns(includeValue);

            return mock.Object;
        }

        public static ISupportedValuePropertiesAvailableStatus CreateSupportedValuesPropertiesAvailableStatus(
            bool includeProperties)
        {
            return CreateSupportedValuesPropertiesAvailableStatus(
                includeDisplayName: includeProperties,
                includeValue: includeProperties);
        }

        public static ICategoryPropertiesAvailableStatus CreateCategoryPropertiesAvailableStatus(
            bool includeDisplayName = true, 
            bool includeName = true, 
            bool includeOrder = true)
        {
            var mock = new Mock<ICategoryPropertiesAvailableStatus>();

            mock.SetupGet(m => m.DisplayName).Returns(includeDisplayName);
            mock.SetupGet(m => m.Name).Returns(includeName);
            mock.SetupGet(m => m.Order).Returns(includeOrder);

            return mock.Object;
        }

        public static ICategoryPropertiesAvailableStatus CreateCategoryPropertiesAvailableStatus(
            bool includeProperties)
        {
            return CreateCategoryPropertiesAvailableStatus(
                includeDisplayName: includeProperties,
                includeName: includeProperties,
                includeOrder: includeProperties);
        }

        public static IConfigurationDimensionPropertiesAvailableStatus CreateConfigurationDimensionAvailableStatus(
            bool includeName = true,
            bool includeValue = true)
        {
            var mock = new Mock<IConfigurationDimensionPropertiesAvailableStatus>();

            mock.SetupGet(m => m.Name).Returns(includeName);
            mock.SetupGet(m => m.Value).Returns(includeValue);

            return mock.Object;
        }

        public static IConfigurationDimensionPropertiesAvailableStatus CreateConfigurationDimensionAvailableStatus(
            bool includeProperties)
        {
            return CreateConfigurationDimensionAvailableStatus(
                includeName: includeProperties,
                includeValue: includeProperties);
        }

        public static IPropertyPagePropertiesAvailableStatus CreatePropertyPagePropertiesAvailableStatus(
            bool includeName = true,
            bool includeDisplayName = true,
            bool includeOrder = true,
            bool includeKind = true)
        {
            var mock = new Mock<IPropertyPagePropertiesAvailableStatus>();

            mock.SetupGet(m => m.Name).Returns(includeName);
            mock.SetupGet(m => m.DisplayName).Returns(includeDisplayName);
            mock.SetupGet(m => m.Order).Returns(includeOrder);
            mock.SetupGet(m => m.Kind).Returns(includeKind);

            return mock.Object;
        }

        public static IPropertyPagePropertiesAvailableStatus CreatePropertyPagePropertiesAvailableStatus(
            bool includeProperties)
        {
            return CreatePropertyPagePropertiesAvailableStatus(
                includeName: includeProperties,
                includeDisplayName: includeProperties,
                includeOrder: includeProperties,
                includeKind: includeProperties);
        }

        public static IUIPropertyEditorPropertiesAvailableStatus CreateUIPropertyEditorPropertiesAvailableStatus(
            bool includeName = true)
        {
            var mock = new Mock<IUIPropertyEditorPropertiesAvailableStatus>();

            mock.SetupGet(m => m.Name).Returns(includeName);

            return mock.Object;
        }

        public static IUIPropertyValuePropertiesAvailableStatus CreateUIPropertyValuePropertiesAvailableStatus(
            bool includeEvaluatedValue = true,
            bool includeUnevaluatedValue = true)
        {
            var mock = new Mock<IUIPropertyValuePropertiesAvailableStatus>();

            mock.SetupGet(m => m.EvaluatedValue).Returns(includeEvaluatedValue);
            mock.SetupGet(m => m.UnevaluatedValue).Returns(includeUnevaluatedValue);

            return mock.Object;
        }

        public static IUIPropertyValuePropertiesAvailableStatus CreateUIPropertyValuePropertiesAvailableStatus(
            bool includeProperties)
        {
            return CreateUIPropertyValuePropertiesAvailableStatus(
                includeEvaluatedValue: includeProperties,
                includeUnevaluatedValue: includeProperties);
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Query;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class PropertiesAvailableStatusFactory
    {
        public static IUIEditorMetadataPropertiesAvailableStatus CreateUIEditorMetadataAvailableStatus(
            bool includeAllProperties = false,
            bool? includeName = null,
            bool? includeValue = null)
        {
            var mock = new Mock<IUIEditorMetadataPropertiesAvailableStatus>();

            mock.SetupGet(m => m.Name).Returns(includeName ?? includeAllProperties);
            mock.SetupGet(m => m.Value).Returns(includeValue ?? includeAllProperties);

            return mock.Object;
        }

        public static IUIPropertyPropertiesAvailableStatus CreateUIPropertyPropertiesAvailableStatus(
            bool includeAllProperties = false,
            bool? includeName = null,
            bool? includeDisplayName = null,
            bool? includeDescription = null,
            bool? includeConfigurationIndependent = null,
            bool? includeHelpUrl = null,
            bool? includeCategoryName = null,
            bool? includeIsVisible = null,
            bool? includeOrder = null,
            bool? includeType = null,
            bool? includeSearchTerms = null,
            bool? includeDependsOn = null,
            bool? includeVisibilityCondition = null)
        {
            var mock = new Mock<IUIPropertyPropertiesAvailableStatus>();

            mock.SetupGet(m => m.Name).Returns(includeName ?? includeAllProperties);
            mock.SetupGet(m => m.DisplayName).Returns(includeDisplayName ?? includeAllProperties);
            mock.SetupGet(m => m.Description).Returns(includeDescription ?? includeAllProperties);
            mock.SetupGet(m => m.ConfigurationIndependent).Returns(includeConfigurationIndependent ?? includeAllProperties);
            mock.SetupGet(m => m.HelpUrl).Returns(includeHelpUrl ?? includeAllProperties);
            mock.SetupGet(m => m.CategoryName).Returns(includeCategoryName ?? includeAllProperties);
            mock.SetupGet(m => m.IsVisible).Returns(includeIsVisible ?? includeAllProperties);
            mock.SetupGet(m => m.Order).Returns(includeOrder ?? includeAllProperties);
            mock.SetupGet(m => m.Type).Returns(includeType ?? includeAllProperties);
            mock.SetupGet(m => m.SearchTerms).Returns(includeSearchTerms ?? includeAllProperties);
            mock.SetupGet(m => m.DependsOn).Returns(includeDependsOn ?? includeAllProperties);
            mock.SetupGet(m => m.VisibilityCondition).Returns(includeVisibilityCondition ?? includeAllProperties);

            return mock.Object;
        }

        public static ISupportedValuePropertiesAvailableStatus CreateSupportedValuesPropertiesAvailableStatus(
            bool includeAllProperties = false,
            bool? includeDisplayName = null,
            bool? includeValue = null)
        {
            var mock = new Mock<ISupportedValuePropertiesAvailableStatus>();

            mock.SetupGet(m => m.DisplayName).Returns(includeDisplayName ?? includeAllProperties);
            mock.SetupGet(m => m.Value).Returns(includeValue ?? includeAllProperties);

            return mock.Object;
        }

        public static ICategoryPropertiesAvailableStatus CreateCategoryPropertiesAvailableStatus(
            bool includeAllProperties = false,
            bool? includeDisplayName = null, 
            bool? includeName = null, 
            bool? includeOrder = null)
        {
            var mock = new Mock<ICategoryPropertiesAvailableStatus>();

            mock.SetupGet(m => m.DisplayName).Returns(includeDisplayName ?? includeAllProperties);
            mock.SetupGet(m => m.Name).Returns(includeName ?? includeAllProperties);
            mock.SetupGet(m => m.Order).Returns(includeOrder ?? includeAllProperties);

            return mock.Object;
        }

        public static IConfigurationDimensionPropertiesAvailableStatus CreateConfigurationDimensionAvailableStatus(
            bool includeAllProperties = false,
            bool? includeName = null,
            bool? includeValue = null)
        {
            var mock = new Mock<IConfigurationDimensionPropertiesAvailableStatus>();

            mock.SetupGet(m => m.Name).Returns(includeName ?? includeAllProperties);
            mock.SetupGet(m => m.Value).Returns(includeValue ?? includeAllProperties);

            return mock.Object;
        }

        public static IPropertyPagePropertiesAvailableStatus CreatePropertyPagePropertiesAvailableStatus(
            bool includeAllProperties = false,
            bool? includeName = null,
            bool? includeDisplayName = null,
            bool? includeOrder = null,
            bool? includeKind = null)
        {
            var mock = new Mock<IPropertyPagePropertiesAvailableStatus>();

            mock.SetupGet(m => m.Name).Returns(includeName ?? includeAllProperties);
            mock.SetupGet(m => m.DisplayName).Returns(includeDisplayName ?? includeAllProperties);
            mock.SetupGet(m => m.Order).Returns(includeOrder ?? includeAllProperties);
            mock.SetupGet(m => m.Kind).Returns(includeKind ?? includeAllProperties);

            return mock.Object;
        }

        public static IUIPropertyEditorPropertiesAvailableStatus CreateUIPropertyEditorPropertiesAvailableStatus(
            bool includeAllProperties = false,
            bool? includeName = null,
            bool? includeMetadata = null)
        {
            var mock = new Mock<IUIPropertyEditorPropertiesAvailableStatus>();

            mock.SetupGet(m => m.Name).Returns(includeName ?? includeAllProperties);
            mock.SetupGet(m => m.Metadata).Returns(includeMetadata ?? includeAllProperties);

            return mock.Object;
        }

        public static IUIPropertyValuePropertiesAvailableStatus CreateUIPropertyValuePropertiesAvailableStatus(
            bool includeAllProperties = false,
            bool? includeEvaluatedValue = null,
            bool? includeUnevaluatedValue = null)
        {
            var mock = new Mock<IUIPropertyValuePropertiesAvailableStatus>();

            mock.SetupGet(m => m.EvaluatedValue).Returns(includeEvaluatedValue ?? includeAllProperties);
            mock.SetupGet(m => m.UnevaluatedValue).Returns(includeUnevaluatedValue ?? includeAllProperties);

            return mock.Object;
        }
    }
}

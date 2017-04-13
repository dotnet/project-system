// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties
{
    [ProjectSystemTrait]
    public class OutputTypeExValueProviderTests
    {
        public static IEnumerable<object[]> ExeEnumValue
        {
            get
            {
                yield return new object[]
                {
                    new PageEnumValue(new EnumValue { Name = "Exe", DisplayName = "1" }),
                    "1"
                };
            }
        }

        [Theory]
        [InlineData("WinExe", "0")]
        [InlineData("Exe", "1")]
        [InlineData("Library", "2")]
        [InlineData("WinMDObj", "3")]
        [InlineData("AppContainerExe", "4")]
        [InlineData("InvalidValue", null)]
        [MemberData("ExeEnumValue", "1")]
        public async void GetEvaluatedValue(object propertyValue, string expectedPropertyValue)
        {
            var properties = ProjectPropertiesFactory.Create(
                UnconfiguredProjectFactory.Create(),
                new PropertyPageData()
                {
                    Category = ConfigurationGeneral.SchemaName,
                    PropertyName = ConfigurationGeneral.OutputTypeProperty,
                    Value = propertyValue
                });
            var provider = new OutputTypeExValueProvider(properties);

            var actualPropertyValue = await provider.OnGetEvaluatedPropertyValueAsync(string.Empty, null);
            Assert.Equal(expectedPropertyValue, actualPropertyValue);
        }

        [Theory]
        [InlineData("Exe")]
        public async void SetValue(string propertyValue)
        {
            var properties = ProjectPropertiesFactory.Create(
                UnconfiguredProjectFactory.Create(),
                new PropertyPageData()
                {
                    Category = ConfigurationGeneral.SchemaName,
                    PropertyName = ConfigurationGeneral.OutputTypeProperty,
                    Value = "InitialValue"
                });
            var provider = new OutputTypeExValueProvider(properties);

            var actualPropertyValue = await provider.OnSetPropertyValueAsync(propertyValue, null);
            Assert.Equal(propertyValue, actualPropertyValue);
        }
    }
}

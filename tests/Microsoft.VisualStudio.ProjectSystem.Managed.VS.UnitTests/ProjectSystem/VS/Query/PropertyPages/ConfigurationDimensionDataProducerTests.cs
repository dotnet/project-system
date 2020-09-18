// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel.Implementation;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    public class ConfigurationDimensionDataProducerTests
    {
        [Fact]
        public void WhenPropertiesAreRequested_PropertyValuesAreReturned()
        {
            var properties = PropertiesAvailableStatusFactory.CreateConfigurationDimensionAvailableStatus(
                includeName: true,
                includeValue: true);

            var entityRuntime = IEntityRuntimeModelFactory.Create();
            var dimension = new KeyValuePair<string, string>("AlphaDimension", "AlphaDimensionValue");

            var result = (ConfigurationDimensionValue)ConfigurationDimensionDataProducer.CreateProjectConfigurationDimension(entityRuntime, dimension, properties);

            Assert.Equal(expected: "AlphaDimension", actual: result.Name);
            Assert.Equal(expected: "AlphaDimensionValue", actual: result.Value);
        }

        [Fact]
        public void WhenPropertiesAreNotRequested_PropertyValuesAreNotReturned()
        {
            var properties = PropertiesAvailableStatusFactory.CreateConfigurationDimensionAvailableStatus(
                includeName: false,
                includeValue: false);

            var entityRuntime = IEntityRuntimeModelFactory.Create();
            var dimension = new KeyValuePair<string, string>("AlphaDimension", "AlphaDimensionValue");

            var result = (ConfigurationDimensionValue)ConfigurationDimensionDataProducer.CreateProjectConfigurationDimension(entityRuntime, dimension, properties);

            Assert.Throws<MissingDataException>(() => result.Name);
            Assert.Throws<MissingDataException>(() => result.Value);
        }
    }
}

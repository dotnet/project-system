﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Properties;

public class InterceptedProjectPropertiesTests
{
    private const string MockPropertyName = "MockProperty";
    private const string Capability1 = nameof(Capability1);
    private const string Capability2 = nameof(Capability2);
    private const string Capability3 = nameof(Capability3);

    [Fact]
    public void InterceptedProjectProperties_GetValueProviderBasedOnCapability_WithNonEmptyAndEmptyAppliesTo()
    {
        var metadataCollection = new IInterceptingPropertyValueProviderMetadata2[3];
        var capabilities = new string?[] { Capability1, Capability2, null };

        for (int i = 0; i < capabilities.Length; i++)
        {
            var mockProviderMetadata = new Mock<IInterceptingPropertyValueProviderMetadata2>();
            mockProviderMetadata.Setup(x => x.PropertyNames).Returns([MockPropertyName]);
            mockProviderMetadata.Setup(x => x.AppliesTo).Returns(capabilities[i]);
            metadataCollection[i] = mockProviderMetadata.Object;
        }

        var providersWithEmptyAndNonEmptyAppliesTo =
            new Providers(
                new List<Lazy<IInterceptingPropertyValueProvider, IInterceptingPropertyValueProviderMetadata2>>
                {
                    new(() => new MockPropertyFilteredInterceptor1(), metadataCollection[0]),
                    new(() => new MockPropertyFilteredInterceptor2(), metadataCollection[1]),
                    new(() => new MockPropertyFilteredInterceptor3(), metadataCollection[2])
                });
      
        Assert.Throws<ArgumentException>(() =>
        {
            providersWithEmptyAndNonEmptyAppliesTo.GetFilteredProvider(MockPropertyName, AppliesToFunction(Capability1));
        });

        Assert.Equal(typeof(MockPropertyFilteredInterceptor3), providersWithEmptyAndNonEmptyAppliesTo.GetFilteredProvider(MockPropertyName, AppliesToFunction(Capability3))?.GetType());

        var providersWithNonEmptyAppliesTo = new Providers(
            new List<Lazy<IInterceptingPropertyValueProvider, IInterceptingPropertyValueProviderMetadata2>>
            {
                new(() => new MockPropertyFilteredInterceptor1(), metadataCollection[0]), 
                new(() => new MockPropertyFilteredInterceptor2(), metadataCollection[1])
            });
        
        Assert.Equal(typeof(MockPropertyFilteredInterceptor1), providersWithNonEmptyAppliesTo.GetFilteredProvider(MockPropertyName, AppliesToFunction(Capability1))?.GetType());
      
        // filtered tuple should return same value
        Assert.Equal(typeof(MockPropertyFilteredInterceptor1), providersWithNonEmptyAppliesTo.GetFilteredProvider(MockPropertyName, AppliesToFunction(Capability1))?.GetType());
    }

    // mock appliesto evaluation. if empty string, true, otherwise we'll just compare based on value equality.
    private static Func<string, bool> AppliesToFunction(string capability)
    {
        return expression => string.IsNullOrEmpty(expression) || expression.Equals(capability);
    }

    [ExportInterceptingPropertyValueProvider(MockPropertyName, ExportInterceptingPropertyValueProviderFile.ProjectFile)]
    [AppliesTo(Capability1)]
    private class MockPropertyFilteredInterceptor1 : InterceptingPropertyValueProviderBase
    {
    }

    [ExportInterceptingPropertyValueProvider(MockPropertyName, ExportInterceptingPropertyValueProviderFile.ProjectFile)]
    [AppliesTo(Capability2)]
    private class MockPropertyFilteredInterceptor2 : InterceptingPropertyValueProviderBase
    {
    }

    [ExportInterceptingPropertyValueProvider(MockPropertyName, ExportInterceptingPropertyValueProviderFile.ProjectFile)]
    private class MockPropertyFilteredInterceptor3 : InterceptingPropertyValueProviderBase
    {
    }
}

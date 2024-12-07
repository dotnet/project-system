// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Build.Construction;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties;

public class DefineConstantsCSharpValueProviderTests
{
    private const string PropertyName = "DefineConstants";
    
    [Theory]
    [InlineData("DEBUG;TRACE", "DEBUG=False,TRACE=False")]
    [InlineData("", "")]
    public async Task GetExistingUnevaluatedValue(string? defineConstantsValue, string expectedFormattedValue)
    {
        var provider = CreateInstance(defineConstantsValue, out _, out _);

        var actualPropertyValue = await provider.OnGetUnevaluatedPropertyValueAsync(string.Empty, string.Empty, null!);
        Assert.Equal(expectedFormattedValue, actualPropertyValue);
    }
    
    [Theory]
    [InlineData("DEBUG,TRACE", null, "DEBUG;TRACE", "DEBUG=False,TRACE=False")]
    [InlineData("$(DefineConstants),DEBUG,TRACE", "PROP1;PROP2", "$(DefineConstants);DEBUG;TRACE", "$(DefineConstants)=False,DEBUG=False,TRACE=False")]
    [InlineData("Constant0,$(DefineConstants),Constant1;;Constant2;Constant3;,Constant4", "Constant4", "Constant0;$(DefineConstants);Constant1;Constant2;Constant3", "Constant0=False,$(DefineConstants)=False,Constant1=False,Constant2=False,Constant3=False,Constant1;;Constant2;Constant3;=null")]
    public async Task SetUnevaluatedValue(string unevaluatedValueToSet, string? defineConstantsValue, string? expectedSetUnevaluatedValue, string expectedFormattedValue)
    {
        var provider = CreateInstance(null, out var projectAccessor, out var project);
        Mock<IProjectProperties> mockProjectProperties = new Mock<IProjectProperties>();
        mockProjectProperties
            .Setup(p => p.GetUnevaluatedPropertyValueAsync(ConfiguredBrowseObject.DefineConstantsProperty))
            .ReturnsAsync(defineConstantsValue);

        var setPropertyValue = await provider.OnSetPropertyValueAsync(PropertyName, unevaluatedValueToSet, mockProjectProperties.Object);
        Assert.Equal(expectedSetUnevaluatedValue, setPropertyValue);
        
        await SetDefineConstantsPropertyAsync(projectAccessor, project, setPropertyValue);
        
        var actualPropertyFormattedValue = await provider.OnGetUnevaluatedPropertyValueAsync(string.Empty, string.Empty, null!);
        Assert.Equal(expectedFormattedValue, actualPropertyFormattedValue);
    }
    
    private static DefineConstantsCSharpValueProvider CreateInstance(string? defineConstantsValue, out IProjectAccessor projectAccessor, out ConfiguredProject project)
    {
        var projectXml = defineConstantsValue is not null
            ? $"""
               <Project>
                   <PropertyGroup>
                       <{ConfiguredBrowseObject.DefineConstantsProperty}>{defineConstantsValue}</{ConfiguredBrowseObject.DefineConstantsProperty}>
                   </PropertyGroup>
               </Project>
               """
            : "<Project></Project>";
        
        projectAccessor = IProjectAccessorFactory.Create(ProjectRootElementFactory.Create(projectXml));
        project = ConfiguredProjectFactory.Create();
        
        return new DefineConstantsCSharpValueProvider(projectAccessor, project);
    }
    
    private static async Task SetDefineConstantsPropertyAsync(IProjectAccessor projectAccessor, ConfiguredProject project, string? setPropertyValue)
    {
        await projectAccessor.OpenProjectXmlForWriteAsync(project.UnconfiguredProject, projectXml =>
        {
            projectXml.AddProperty(PropertyName, setPropertyValue);
        });
    }
}

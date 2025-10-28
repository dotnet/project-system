// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Utilities;

namespace Microsoft.VisualStudio.Utilities;

public class EnvironmentServiceTests
{
    [Theory]
    [InlineData(Environment.SpecialFolder.ProgramFiles)]
    [InlineData(Environment.SpecialFolder.ApplicationData)]
    [InlineData(Environment.SpecialFolder.CommonApplicationData)]
    [InlineData(Environment.SpecialFolder.System)]
    public void GetFolderPath_ReturnsSystemValue(Environment.SpecialFolder folder)
    {
        var service = new EnvironmentService();

        string? result = service.GetFolderPath(folder);
        string expected = Environment.GetFolderPath(folder);

        Assert.Equal(string.IsNullOrEmpty(expected) ? null : expected, result);
    }

    [Fact]
    public void GetEnvironmentVariable_WhenVariableExists_ReturnsValue()
    {
        var service = new EnvironmentService();
        
        // PATH should exist on all systems
        string? result = service.GetEnvironmentVariable("PATH");
        
        Assert.NotNull(result);
        Assert.Equal(Environment.GetEnvironmentVariable("PATH"), result);
    }

    [Fact]
    public void GetEnvironmentVariable_WhenVariableDoesNotExist_ReturnsNull()
    {
        var service = new EnvironmentService();
        
        // Use a GUID to ensure the variable doesn't exist
        string nonExistentVar = $"NON_EXISTENT_VAR_{Guid.NewGuid():N}";
        
        string? result = service.GetEnvironmentVariable(nonExistentVar);
     
        Assert.Null(result);
    }

    [Fact]
    public void GetEnvironmentVariable_WithCommonSystemVariables_ReturnsExpectedValues()
    {
        var service = new EnvironmentService();
        
        // Test common system variables that should exist
        string[] variables = { "PATH", "TEMP", "TMP" };
        
        foreach (string varName in variables)
        {
            string? result = service.GetEnvironmentVariable(varName);
            string? expected = Environment.GetEnvironmentVariable(varName);
 
            Assert.Equal(expected, result);
        }
    }

    [Fact]
    public void ExpandEnvironmentVariables_WithNoVariables_ReturnsSameString()
    {
        var service = new EnvironmentService();
        string input = "C:\\Some\\Path\\Without\\Variables";
  
        string result = service.ExpandEnvironmentVariables(input);
        
        Assert.Equal(input, result);
        Assert.Same(input, result); // Should return the same instance for performance
    }

    [Fact]
    public void ExpandEnvironmentVariables_WithVariable_ExpandsCorrectly()
    {
        var service = new EnvironmentService();
        
        // Set a test environment variable
        string testVarName = $"TEST_VAR_{Guid.NewGuid():N}";
        string testVarValue = "TestValue123";
        Environment.SetEnvironmentVariable(testVarName, testVarValue);
        
        try
        {
            string input = $"Before %{testVarName}% After";
            string result = service.ExpandEnvironmentVariables(input);
     
            Assert.Equal($"Before {testVarValue} After", result);
        }
        finally
        {
            // Clean up
            Environment.SetEnvironmentVariable(testVarName, null);
        }
    }

    [Fact]
    public void ExpandEnvironmentVariables_WithMultipleVariables_ExpandsAll()
    {
        var service = new EnvironmentService();
   
        // Set test environment variables
        string testVar1 = $"TEST_VAR1_{Guid.NewGuid():N}";
        string testVar2 = $"TEST_VAR2_{Guid.NewGuid():N}";
        Environment.SetEnvironmentVariable(testVar1, "Value1");
        Environment.SetEnvironmentVariable(testVar2, "Value2");
        
        try
        {
            string input = $"%{testVar1}%\\Path\\%{testVar2}%";
            string result = service.ExpandEnvironmentVariables(input);
            
            Assert.Equal("Value1\\Path\\Value2", result);
        }
        finally
        {
            // Clean up
            Environment.SetEnvironmentVariable(testVar1, null);
            Environment.SetEnvironmentVariable(testVar2, null);
        }
    }

    [Fact]
    public void ExpandEnvironmentVariables_WithSystemVariable_ExpandsCorrectly()
    {
        var service = new EnvironmentService();
        string input = "%TEMP%\\subfolder";
        
        string result = service.ExpandEnvironmentVariables(input);
        string expected = Environment.ExpandEnvironmentVariables(input);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void ExpandEnvironmentVariables_WithNonExistentVariable_LeavesUnexpanded()
    {
        var service = new EnvironmentService();
        string nonExistentVar = $"NON_EXISTENT_{Guid.NewGuid():N}";
        string input = $"%{nonExistentVar}%";
        
        string result = service.ExpandEnvironmentVariables(input);
        string expected = Environment.ExpandEnvironmentVariables(input);
        
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ExpandEnvironmentVariables_WithEmptyString_ReturnsEmptyString()
    {
        var service = new EnvironmentService();
        string input = string.Empty;
        
        string result = service.ExpandEnvironmentVariables(input);
        
        Assert.Equal(string.Empty, result);
        Assert.Same(input, result); // Should return the same instance
    }

    [Fact]
    public void ExpandEnvironmentVariables_WithOnlyText_ReturnsOriginal()
    {
        var service = new EnvironmentService();
        string input = "No environment variables here";
     
        string result = service.ExpandEnvironmentVariables(input);
        
        Assert.Equal(input, result);
        Assert.Same(input, result); // Performance optimization check
    }
}

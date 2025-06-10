// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.Debug;

public sealed class CommandEscapingTests
{
    [Theory]
    [InlineData("", "")]
    [InlineData(" ", " ")]
    [InlineData("abcdefghijklmnop", "abcdefghijklmnop")]
    [InlineData(
        """exec "C:\temp\test.dll" """,
        """exec "C:\temp\test.dll" """)]
    [InlineData(
        """exec ^<>"C:\temp&^\test.dll"&""",
        """exec ^^^<^>"C:\temp&^\test.dll"^&""")]
    public void CommandEscaping_EscapeString(string input, string expected)
    {
        Assert.Equal(expected, CommandEscaping.EscapeString(input));
    }
}

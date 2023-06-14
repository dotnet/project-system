// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Properties;

/// <summary>
/// Used in both the "Project" and "Executable" launch profile pages.
/// </summary>
[ExportInterceptingPropertyValueProvider("CommandLineArgumentsOverriddenWarning", ExportInterceptingPropertyValueProviderFile.ProjectFile)]
internal sealed class CommandLineArgumentsOverriddenWarningValueProvider : NoOpInterceptingPropertyValueProvider
{
}

﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Properties;

[ExportInterceptingPropertyValueProvider("CreateOrOpenAssemblyResources", ExportInterceptingPropertyValueProviderFile.ProjectFile)]
internal sealed class CreateOrOpenAssemblyResourcesValueProvider : NoOpInterceptingPropertyValueProvider
{
}

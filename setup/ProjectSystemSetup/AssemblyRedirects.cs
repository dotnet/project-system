// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio;

// These provide code-base binding redirects for the assemblies we provide in this VSIX. For NGEN purposes,
// we also update src/ProductData/AssemblyVersions.tt inside VS repo, which is done during VS insertion.
[assembly: ProvideCodeBaseBindingRedirection("Microsoft.VisualStudio.ProjectSystem.Managed")]
[assembly: ProvideCodeBaseBindingRedirection("Microsoft.VisualStudio.ProjectSystem.Managed.VS")]

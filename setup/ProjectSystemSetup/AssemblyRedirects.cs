// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio;

// These provide code-base binding redirects for the assemblies we provide in this VSIX. For NGEN purposes,
// we also need to update src/appid/devenv/stub/devenv.urt.config.tt inside VS repo, which is done automatically 
// via the Roslyn Insertion tool when it consumes artifacts\Debug\DevDivInsertionFiles\DependentAssemblyVersions.csv.
[assembly: ProvideCodeBaseBindingRedirection("Microsoft.VisualStudio.ProjectSystem.Managed")]
[assembly: ProvideCodeBaseBindingRedirection("Microsoft.VisualStudio.ProjectSystem.Managed.VS")]

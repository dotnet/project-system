// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using NuGet.SolutionRestoreManager;
using NuGet.VisualStudio;

[assembly: ProjectSystemContract(ProjectSystemContractScope.Global, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne, ContractType = typeof(SVsServiceProvider))]
[assembly: ProjectSystemContract(ProjectSystemContractScope.Global, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne, ContractType = typeof(SAsyncServiceProvider))]
[assembly: ProjectSystemContract(ProjectSystemContractScope.Global, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne, ContractType = typeof(VisualStudioWorkspace))]
[assembly: ProjectSystemContract(ProjectSystemContractScope.Global, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne, ContractType = typeof(ICodeModelFactory))]
[assembly: ProjectSystemContract(ProjectSystemContractScope.Global, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne, ContractType = typeof(IVsSolutionRestoreService3))]
[assembly: ProjectSystemContract(ProjectSystemContractScope.Global, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne, ContractType = typeof(IVsFrameworkParser))]
[assembly: ProjectSystemContract(ProjectSystemContractScope.Global, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne, ContractType = typeof(IVsFrameworkCompatibility))]

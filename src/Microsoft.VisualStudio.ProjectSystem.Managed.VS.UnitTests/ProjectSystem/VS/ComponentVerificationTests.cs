// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.Composition.Reflection;
using Microsoft.VisualStudio.ProjectSystem.VS.LanguageServices;

using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    public partial class ComponentVerificationTests
    {
        /// <summary>
        /// The list of assemblies that may contain <see cref="ProjectSystemContractProvider.System"/> exports.
        /// </summary>
        private static readonly IReadOnlyList<Assembly> BuildInAssemblies = new Assembly[]
        {
            typeof(IConfiguredProjectImplicitActivationTracking).Assembly,  // Microsoft.VisualStudio.ProjectSystem.Managed
            typeof(VsContainedLanguageComponentsFactory).Assembly,          // Microsoft.VisualStudio.ProjectSystem.Managed.VS
        };

        /// <summary>
        /// The list of assemblies to scan for contracts.
        /// </summary>
        private static readonly IReadOnlyList<Assembly> ContractAssemblies = new Assembly[]
        {
            typeof(IProjectService).Assembly,                               // Microsoft.VisualStudio.ProjectSystem
            typeof(IVsProjectServices).Assembly,                            // Microsoft.VisualStudio.ProjectSystem.VS
            typeof(IConfiguredProjectImplicitActivationTracking).Assembly,  // Microsoft.VisualStudio.ProjectSystem.Managed
            typeof(VsContainedLanguageComponentsFactory).Assembly,          // Microsoft.VisualStudio.ProjectSystem.Managed.VS
        };

        /// <summary>
        /// CPS scopes in the right order.
        /// </summary>
        private static readonly string[] ScopesInOrder = new string[]
        {
            ExportContractNames.Scopes.ProjectService,
            ExportContractNames.Scopes.UnconfiguredProject,
            ExportContractNames.Scopes.ConfiguredProject,
        };

        private readonly ComposableCatalog _composableCatalog;
        private readonly CompositionConfiguration _compositionConfiguration;
        private readonly List<string> _errors = new List<string>();

        public ComponentVerificationTests()
        {
            var discovery = PartDiscovery.Combine(
                new AttributedPartDiscoveryV1(Resolver.DefaultInstance),
                new AttributedPartDiscovery(Resolver.DefaultInstance, isNonPublicSupported: true));
            var parts = discovery.CreatePartsAsync(BuildInAssemblies).GetAwaiter().GetResult();
            var scopeParts = discovery.CreatePartsAsync(typeof(UnconfiguredProjectScope), typeof(ConfiguredProjectScope), typeof(ProjectServiceScope), typeof(GlobalScope)).GetAwaiter().GetResult();

            ComposableCatalog catalog = ComposableCatalog.Create(Resolver.DefaultInstance)
                .AddParts(parts)
                .AddParts(scopeParts)
                .WithCompositionService();

            // Prepare the self-host service and composition
            _composableCatalog = catalog;
            _compositionConfiguration = CompositionConfiguration.Create(catalog);

            // TODO: Check the composition for errors.
            // We'll need the CPS implementations to be available as a package before we can 
            // verify that.
        }

        /// <summary>
        /// Verifies that a consumer should respect the capability of a component.
        /// When a consumer imports a component, it requires to check the capability of the provider,
        /// unless the consumer itself has more restrictive capability requirements than the provider.
        /// </summary>
        [Fact]
        public void VerifyAllImportsRespectsAppliesToMetadataOfExports()
        {
            foreach (ComposedPart part in _compositionConfiguration.Parts)
            {
                foreach (KeyValuePair<ImportDefinitionBinding, IReadOnlyList<ExportDefinitionBinding>> importExportBindingPair in part.SatisfyingExports)
                {
                    ImportDefinitionBinding importBinding = importExportBindingPair.Key;
                    var importingProperty = importBinding.ImportingMember as PropertyInfo;
                    if (importingProperty == null)
                    {
                        // we don't verify ImportingConstructor, only check properties.
                        continue;
                    }

                    Type memberType = importingProperty.PropertyType;

                    // ImportMany, we want to use OrderPrecedenceImportCollection
                    if (importBinding.ImportDefinition.Cardinality == ImportCardinality.ZeroOrMore)
                    {
                        if (importExportBindingPair.Value.Any(binding => !string.IsNullOrEmpty(GetAppliesToMetadata(binding.ExportDefinition))))
                        {
                            if (!IsSubclassOfGenericType(typeof(OrderPrecedenceImportCollection<,>), memberType))
                            {
                                _errors.Add($"{part.Definition.Type.FullName}.{importingProperty.Name} needs use OrderPrecedenceImportCollection to import componentss");
                            }
                        }

                        continue;
                    }

                    // Single import
                    ExportDefinitionBinding exportBinding = importExportBindingPair.Value.SingleOrDefault();
                    if (exportBinding != null)
                    {
                        string appliesTo = GetAppliesToMetadata(exportBinding.ExportDefinition);
                        if (!string.IsNullOrEmpty(appliesTo) && !ContainsExpression(appliesTo))
                        {
                            // If the consumer imports metadata, we assume it will be checked.
                            if (!IsSubclassOfGenericType(typeof(Lazy<,>), memberType))
                            {
                                // we require it to import the metadata, or the component requires the same capability, or the capability
                                // of the consumed component can be inferred from the capability of the consumer.
                                foreach (ExportDefinition exportDefinition in part.Definition.ExportDefinitions.Select(p => p.Value))
                                {
                                    string requiredAppliesTo = GetAppliesToMetadata(exportDefinition);
                                    if (requiredAppliesTo == null ||
                                        !CanCapabilityInferred(appliesTo, requiredAppliesTo) &&
                                        !ContainsExpression(requiredAppliesTo))
                                    {
                                        _errors.Add($"{part.Definition.Type.FullName}.{ importingProperty.Name} needs to check AppliesTo metadata of the imported component.");
                                    }
                                }
                            }
                        }
                    }
                }
            }

            AssertNoErrors();
        }

        /// <summary>
        /// If one component is exported through mutiple points/properties,
        /// we requires all of them using the same capability filering.
        /// </summary>
        [Fact]
        public void VerifyAllExportsOfOneComponentComingWithSameAppliesToAttribute()
        {
            foreach (ComposablePartDefinition part in _composableCatalog.Parts)
            {
                // Gather the appliesTo metadata from all exports of the same part.
                var appliesToMetadata = new List<string>();
                foreach (KeyValuePair<MemberRef, ExportDefinition> exportDefinitionPair in part.ExportDefinitions)
                {
                    ExportDefinition exportDefinition = exportDefinitionPair.Value;
                    if (exportDefinition.Metadata.TryGetValue(nameof(AppliesToAttribute.AppliesTo), out object metadata))
                    {
                        appliesToMetadata.Add((string)metadata);
                    }
                    else
                    {
                        appliesToMetadata.Add(null);
                    }
                }

                // Now check all of them should be the same.
                if (appliesToMetadata.Distinct().Count() > 1)
                {
                    _errors.Add($"{part.Type.FullName} exports multiple values with different capability requirement.");
                }
            }

            AssertNoErrors();
        }

        /// <summary>
        /// If a component should always carray AppliesTo metadata, if its consumer requires it.
        /// </summary>
        [Fact]
        public void VerifyExportsHasAppliesToMetadataWhenItIsRequired()
        {
            var knownContractsRequiringMetadata = new Dictionary<string, HashSet<Type>>();

            // First step, we scan all imports, and gather all places requiring "AppliesTo" metadata.
            foreach (ComposablePartDefinition part in _composableCatalog.Parts)
            {
                foreach (ImportDefinitionBinding import in part.ImportingMembers)
                {
                    if (IsAppliesToRequired(import))
                    {
                        if (!knownContractsRequiringMetadata.TryGetValue(import.ImportDefinition.ContractName, out HashSet<Type> contractTypes))
                        {
                            contractTypes = new HashSet<Type>();
                            knownContractsRequiringMetadata.Add(import.ImportDefinition.ContractName, contractTypes);
                        }

                        contractTypes.Add(import.ImportingSiteElementType);
                    }
                }
            }

            // Now, check all exports to see whether it matches those contracts
            foreach (ComposablePartDefinition part in _composableCatalog.Parts)
            {
                foreach (KeyValuePair<MemberRef, ExportDefinition> exportDefinitionPair in part.ExportDefinitions)
                {
                    // If the exports has already had the metadata, it is good.
                    ExportDefinition exportDefinition = exportDefinitionPair.Value;
                    if (exportDefinition.Metadata.ContainsKey(nameof(AppliesToAttribute.AppliesTo)))
                    {
                        continue;
                    }

                    // Check whether the export satisfy any contract required the appliesTo metadata.
                    // If it matches one of the contract, we will report an error, because it lacks the required metadata.
                    if (knownContractsRequiringMetadata.TryGetValue(exportDefinition.ContractName, out HashSet<Type> contractTypes))
                    {
                        MemberRef exportMember = exportDefinitionPair.Key;
                        Type exportType;
                        if (exportMember == null)
                        {
                            exportType = part.Type;
                        }
                        else
                        {
                            exportType = exportMember.DeclaringType.Resolve();
                        }

                        if (contractTypes.Any(t => t.IsAssignableFrom(exportType)))
                        {
                            _errors.Add($"{part.Type.FullName} needs to specify its capability requirement.");
                        }
                    }
                }
            }

            AssertNoErrors();
        }

        [Fact]
        public void AllComponentsMatchesContractMetadata()
        {
            Dictionary<string, ContractMetadata> contracts = CollectContractMetadata(ContractAssemblies);

            foreach (ComposablePartDefinition part in _composableCatalog.Parts)
            {
                ProjectSystemContractScope? importScope = null;
                ImportDefinition relatedImports = null;
                foreach (ImportDefinitionBinding import in part.Imports)
                {
                    ImportDefinition importDefinition = import.ImportDefinition;
                    if (importDefinition.ExportFactorySharingBoundaries.Count == 0 && contracts.TryGetValue(importDefinition.ContractName, out ContractMetadata contractMetadata))
                    {
                        if (contractMetadata.Cardinality == ImportCardinality.ZeroOrMore && importDefinition.Cardinality != ImportCardinality.ZeroOrMore)
                        {
                            _errors.Add($"Must use ImportMany in {part.Type.FullName} to import a contract {importDefinition.ContractName} which can be implemented by an extension.");
                        }

                        if (contractMetadata.Scope.HasValue &&
                            (!importScope.HasValue || importScope.Value < contractMetadata.Scope.Value))
                        {
                            importScope = contractMetadata.Scope;
                            relatedImports = importDefinition;
                        }
                    }
                }

                if (importScope.HasValue)
                {
                    foreach (KeyValuePair<MemberRef, ExportDefinition> exportPair in part.ExportDefinitions)
                    {
                        ExportDefinition exportDefinition = exportPair.Value;
                        if (contracts.TryGetValue(exportDefinition.ContractName, out ContractMetadata contractMetadata) && contractMetadata.Scope.HasValue)
                        {
                            // Do we live in a child scope (ConfiguredProject) but export to a parent scope (UnconfiguredProject)?
                            if (contractMetadata.Scope.Value < importScope.Value)
                            {
                                _errors.Add($"{part.Type.FullName} exports to the {contractMetadata.Scope.Value.ToString()} scope, but it imports {relatedImports.ContractName} from {importScope.Value.ToString()} scope, which is invalid.");
                            }
                        }
                    }
                }
            }

            AssertNoErrors();
        }

        [Fact]
        public void AllInterfaceContractsHaveMetadata()
        {
            Dictionary<string, ContractMetadata> contracts = CollectContractMetadata(ContractAssemblies.Union(BuildInAssemblies));
            var interfaceNames = new HashSet<string>();
            foreach (Assembly assembly in ContractAssemblies)
            {
                foreach (Type type in GetTypes(assembly))
                {
                    if (type.IsPublic && type.IsInterface)
                    {
                        interfaceNames.Add(type.FullName);
                    }
                }
            }

            foreach (ComposablePartDefinition part in _composableCatalog.Parts)
            {
                foreach (KeyValuePair<MemberRef, ExportDefinition> exportPair in part.ExportDefinitions)
                {
                    ExportDefinition exportDefinition = exportPair.Value;
                    if (!CheckContractHasMetadata(exportDefinition.ContractName, part, contracts, interfaceNames))
                    {
                        if (exportDefinition.ContractName.StartsWith("Microsoft.VisualStudio.ProjectSystem.", StringComparison.Ordinal))
                        {
                            _errors.Add($"{part.Type.FullName} exports a contract {exportDefinition.ContractName}, which doesn't have contract metadata.");
                        }
                    }
                }

                foreach (ImportDefinitionBinding import in part.Imports)
                {
                    ImportDefinition importDefinition = import.ImportDefinition;

                    CheckContractHasMetadata(importDefinition.ContractName, part, contracts, interfaceNames);
                }
            }

            AssertNoErrors();
        }

        private Dictionary<string, ContractMetadata> CollectContractMetadata(IEnumerable<Assembly> assemblies)
        {
            Requires.NotNull(assemblies, nameof(assemblies));
            var contracts = new Dictionary<string, ContractMetadata>(StringComparer.Ordinal);
            foreach (Assembly contractAssembly in assemblies)
            {
                ReadContractMetadata(contracts, contractAssembly);
            }

            return contracts;
        }

        private void ReadContractMetadata(Dictionary<string, ContractMetadata> contracts, Assembly contractAssembly)
        {
            Requires.NotNull(contracts, nameof(contracts));
            Requires.NotNull(contractAssembly, nameof(contractAssembly));
            foreach (ProjectSystemContractAttribute assemblyAttribute in contractAssembly.GetCustomAttributes<ProjectSystemContractAttribute>())
            {
                if (!string.IsNullOrEmpty(assemblyAttribute.ContractName))
                {
                    AddContractMetadata(contracts, assemblyAttribute.ContractName, assemblyAttribute.Scope, assemblyAttribute.Provider, assemblyAttribute.Cardinality);
                }
            }

            foreach (Type definedType in GetTypes(contractAssembly))
            {
                if (definedType.IsInterface || definedType.IsClass)
                {
                    foreach (ProjectSystemContractAttribute attribute in definedType.GetCustomAttributes<ProjectSystemContractAttribute>())
                    {
                        string name = attribute.ContractName ?? definedType.FullName;
                        AddContractMetadata(contracts, name, attribute.Scope, attribute.Provider, attribute.Cardinality);
                    }
                }
            }
        }

        private Type[] GetTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                var exceptions = ex.LoaderExceptions.Where(e => !IsIgnorable(e))
                                                    .ToArray();

                if (exceptions.Length == 0)
                    return ex.Types.Where(t => t != null).ToArray();

                string message = ex.ToString();

                message += "\nLoaderExceptions:\n";

                for (int i = 0; i < ex.LoaderExceptions.Length; i++)
                {
                    message += ex.LoaderExceptions[i].ToString();
                    message += "\n";
                }

                Assert.False(true, message);
            }

            return null;
        }

        private bool IsIgnorable(Exception exception)
        {
            if (exception is FileNotFoundException fileNotFound)
            {
                return fileNotFound.FileName == "Microsoft.VisualStudio.ProjectServices, Version=15.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            }

            return false;
        }

        private void AddContractMetadata(Dictionary<string, ContractMetadata> contracts, string name, ProjectSystemContractScope scope, ProjectSystemContractProvider provider, ImportCardinality cardinality)
        {
            Requires.NotNull(contracts, nameof(contracts));
            Requires.NotNullOrEmpty(name, nameof(name));

            if (!contracts.TryGetValue(name, out ContractMetadata metadata))
            {
                metadata = new ContractMetadata
                {
                    Provider = provider,
                    Scope = scope,
                    Cardinality = cardinality
                };

                contracts.Add(name, metadata);
            }
            else
            {
                // We don't support using the contract name with different interfaces, so we don't verify those contracts.
                if (metadata.Scope != scope)
                {
                    metadata.Scope = null;
                }

                if (metadata.Provider != provider)
                {
                    metadata.Provider = null;
                }

                if (metadata.Cardinality != cardinality)
                {
                    metadata.Cardinality = null;
                }
            }
        }

        private bool CheckContractHasMetadata(string contractName, ComposablePartDefinition part, Dictionary<string, ContractMetadata> contractMetadata, HashSet<string> interfaceNames)
        {
            Requires.NotNull(contractName, nameof(contractName));
            if (contractMetadata.ContainsKey(contractName) || contractName == part.Type.FullName || contractName.Contains("{"))
            {
                return true;
            }

            if (interfaceNames.Contains(contractName))
            {
                _errors.Add($"{part.Type.FullName} exports/imports a contract {contractName}, which doesn't have contract metadata.");
            }

            return false;
        }

        private void AssertNoErrors()
        {
            string message = $"There were {_errors.Count} errors." + Environment.NewLine;

            Assert.True(_errors.Count == 0, message + string.Join(Environment.NewLine, _errors));
        }

        /// <summary>
        /// Check whether the import requiring a component to have "AppliesTo" metadata.
        /// If the imports ask metadata from the exports, and the metadata based on IAppliesToMetadataView,
        /// the "AppliesTo" metadata is required.
        /// </summary>
        private static bool IsAppliesToRequired(ImportDefinitionBinding import)
        {
            Type metadataType = import.MetadataType;
            Type appliesToView = typeof(IAppliesToMetadataView);
            return metadataType != null && appliesToView.IsAssignableFrom(appliesToView);
        }

        /// <summary>
        /// Check whether the sharing scope of the component is valid.
        /// An unconfigured project component cannot have configured project in its sharing boundaries list. 
        /// A project service component cannot have both configured and unconfigured project in its list.
        /// </summary>
        private static bool IsValidSharingScope(string sharingScope, string requiredScope)
        {
            return GetScopeIndex(sharingScope) <= GetScopeIndex(requiredScope);
        }

        private static int GetScopeIndex(string scope)
        {
            int index = 0;
            foreach (string knownScope in ScopesInOrder)
            {
                if (knownScope.Equals(scope, StringComparison.OrdinalIgnoreCase))
                {
                    return index;
                }

                index++;
            }

            return -1;
        }

        private static string GetPartScope(ComposedPart part)
        {
            return part.RequiredSharingBoundaries.OrderByDescending(s => GetScopeIndex(s)).FirstOrDefault();
        }

        /// <summary>
        /// A set of rules we think one capability can infer another one.  Some are just workaround, so we can fix some issues later.
        /// </summary>
        private static readonly KeyValuePair<string, string>[] knownCapabilityInferringRules = new KeyValuePair<string, string>[]
        {
            //new KeyValuePair<string, string>(ProjectCapabilities.SharedAssetsProject, ProjectCapabilities.Cps),
            //new KeyValuePair<string, string>(ProjectCapabilities.NestedProjects, ProjectCapabilities.Cps),
            //new KeyValuePair<string, string>(ProjectCapabilities.OutputGroups, ProjectCapabilities.Cps),
            //new KeyValuePair<string, string>(ProjectCapability.UseFileGlobs, ProjectCapabilities.Cps),
        };


        /// <summary>
        /// Check whether a capability can be inferred from another one.
        /// For example, share project is a CPS project, so a component using inside a 
        /// share project can depend on a component requiring CPS project to exist. 
        /// </summary>
        private static bool CanCapabilityInferred(string inferredCapability, string currentCapability)
        {
            if (string.IsNullOrEmpty(currentCapability))
            {
                return false;
            }

            if (string.Equals(inferredCapability, currentCapability, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            foreach (KeyValuePair<string, string> knownInferringRule in knownCapabilityInferringRules)
            {
                if (string.Equals(currentCapability, knownInferringRule.Key, StringComparison.OrdinalIgnoreCase))
                {
                    if (string.Equals(inferredCapability, knownInferringRule.Value, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Check whether a capability is not a simple string, but a complex expression.
        /// We don't have built-in logic to check whether one expression can infer another one today, so we don't do validation when an expression is being used.
        /// </summary>
        private static bool ContainsExpression(string capability)
        {
            return capability != null && capability.IndexOfAny(new char[] { '&', '|', '!' }) >= 0;
        }

        /// <summary>
        /// Check whether a type is a subclass of a generic type.
        /// </summary>
        private static bool IsSubclassOfGenericType(Type genericType, Type type)
        {
            while (type != null && type != typeof(object))
            {
                Type currentType = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
                if (genericType == currentType)
                {
                    return true;
                }

                type = type.BaseType;
            }

            return false;
        }

        /// <summary>
        /// Get AppliesTo metadata from an export.
        /// </summary>
        /// <returns>returns null if the metadata cannot be found.</returns>
        private static string GetAppliesToMetadata(ExportDefinition exportDefinition)
        {
            if (exportDefinition.Metadata.TryGetValue(nameof(AppliesToAttribute.AppliesTo), out object appliesToMetadata))
            {
                return (string)appliesToMetadata;
            }

            return null;
        }

        private class ContractMetadata
        {
            public ProjectSystemContractProvider? Provider { get; set; }

            public ProjectSystemContractScope? Scope { get; set; }

            public ImportCardinality? Cardinality { get; set; }
        }
    }
}

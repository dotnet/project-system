// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.Composition.Reflection;

using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    public partial class ComponentVerificationTests
    {
        private readonly List<string> _errors = new List<string>();

        [Theory]
        [ClassData(typeof(SatisfyingExportsTestData))]
        public void ImportsMustFilterBasedOnCapabilities(ComposedPart part, KeyValuePair<ImportDefinitionBinding, IReadOnlyList<ExportDefinitionBinding>> binding)
        {   // Imports should respect/filter the capabilities of the exports they receive

            ImportDefinitionBinding importBinding = binding.Key;
            var importingProperty = importBinding.ImportingMember as PropertyInfo;
            if (importingProperty == null)  // We don't verify ImportingConstructor, only check properties.
                return;

            Type memberType = importingProperty.PropertyType;

            // ImportMany, we want to use OrderPrecedenceImportCollection
            if (importBinding.ImportDefinition.Cardinality == ImportCardinality.ZeroOrMore)
            {
                if (binding.Value.Any(b => !string.IsNullOrEmpty(GetAppliesToMetadata(b.ExportDefinition))))
                {
                    if (!IsSubclassOfGenericType(typeof(OrderPrecedenceImportCollection<,>), memberType))
                    {
                        Assert.False(true, $"{part.Definition.Type.FullName}.{importingProperty.Name} needs to use OrderPrecedenceImportCollection to import components.");
                    }
                }

                return;
            }

            // Single import
            ExportDefinitionBinding exportBinding = binding.Value.SingleOrDefault();
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
                                !ContainsExpression(requiredAppliesTo))
                            {
                                Assert.False(true, $"{part.Definition.Type.FullName}.{ importingProperty.Name} needs to check AppliesTo metadata of the imported component.");
                            }
                        }
                    }
                }
            }
        }

        [Theory]
        [ClassData(typeof(ComposablePartDefinitionTestData))]
        public void ExportsFromSamePartMustApplyToSameCapabilities(ComposablePartDefinition definition)
        {   // Exports coming from a single part must apply to the same capabilities

            // Gather the appliesTo metadata from all exports of the same part.
            var appliesToMetadata = new List<string>();
            foreach (KeyValuePair<MemberRef, ExportDefinition> exportDefinitionPair in definition.ExportDefinitions)
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
                Assert.False(true, $"{definition.Type.FullName} exports multiple values with differing AppliesTo. All exports from a component must apply to the same capabilities.");
            }
        }

        [Theory]
        [ClassData(typeof(ExportsTestData))]
        public void ExportsMustBeMarkedWithApplyToIfRequired(ComposablePartDefinition definition, KeyValuePair<MemberRef, ExportDefinition> export)
        {   // If a contract requires AppliesTo to be specified, then an export must specify it

            var contractsRequiringMetadata = ComponentComposition.Instance.ContractsRequiringMetadata;

            // If the exports has already had the metadata, it is good.
            ExportDefinition exportDefinition = export.Value;
            if (exportDefinition.Metadata.ContainsKey(nameof(AppliesToAttribute.AppliesTo)))
            {
                return;
            }

            // Check whether the export satisfy any contract required the appliesTo metadata.
            // If it matches one of the contract, we will report an error, because it lacks the required metadata.
            if (contractsRequiringMetadata.TryGetValue(exportDefinition.ContractName, out ISet<Type> contractTypes))
            {
                MemberRef exportMember = export.Key;
                Type exportType;
                if (exportMember == null)
                {
                    exportType = definition.Type;
                }
                else
                {
                    exportType = exportMember.DeclaringType.Resolve();
                }

                if (contractTypes.Any(t => t.IsAssignableFrom(exportType)))
                {
                    Assert.False(true, $"{definition.Type.FullName} must specify [AppliesTo] to its export of {exportType}.");
                }
            }
        }

        [Theory]
        [ClassData(typeof(ImportsTestData))]
        public void ImportsMustRespectContractImportCardinality(ComposablePartDefinition definition, ImportDefinitionBinding import)
        {   // Imports must respect import cardinality specified via [ProjectSystemContract]

            var contracts = ComponentComposition.Instance.Contracts;

            ImportDefinition importDefinition = import.ImportDefinition;
            if (contracts.TryGetValue(importDefinition.ContractName, out ComponentComposition.ContractMetadata contractMetadata))
            {
                if (contractMetadata.Cardinality == ImportCardinality.ZeroOrMore && importDefinition.Cardinality != ImportCardinality.ZeroOrMore)
                {
                    Assert.False(true, $"Must use [ImportMany] in {definition.Type.FullName} to import a contract {importDefinition.ContractName} which can be implemented by an extension.");
                }
            }
        }

        [Theory]
        [ClassData(typeof(ExportsTestData))]
        public void ExportsMustMatchImportScope(ComposablePartDefinition definition, KeyValuePair<MemberRef, ExportDefinition> export)
        {   // Exports cannot be exported into a parent scope (indicated by contract's [ProjectSystemContract]), when part imports something from a child scope

            var contracts = ComponentComposition.Instance.Contracts;

            ProjectSystemContractScope? importScope = null;
            ImportDefinition relatedImports = null;
            foreach (ImportDefinitionBinding import in definition.Imports)
            {
                ImportDefinition importDefinition = import.ImportDefinition;
                if (importDefinition.ExportFactorySharingBoundaries.Count == 0 && contracts.TryGetValue(importDefinition.ContractName, out ComponentComposition.ContractMetadata contractMetadata))
                {
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
                ExportDefinition exportDefinition = export.Value;
                if (contracts.TryGetValue(exportDefinition.ContractName, out ComponentComposition.ContractMetadata contractMetadata) && contractMetadata.Scope.HasValue)
                {
                    // Do we live in a child scope (ConfiguredProject) but export to a parent scope (UnconfiguredProject)?
                    if (contractMetadata.Scope.Value < importScope.Value)
                    {
                        Assert.False(true, $"{definition.Type.FullName} exports to the {contractMetadata.Scope.Value.ToString()} scope, but it imports {relatedImports.ContractName} from {importScope.Value.ToString()} scope, which is invalid.");
                    }
                }
            }
        }

        [Theory]
        [ClassData(typeof(ImportsTestData))]
        public void ImportsMustImportContractsMarkedWithProjectSystemContract(ComposablePartDefinition definition, ImportDefinitionBinding import)
        {  // Imports must importsinterfaces that are marked with [ProjectSystemContract]

            ImportDefinition importDefinition = import.ImportDefinition;
            if (!CheckContractHasMetadata(importDefinition.ContractName, definition, ComponentComposition.Instance.Contracts, ComponentComposition.Instance.InterfaceNames))
            {
                Assert.False(true, $"{definition.Type.FullName} imports a contract {importDefinition.ContractName}, which is not applied with [ProjectSystemContract]");
            }
        }

        [Theory]
        [ClassData(typeof(ExportsTestData))]
        public void ExportsMustExportContractsMarkedWithProjectSystemContract(ComposablePartDefinition definition, KeyValuePair<MemberRef, ExportDefinition> export)
        {   // Exports must export interfaces that are marked with [ProjectSystemContract]

            ExportDefinition exportDefinition = export.Value;
            if (!CheckContractHasMetadata(exportDefinition.ContractName, definition, ComponentComposition.Instance.Contracts, ComponentComposition.Instance.InterfaceNames))
            {
                Assert.False(true, $"{definition.Type.FullName} exports a contract {exportDefinition.ContractName}, which is not applied with [ProjectSystemContract]");
            }

        }

        private bool CheckContractHasMetadata(string contractName, ComposablePartDefinition part, IDictionary<string, ComponentComposition.ContractMetadata> contractMetadata, ISet<string> interfaceNames)
        {
            Requires.NotNull(contractName, nameof(contractName));
            if (contractMetadata.ContainsKey(contractName) || contractName == part.Type.FullName || contractName.Contains("{"))
            {
                return true;
            }

            return interfaceNames.Contains(contractName);
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
    }
}

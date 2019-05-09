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
        [Theory]
        [ClassData(typeof(ComposedPartTestData))]
        public void ImportsMustFilterBasedOnCapabilities(Type type)
        {   
            // Imports should respect/filter the capabilities of the exports they receive

            var part = ComponentComposition.Instance.FindComposedPart(type);

            foreach ((ImportDefinitionBinding import, IReadOnlyList<ExportDefinitionBinding> exports) in part.SatisfyingExports)
            {
                var importingProperty = import.ImportingMember as PropertyInfo;
                if (importingProperty == null)  // We don't verify ImportingConstructor, only check properties.
                    return;

                Type memberType = importingProperty.PropertyType;

                // ImportMany, we want to use OrderPrecedenceImportCollection
                if (import.ImportDefinition.Cardinality == ImportCardinality.ZeroOrMore)
                {
                    if (exports.Any(b => !string.IsNullOrEmpty(GetAppliesToMetadata(b.ExportDefinition))))
                    {
                        if (!IsSubclassOfGenericType(typeof(OrderPrecedenceImportCollection<,>), memberType))
                        {
                            Assert.False(true, $"{part.Definition.Type.FullName}.{importingProperty.Name} needs to use OrderPrecedenceImportCollection to import components.");
                        }
                    }

                    return;
                }

                // Single import
                ExportDefinitionBinding exportBinding = exports.SingleOrDefault();
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
        }

        [Theory]
        [ClassData(typeof(ComposablePartDefinitionTestData))]
        public void ExportsFromSamePartMustApplyToSameCapabilities(Type type)
        {   
            // Exports coming from a single part must apply to the same capabilities

            var definition = ComponentComposition.Instance.FindComposablePartDefinition(type);

            // Gather the appliesTo metadata from all exports of the same part.
            var appliesToMetadata = new List<string>();
            foreach (KeyValuePair<MemberRef, ExportDefinition> exportDefinitionPair in definition.ExportDefinitions)
            {
                ExportDefinition exportDefinition = exportDefinitionPair.Value;
                exportDefinition.Metadata.TryGetValue(nameof(AppliesToAttribute.AppliesTo), out object metadata);
                appliesToMetadata.Add((string)metadata);
            }

            // Now check all of them should be the same.
            if (appliesToMetadata.Distinct().Count() > 1)
            {
                Assert.False(true, $"{definition.Type.FullName} exports multiple values with differing AppliesTo. All exports from a component must apply to the same capabilities.");
            }
        }

        [Theory]
        [ClassData(typeof(ComposablePartDefinitionTestData))]
        public void ExportsMustBeMarkedWithApplyToIfRequired(Type type)
        {   
            // If a contract requires AppliesTo to be specified, then an export must specify it

            var definition = ComponentComposition.Instance.FindComposablePartDefinition(type);

            foreach (KeyValuePair<MemberRef, ExportDefinition> export in definition.ExportDefinitions)
            {
                var contractsRequiringMetadata = ComponentComposition.Instance.ContractsRequiringAppliesTo;

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
        }

        [Theory]
        [ClassData(typeof(ComposablePartDefinitionTestData))]
        public void ImportsMustRespectContractImportCardinality(Type type)
        {   
            // Imports must respect import cardinality specified via [ProjectSystemContract]

            var contracts = ComponentComposition.Instance.Contracts;
            var definition = ComponentComposition.Instance.FindComposablePartDefinition(type);

            foreach (ImportDefinitionBinding import in definition.Imports)
            {
                ImportDefinition importDefinition = import.ImportDefinition;
                if (contracts.TryGetValue(importDefinition.ContractName, out ComponentComposition.ContractMetadata contractMetadata))
                {
                    if (contractMetadata.Cardinality == ImportCardinality.ZeroOrMore && importDefinition.Cardinality != ImportCardinality.ZeroOrMore)
                    {
                        Assert.False(true, $"Must use [ImportMany] in {definition.Type.FullName} to import a contract {importDefinition.ContractName} which can be implemented by an extension.");
                    }
                }
            }
        }

        [Theory]
        [ClassData(typeof(ComposablePartDefinitionTestData))]
        public void ImportsMustMatchExportScope(Type type)
        {   
            // Imports cannot import something from a child scope, if the part comes from parent scope

            var definition = ComponentComposition.Instance.FindComposablePartDefinition(type);

            foreach (ImportDefinitionBinding import in definition.Imports)
            {
                ImportDefinition importDefinition = import.ImportDefinition;
                if (importDefinition.ExportFactorySharingBoundaries.Count > 0)
                    return; // You can import something from child if its the start of a scope

                var contracts = ComponentComposition.Instance.Contracts;

                if (contracts.TryGetValue(importDefinition.ContractName, out ComponentComposition.ContractMetadata importContractMetadata) && importContractMetadata.Scope != null)
                {
                    foreach (KeyValuePair<MemberRef, ExportDefinition> export in definition.ExportDefinitions)
                    {
                        if (contracts.TryGetValue(export.Value.ContractName, out ComponentComposition.ContractMetadata exportContractMetadata) && exportContractMetadata.Scope != null)
                        {
                            // Do we import from a child scope but export to a parent scope? ie Importing ConfiguredProject, but exporting to an UnconfiguredProject service would be invalid
                            if (exportContractMetadata.Scope < importContractMetadata.Scope)
                            {
                                Assert.False(true, $"{definition.Type.FullName} exports to the {exportContractMetadata.Scope.Value} scope, but it imports {importDefinition.ContractName} from {importContractMetadata.Scope} scope, which is a child of the preceeding scope.");
                            }
                        }
                    }
                }
            }
        }

        [Theory]
        [ClassData(typeof(ComposablePartDefinitionTestData))]
        public void ImportsMustImportContractsMarkedWithProjectSystemContract(Type type)
        {  
            // Imports must import interfaces that are marked with [ProjectSystemContract]

            var definition = ComponentComposition.Instance.FindComposablePartDefinition(type);

            foreach (ImportDefinitionBinding import in definition.Imports)
            {
                ImportDefinition importDefinition = import.ImportDefinition;
                if (!CheckContractHasMetadata(GetContractName(importDefinition), definition, ComponentComposition.Instance.Contracts, ComponentComposition.Instance.InterfaceNames))
                {
                    Assert.False(true, $"{definition.Type.FullName} imports a contract {importDefinition.ContractName}, which is not applied with [ProjectSystemContract]");
                }
            }
        }

        [Theory]
        [ClassData(typeof(ComposablePartDefinitionTestData))]
        public void ExportsMustExportContractsMarkedWithProjectSystemContract(Type type)
        {   
            // Exports must export interfaces that are marked with [ProjectSystemContract]

            var definition = ComponentComposition.Instance.FindComposablePartDefinition(type);

            foreach (KeyValuePair<MemberRef, ExportDefinition> export in definition.ExportDefinitions)
            {
                ExportDefinition exportDefinition = export.Value;
                if (!CheckContractHasMetadata(exportDefinition.ContractName, definition, ComponentComposition.Instance.Contracts, ComponentComposition.Instance.InterfaceNames))
                {
                    Assert.False(true, $"{definition.Type.FullName} exports a contract {exportDefinition.ContractName}, which is not applied with [ProjectSystemContract]");
                }
            }
        }

        private bool CheckContractHasMetadata(string contractName, ComposablePartDefinition part, IDictionary<string, ComponentComposition.ContractMetadata> contractMetadata, ISet<string> interfaceNames)
        {
            Requires.NotNull(contractName, nameof(contractName));
            if (contractMetadata.ContainsKey(contractName) || contractName == part.Type.FullName || contractName.Contains("{"))
            {
                return true;
            }

            return !interfaceNames.Contains(contractName);
        }

        private string GetContractName(ImportDefinition import)
        {
            if (import.Metadata.TryGetValue("System.ComponentModel.Composition.GenericContractName", out var value))
            {
                return (string)value;
            }

            return import.ContractName;
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

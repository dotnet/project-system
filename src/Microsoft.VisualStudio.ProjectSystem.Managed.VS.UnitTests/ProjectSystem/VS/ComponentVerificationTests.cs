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

        /// <summary>
        /// Verifies that a consumer should respect the capability of a component.
        /// When a consumer imports a component, it requires to check the capability of the provider,
        /// unless the consumer itself has more restrictive capability requirements than the provider.
        /// </summary>
        [Theory]
        [ClassData(typeof(ComposedPartTestData))]
        public void VerifyAllImportsRespectsAppliesToMetadataOfExports(ComposedPart part)
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
                                    !ContainsExpression(requiredAppliesTo))
                                {
                                    _errors.Add($"{part.Definition.Type.FullName}.{ importingProperty.Name} needs to check AppliesTo metadata of the imported component.");
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
        [Theory]
        [ClassData(typeof(ComposablePartDefinitionTestData))]
        public void VerifyAllExportsOfOneComponentComingWithSameAppliesToAttribute(ComposablePartDefinition definition)
        {

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
                _errors.Add($"{definition.Type.FullName} exports multiple values with different capability requirement.");
            }

            AssertNoErrors();
        }

        /// <summary>
        /// If a component should always carray AppliesTo metadata, if its consumer requires it.
        /// </summary>
        [Theory]
        [ClassData(typeof(ComposablePartDefinitionTestData))]
        public void VerifyExportsHasAppliesToMetadataWhenItIsRequired(ComposablePartDefinition definition)
        {
            var contractsRequiringMetadata = ComponentComposition.Instance.ContractsRequiringMetadata;


            foreach (KeyValuePair<MemberRef, ExportDefinition> exportDefinitionPair in definition.ExportDefinitions)
            {
                // If the exports has already had the metadata, it is good.
                ExportDefinition exportDefinition = exportDefinitionPair.Value;
                if (exportDefinition.Metadata.ContainsKey(nameof(AppliesToAttribute.AppliesTo)))
                {
                    continue;
                }

                // Check whether the export satisfy any contract required the appliesTo metadata.
                // If it matches one of the contract, we will report an error, because it lacks the required metadata.
                if (contractsRequiringMetadata.TryGetValue(exportDefinition.ContractName, out ISet<Type> contractTypes))
                {
                    MemberRef exportMember = exportDefinitionPair.Key;
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
                        _errors.Add($"{definition.Type.FullName} needs to specify its capability requirement.");
                    }
                }
            }

            AssertNoErrors();
        }

        [Theory]
        [ClassData(typeof(ComposablePartDefinitionTestData))]
        public void AllComponentsMatchesContractMetadata(ComposablePartDefinition definition)
        {
            var contracts = ComponentComposition.Instance.Contracts;

            ProjectSystemContractScope? importScope = null;
            ImportDefinition relatedImports = null;
            foreach (ImportDefinitionBinding import in definition.Imports)
            {
                ImportDefinition importDefinition = import.ImportDefinition;
                if (importDefinition.ExportFactorySharingBoundaries.Count == 0 && contracts.TryGetValue(importDefinition.ContractName, out ComponentComposition.ContractMetadata contractMetadata))
                {
                    if (contractMetadata.Cardinality == ImportCardinality.ZeroOrMore && importDefinition.Cardinality != ImportCardinality.ZeroOrMore)
                    {
                        _errors.Add($"Must use ImportMany in {definition.Type.FullName} to import a contract {importDefinition.ContractName} which can be implemented by an extension.");
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
                foreach (KeyValuePair<MemberRef, ExportDefinition> exportPair in definition.ExportDefinitions)
                {
                    ExportDefinition exportDefinition = exportPair.Value;
                    if (contracts.TryGetValue(exportDefinition.ContractName, out ComponentComposition.ContractMetadata contractMetadata) && contractMetadata.Scope.HasValue)
                    {
                        // Do we live in a child scope (ConfiguredProject) but export to a parent scope (UnconfiguredProject)?
                        if (contractMetadata.Scope.Value < importScope.Value)
                        {
                            _errors.Add($"{definition.Type.FullName} exports to the {contractMetadata.Scope.Value.ToString()} scope, but it imports {relatedImports.ContractName} from {importScope.Value.ToString()} scope, which is invalid.");
                        }
                    }
                }
            }

            AssertNoErrors();
        }

        [Theory]
        [ClassData(typeof(ComposablePartDefinitionTestData))]
        public void AllInterfaceContractsHaveMetadata(ComposablePartDefinition definition)
        {
            var contracts = ComponentComposition.Instance.Contracts;

            foreach (KeyValuePair<MemberRef, ExportDefinition> exportPair in definition.ExportDefinitions)
            {
                ExportDefinition exportDefinition = exportPair.Value;
                if (!CheckContractHasMetadata(exportDefinition.ContractName, definition, contracts, ComponentComposition.Instance.InterfaceNames))
                {
                    if (exportDefinition.ContractName.StartsWith("Microsoft.VisualStudio.ProjectSystem.", StringComparison.Ordinal))
                    {
                        _errors.Add($"{definition.Type.FullName} exports a contract {exportDefinition.ContractName}, which doesn't have contract metadata.");
                    }
                }
            }

            foreach (ImportDefinitionBinding import in definition.Imports)
            {
                ImportDefinition importDefinition = import.ImportDefinition;

                CheckContractHasMetadata(importDefinition.ContractName, definition, contracts, ComponentComposition.Instance.InterfaceNames);
            }

            AssertNoErrors();
        }


        private bool CheckContractHasMetadata(string contractName, ComposablePartDefinition part, IDictionary<string, ComponentComposition.ContractMetadata> contractMetadata, ISet<string> interfaceNames)
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

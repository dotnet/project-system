// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.ProjectSystem.VS.LanguageServices;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal partial class ComponentComposition
    {
        /// <summary>
        /// The list of assemblies that may contain <see cref="ProjectSystemContractProvider.System"/> exports.
        /// </summary>
        internal static readonly IReadOnlyList<Assembly> BuiltInAssemblies = new Assembly[]
        {
            typeof(ConfiguredProjectImplicitActivationTracking).Assembly,   // Microsoft.VisualStudio.ProjectSystem.Managed
            typeof(VsContainedLanguageComponentsFactory).Assembly,          // Microsoft.VisualStudio.ProjectSystem.Managed.VS
        };

        /// <summary>
        /// The list of assemblies to scan for contracts.
        /// </summary>
        private static readonly IReadOnlyList<Assembly> ContractAssemblies = new Assembly[]
        {
            typeof(IProjectService).Assembly,                               // Microsoft.VisualStudio.ProjectSystem
            typeof(IVsProjectServices).Assembly,                            // Microsoft.VisualStudio.ProjectSystem.VS
            typeof(ConfiguredProjectImplicitActivationTracking).Assembly,   // Microsoft.VisualStudio.ProjectSystem.Managed
            typeof(VsContainedLanguageComponentsFactory).Assembly,          // Microsoft.VisualStudio.ProjectSystem.Managed.VS
        };

        public static readonly ComponentComposition Instance = new();

        public ComponentComposition()
        {
            var discovery = PartDiscovery.Combine(new AttributedPartDiscoveryV1(Resolver.DefaultInstance),
                                                  new AttributedPartDiscovery(Resolver.DefaultInstance, isNonPublicSupported: true));

            var parts = discovery.CreatePartsAsync(BuiltInAssemblies).GetAwaiter().GetResult();
            var scopeParts = discovery.CreatePartsAsync(typeof(UnconfiguredProjectScope), typeof(ConfiguredProjectScope), typeof(ProjectServiceScope), typeof(GlobalScope)).GetAwaiter().GetResult();

            ComposableCatalog catalog = ComposableCatalog.Create(Resolver.DefaultInstance)
                .AddParts(parts)
                .AddParts(scopeParts)
                .WithCompositionService();

            // Prepare the self-host service and composition
            Catalog = catalog;
            Configuration = CompositionConfiguration.Create(catalog);
            Contracts = CollectContractMetadata(ContractAssemblies.Union(BuiltInAssemblies));
            ContractsRequiringAppliesTo = CollectContractsRequiringAppliesTo(catalog);
            InterfaceNames = CollectInterfaceNames(ContractAssemblies);
        }

        public ComposableCatalog Catalog { get; }

        public CompositionConfiguration Configuration { get; }

        public IDictionary<string, ContractMetadata> Contracts { get; }

        public IDictionary<string, ISet<Type>> ContractsRequiringAppliesTo { get; }

        public ISet<string> InterfaceNames { get; }

        public ComposedPart? FindComposedPart(Type type)
        {
            foreach (ComposedPart part in Configuration.Parts)
            {
                if (type == part.Definition.Type)
                {
                    return part;
                }
            }

            return null;
        }

        public ComposablePartDefinition? FindComposablePartDefinition(Type type)
        {
            foreach (ComposablePartDefinition part in Catalog.Parts)
            {
                if (type == part.Type)
                {
                    return part;
                }
            }

            return null;
        }

        private static IDictionary<string, ISet<Type>> CollectContractsRequiringAppliesTo(ComposableCatalog catalog)
        {
            var contractsRequiringAppliesTo = new Dictionary<string, ISet<Type>>();

            // First step, we scan all imports, and gather all places requiring "AppliesTo" metadata.
            foreach (ComposablePartDefinition part in catalog.Parts)
            {
                foreach (ImportDefinitionBinding import in part.ImportingMembers)
                {
                    if (IsAppliesToRequired(import))
                    {
                        if (!contractsRequiringAppliesTo.TryGetValue(import.ImportDefinition.ContractName, out ISet<Type> contractTypes))
                        {
                            contractTypes = new HashSet<Type>();
                            contractsRequiringAppliesTo.Add(import.ImportDefinition.ContractName, contractTypes);
                        }

                        if (import.ImportingSiteElementType is not null)
                        {
                            contractTypes.Add(import.ImportingSiteElementType);
                        }
                    }
                }
            }

            return contractsRequiringAppliesTo;
        }

        private static ISet<string> CollectInterfaceNames(IEnumerable<Assembly> assemblies)
        {
            var interfaceNames = new HashSet<string>();
            foreach (Assembly assembly in assemblies)
            {
                foreach (Type type in GetTypes(assembly))
                {
                    if (type.IsPublic && type.IsInterface)
                    {
                        interfaceNames.Add(type.FullName);
                    }
                }
            }

            return interfaceNames;
        }

        private static Dictionary<string, ContractMetadata> CollectContractMetadata(IEnumerable<Assembly> assemblies)
        {
            Requires.NotNull(assemblies, nameof(assemblies));
            var contracts = new Dictionary<string, ContractMetadata>(StringComparer.Ordinal);
            foreach (Assembly contractAssembly in assemblies)
            {
                ReadContractMetadata(contracts, contractAssembly);
            }

            return contracts;
        }

        private static void ReadContractMetadata(Dictionary<string, ContractMetadata> contracts, Assembly contractAssembly)
        {
            Requires.NotNull(contracts, nameof(contracts));
            Requires.NotNull(contractAssembly, nameof(contractAssembly));
            foreach (ProjectSystemContractAttribute assemblyAttribute in contractAssembly.GetCustomAttributes<ProjectSystemContractAttribute>())
            {
                var contractName = assemblyAttribute.ContractName;
                var contractType = assemblyAttribute.ContractType;

                if (contractName is not null || contractType is not null)
                {
                    AddContractMetadata(contracts, contractName ?? contractType!.FullName, assemblyAttribute.Scope, assemblyAttribute.Provider, assemblyAttribute.Cardinality);
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

        private static Type[] GetTypes(Assembly assembly)
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
                    return ex.Types.WhereNotNull().ToArray();

                string message = ex.ToString();

                message += "\nLoaderExceptions:\n";

                for (int i = 0; i < ex.LoaderExceptions.Length; i++)
                {
                    message += ex.LoaderExceptions[i].ToString();
                    message += "\n";
                }

                throw new Xunit.Sdk.XunitException(message);
            }
        }

        private static bool IsIgnorable(Exception exception)
        {
            if (exception is FileNotFoundException fileNotFound)
            {
                return fileNotFound.FileName.StartsWith("Microsoft.VisualStudio.ProjectServices,", StringComparison.Ordinal);
            }

            return false;
        }

        private static void AddContractMetadata(Dictionary<string, ContractMetadata> contracts, string name, ProjectSystemContractScope scope, ProjectSystemContractProvider provider, ImportCardinality cardinality)
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

        /// <summary>
        /// Check whether the import requiring a component to have "AppliesTo" metadata.
        /// If the imports ask metadata from the exports, and the metadata based on IAppliesToMetadataView,
        /// the "AppliesTo" metadata is required.
        /// </summary>
        private static bool IsAppliesToRequired(ImportDefinitionBinding import)
        {
            Type appliesToView = typeof(IAppliesToMetadataView);
            return import.MetadataType is not null && appliesToView.IsAssignableFrom(import.MetadataType);
        }
    }
}

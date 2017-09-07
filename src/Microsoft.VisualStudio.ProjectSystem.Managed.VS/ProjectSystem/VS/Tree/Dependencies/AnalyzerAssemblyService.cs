// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    [Export(typeof(IAnalyzerAssemblyService))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    internal class AnalyzerAssemblyService : IAnalyzerAssemblyService
    {
        private object _guard = new object();
        private Dictionary<string, bool> _map = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        public bool ContainsDiagnosticAnalyzers(string fullPathToAssembly)
        {
            lock (_guard)
            {
                if (!_map.TryGetValue(fullPathToAssembly, out bool containsDiagnosticAnalyzers))
                {
                    containsDiagnosticAnalyzers = ContainsDiagnosticAnalyzersCore(fullPathToAssembly);
                    _map.Add(fullPathToAssembly, containsDiagnosticAnalyzers);
                }

                return containsDiagnosticAnalyzers;
            }
        }

        private static bool ContainsDiagnosticAnalyzersCore(string path)
        {
            try
            {
                using (var assemblyStream = System.IO.File.OpenRead(path))
                using (var peReader = new PEReader(assemblyStream, PEStreamOptions.LeaveOpen))
                {
                    var reader = peReader.GetMetadataReader();
                    foreach (var typeDefinitionHandle in reader.TypeDefinitions)
                    {
                        var typeDefinition = reader.GetTypeDefinition(typeDefinitionHandle);
                        foreach (var customAttributeHandle in typeDefinition.GetCustomAttributes())
                        {
                            if (IsTargetAttribute(reader,
                                                  customAttributeHandle,
                                                  namespaceName: "Microsoft.CodeAnalysis.Diagnostics",
                                                  typeName: "DiagnosticAnalyzerAttribute"))
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            catch
            {
                // Ignore any errors in reading the assembly metadata.
            }

            return false;
        }

        private static bool IsTargetAttribute(
            System.Reflection.Metadata.MetadataReader metadataReader,
            CustomAttributeHandle customAttributeHandle,
            string namespaceName,
            string typeName)
        {
            if (!GetTypeAndConstructor(metadataReader, customAttributeHandle, out EntityHandle ctorTypeHandle, out EntityHandle ctorHandle))
            {
                return false;
            }

            if (!GetAttributeNamespaceAndName(metadataReader, ctorTypeHandle, out StringHandle ctorTypeNamespaceHandle, out StringHandle ctorTypeNameHandle))
            {
                return false;
            }

            try
            {
                return metadataReader.StringComparer.Equals(ctorTypeNameHandle, typeName)
                    && metadataReader.StringComparer.Equals(ctorTypeNamespaceHandle, namespaceName);
            }
            catch (BadImageFormatException)
            {
                return false;
            }
        }

        private static bool GetTypeAndConstructor(
            System.Reflection.Metadata.MetadataReader metadataReader,
            CustomAttributeHandle customAttributeHandle,
            out EntityHandle ctorType,
            out EntityHandle attributeCtor
            )
        {
            try
            {
                ctorType = default(EntityHandle);

                attributeCtor = metadataReader.GetCustomAttribute(customAttributeHandle).Constructor;

                if (attributeCtor.Kind == HandleKind.MemberReference)
                {
                    var memberRef = metadataReader.GetMemberReference((MemberReferenceHandle)attributeCtor);
                    var ctorName = memberRef.Name;

                    if (!metadataReader.StringComparer.Equals(ctorName, ".ctor"))
                    {
                        // Not a constructor.
                        return false;
                    }

                    ctorType = memberRef.Parent;
                }
                else if (attributeCtor.Kind == HandleKind.MethodDefinition)
                {
                    var methodDef = metadataReader.GetMethodDefinition((MethodDefinitionHandle)attributeCtor);
                    var ctorName = methodDef.Name;

                    if (!metadataReader.StringComparer.Equals(ctorName, ".ctor"))
                    {
                        // Not a constructor.
                        return false;
                    }

                    ctorType = methodDef.GetDeclaringType();
                }
                else
                {
                    // Unsupported metadata.
                    return false;
                }

                return true;
            }
            catch (BadImageFormatException)
            {
                ctorType = default(EntityHandle);
                attributeCtor = default(EntityHandle);
                return false;
            }
        }

        private static bool GetAttributeNamespaceAndName(
            System.Reflection.Metadata.MetadataReader metadataReader,
            EntityHandle typeDefOrRef,
            out StringHandle namespaceHandle,
            out StringHandle nameHandle)
        {
            nameHandle = default(StringHandle);
            namespaceHandle = default(StringHandle);

            try
            {
                if (typeDefOrRef.Kind == HandleKind.TypeReference)
                {
                    var typeRefRow = metadataReader.GetTypeReference((TypeReferenceHandle)typeDefOrRef);
                    var handleType = typeRefRow.ResolutionScope.Kind;

                    if (handleType == HandleKind.TypeReference || handleType == HandleKind.TypeDefinition)
                    {
                        // TODO - Support nested types.
                        return false;
                    }

                    nameHandle = typeRefRow.Name;
                    namespaceHandle = typeRefRow.Namespace;
                }
                else if (typeDefOrRef.Kind == HandleKind.TypeDefinition)
                {
                    var def = metadataReader.GetTypeDefinition((TypeDefinitionHandle)typeDefOrRef);

                    if (IsNested(def.Attributes))
                    {
                        // TODO - Support nested types.
                        return false;
                    }

                    nameHandle = def.Name;
                    namespaceHandle = def.Namespace;
                }
                else
                {
                    // Unsupported metadata.
                    return false;
                }

                return true;
            }
            catch (BadImageFormatException)
            {
                return false;
            }
        }

        private static bool IsNested(TypeAttributes flags)
        {
            return (flags & ((TypeAttributes)0x0000006)) != 0;
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using System.Runtime.InteropServices;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using VSLangProj;
using VSLangProj80;

namespace Microsoft.VisualStudio.ProjectSystem.VS.References
{
    /// <summary>
    ///     Provides an implementation of <see cref="IVsDesignTimeAssemblyResolution"/> that sits over the top of VSLangProj.References.
    /// </summary>
    [ExportProjectNodeComService(typeof(IVsDesignTimeAssemblyResolution))]  // Need to override CPS's version, which it implements on the project node as IVsDesignTimeAssemblyResolution
    [ExportVsProfferedProjectService(typeof(SVsDesignTimeAssemblyResolution))]
    [AppliesTo(ProjectCapability.DotNet)]
    [Order(Order.Default)] // Before CPS's version
    internal partial class DesignTimeAssemblyResolution : IVsDesignTimeAssemblyResolution, IDisposable
    {
        // NOTE: Unlike the legacy project system, this implementation does resolve only "framework" assemblies. In .NET Core and other project types, framework assemblies
        // are not treated specially - they just come through as normal references from packages. We also do not have a static registration of what assemblies would make up 
        // a framework, so we assume that what the project is referencing represents the "framework" of accessible types. This is the same as what the legacy project system
        // does under the UWP flavor (when the DTARUseReferencesFromProject MSBuild property is set).
        // 
        // This implementation will work for .NET Core based projects, but we might run into unexpected behavior when bringing up legacy projects where designers/components
        // expect to ask for/use types that are not currently referenced by the project. We should revisit at that time.
        //
        // Ideally this would sit on a simple wrapper over the top of project subscription service, however, CPS's internal ReferencesHostBridge, which populates VSLangProj.References,
        // already does the work to listen to the project subscription for reference adds/removes/changes and makes sure to publish the results in sync with the solution tree.
        // We just use its results.
        private IUnconfiguredProjectVsServices? _projectVsServices;

        [ImportingConstructor]
        public DesignTimeAssemblyResolution(IUnconfiguredProjectVsServices projectVsServices)
        {
            Requires.NotNull(projectVsServices, nameof(projectVsServices));

            _projectVsServices = projectVsServices;
        }

        public int GetTargetFramework(out string? ppTargetFramework)
        {
            if (_projectVsServices is null)
            {
                ppTargetFramework = null;
                return HResult.Unexpected;
            }

            return _projectVsServices.VsHierarchy.GetProperty(VsHierarchyPropID.TargetFrameworkMoniker, defaultValue: null, result: out ppTargetFramework);
        }

        public int ResolveAssemblyPathInTargetFx(string?[]? prgAssemblySpecs, uint cAssembliesToResolve, VsResolvedAssemblyPath[]? prgResolvedAssemblyPaths, out uint pcResolvedAssemblyPaths)
        {
            if (prgAssemblySpecs is null || cAssembliesToResolve == 0 || prgResolvedAssemblyPaths is null || cAssembliesToResolve != prgAssemblySpecs.Length || cAssembliesToResolve != prgResolvedAssemblyPaths.Length)
            {
                pcResolvedAssemblyPaths = 0;
                return HResult.InvalidArg;
            }

            if (!TryParseAssemblyNames(prgAssemblySpecs, out AssemblyName[] assemblyNames))
            {
                pcResolvedAssemblyPaths = 0;
                return HResult.InvalidArg;
            }

            if (_projectVsServices is null)
            {
                pcResolvedAssemblyPaths = 0;
                return HResult.Unexpected;
            }

            pcResolvedAssemblyPaths = ResolveReferences(prgAssemblySpecs, assemblyNames, prgResolvedAssemblyPaths);
            return HResult.OK;
        }

        private uint ResolveReferences(string?[] originalNames, AssemblyName[] assemblyName, [In, Out]VsResolvedAssemblyPath[] assemblyPaths)
        {
            Assumes.True(originalNames.Length == assemblyName.Length && originalNames.Length == assemblyPaths.Length);

            uint resolvedReferencesCount = 0;
            IDictionary<string, ResolvedReference> references = GetAllResolvedReferences();

            for (int i = 0; i < assemblyName.Length; i++)
            {
                string? resolvedPath = FindResolvedAssemblyPath(references, assemblyName[i]);
                if (resolvedPath is not null)
                {
                    assemblyPaths[resolvedReferencesCount] = new VsResolvedAssemblyPath()
                    {
                        bstrOrigAssemblySpec = originalNames[i],    // Note we use the original name, not the parsed name, as they could be different
                        bstrResolvedAssemblyPath = resolvedPath
                    };

                    resolvedReferencesCount++;
                }
            }

            return resolvedReferencesCount;
        }

        private static string? FindResolvedAssemblyPath(IDictionary<string, ResolvedReference> references, AssemblyName assemblyName)
        {
            // NOTE: We mimic the behavior of the legacy project system when in "DTARUseReferencesFromProject" mode, it matches 
            // only on version, and only against currently referenced assemblies, nothing more. 
            //
            // See ResolveAssemblyReference in vs\env\vscore\package\MSBuild\ToolLocationHelperShim.cs
            //
            // 
            if (references.TryGetValue(assemblyName.Name, out ResolvedReference reference))
            {
                // If the caller didn't specify a version, than they only want to match on name
                if (assemblyName.Version is null)
                    return reference.ResolvedPath;

                // If the reference is the same or higher than the requested version, then we consider it a match
                if (reference.Version is not null && reference.Version >= assemblyName.Version)
                    return reference.ResolvedPath;
            }

            return null;
        }

        private IDictionary<string, ResolvedReference> GetAllResolvedReferences()
        {
            var resolvedReferences = new Dictionary<string, ResolvedReference>(StringComparer.Ordinal);

            VSProject? project = GetVSProject();
            if (project is not null)
            {
                foreach (Reference3 reference in project.References.OfType<Reference3>())
                {
                    // We only want resolved assembly references
                    if (reference.RefType == (uint)__PROJECTREFERENCETYPE.PROJREFTYPE_ASSEMBLY && reference.Resolved)
                    {
                        resolvedReferences[reference.Name] = new ResolvedReference(reference.Path, TryParseVersionOrNull(reference.Version));
                    }
                }
            }

            return resolvedReferences;
        }

        private VSProject? GetVSProject()
        {
            Project? project = _projectVsServices?.VsHierarchy.GetProperty<Project>(VsHierarchyPropID.ExtObject);

            return project?.Object as VSProject;
        }

        private static bool TryParseAssemblyNames(string?[] assemblyNames, out AssemblyName[] result)
        {
            result = new AssemblyName[assemblyNames.Length];

            for (int i = 0; i < assemblyNames.Length; i++)
            {
                string? assemblyName = assemblyNames[i];

                if (string.IsNullOrEmpty(assemblyName))
                    return false;

                try
                {
                    result[i] = new AssemblyName(assemblyName);
                }
                catch (FileLoadException)
                {
                    return false;
                }
            }

            return true;
        }

        private static Version? TryParseVersionOrNull(string version)
        {
            if (Version.TryParse(version, out Version result))
            {
                return result;
            }

            return null;
        }

        public void Dispose()
        {
            // Important for ProjectNodeComServices to null out fields to reduce the amount 
            // of data we leak when extensions incorrectly holds onto the IVsHierarchy.
            _projectVsServices = null;
        }
    }
}

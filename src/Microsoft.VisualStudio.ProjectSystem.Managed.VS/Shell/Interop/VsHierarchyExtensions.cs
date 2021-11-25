// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.VS;
using Microsoft.VisualStudio.ProjectSystem.VS.Utilities;

namespace Microsoft.VisualStudio.Shell.Interop
{
    /// <summary>
    ///     Provides extension methods for <see cref="IVsHierarchy"/> instances.
    /// </summary>
    internal static class VsHierarchyExtensions
    {
        /// <summary>
        ///     Returns the GUID of the specified property.
        /// </summary>
        public static Guid GetGuidProperty(this IVsHierarchy hierarchy, VsHierarchyPropID property)
        {
            Requires.NotNull(hierarchy, nameof(hierarchy));
            Verify.HResult(hierarchy.GetGuidProperty(HierarchyId.Root, (int)property, out Guid result));

            return result;
        }

        /// <summary>
        ///     Gets the value of the specified property if the hierarchy supports it, or throws an exception if there was an error.
        /// </summary>
        public static T? GetProperty<T>(this IVsHierarchy hierarchy, VsHierarchyPropID property, T? defaultValue = default)
        {
            return GetProperty(hierarchy, HierarchyId.Root, property, defaultValue);
        }

        /// <summary>
        ///     Gets the value of the specified property of the specified item if the hierarchy supports it, or throws an exception if there was an error.
        /// </summary>
        public static T? GetProperty<T>(this IVsHierarchy hierarchy, HierarchyId item, VsHierarchyPropID property, T? defaultValue = default)
        {
            Verify.HResult(GetProperty(hierarchy, item, property, defaultValue, out T? result));

            return result;
        }

        /// <summary>
        ///     Gets the value of the specified property if the hierarchy supports it, or returns a HRESULT if there was an error.
        /// </summary>
        public static int GetProperty<T>(this IVsHierarchy hierarchy, VsHierarchyPropID property, T? defaultValue, out T? result)
        {
            return GetProperty(hierarchy, HierarchyId.Root, property, defaultValue, out result);
        }

        /// <summary>
        ///     Gets the value of the specified property of the specified item if the hierarchy supports it, or returns a HRESULT if there was an error.
        /// </summary>
        public static int GetProperty<T>(this IVsHierarchy hierarchy, HierarchyId item, VsHierarchyPropID property, T defaultValue, out T? result)
        {
            Requires.NotNull(hierarchy, nameof(hierarchy));

            if (item.IsNilOrEmpty || item.IsSelection)
                throw new ArgumentException(null, nameof(item));

            HResult hr = hierarchy.GetProperty(item, (int)property, out object resultObject);
            if (hr.IsOK)
            {
                // NOTE: We consider it a bug in the underlying project system or the caller if this cast fails
                result = (T)resultObject;
                return HResult.OK;
            }

            if (hr == HResult.MemberNotFound)
            {
                result = defaultValue;
                return HResult.OK;
            }

            result = default!;
            return hr;
        }

        /// <summary>
        /// Returns EnvDTE.Project object for the hierarchy
        /// </summary>
        public static EnvDTE.Project? GetDTEProject(this IVsHierarchy hierarchy)
        {
            UIThreadHelper.VerifyOnUIThread();
            if (ErrorHandler.Succeeded(hierarchy.GetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ExtObject, out object extObject)))
            {
                return extObject as EnvDTE.Project;
            }

            return null;
        }

        /// <summary>
        /// Returns the path to the project file. Assumes the hierarchy implements IVsProject. Returns null on failure
        /// </summary>
        public static string? GetProjectFilePath(this IVsHierarchy hierarchy)
        {
            if (ErrorHandler.Succeeded(((IVsProject)hierarchy).GetMkDocument(VSConstants.VSITEMID_ROOT, out string projectPath)))
            {
                return projectPath;
            }

            return null;
        }
    }
}

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Properties;
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
            HResult hr = hierarchy.GetGuidProperty(HierarchyId.Root, (int)property, out Guid result);
            if (hr.Failed)
                throw hr.Exception;

            return result;
        }

        /// <summary>
        ///     Gets the value of the specified property if the hierarchy supports it, or throws an excepton if there was an error.
        /// </summary>
        public static T GetProperty<T>(this IVsHierarchy hierarchy, VsHierarchyPropID property, T defaultValue = default(T))
        {
            return GetProperty(hierarchy, HierarchyId.Root, property, defaultValue);
        }

        /// <summary>
        ///     Gets the value of the specified property of the specified item if the hierarchy supports it, or throws an exception if there was an error.
        /// </summary>
        public static T GetProperty<T>(this IVsHierarchy hierarchy, HierarchyId item, VsHierarchyPropID property, T defaultValue = default(T))
        {
            HResult hr = GetProperty(hierarchy, item, property, defaultValue, out T result);
            if (hr.Failed)
                throw hr.Exception;

            return result;
        }

        /// <summary>
        ///     Gets the value of the specified property if the hierarchy supports it, or returns a HRESULT if there was an error.
        /// </summary>
        public static int GetProperty<T>(this IVsHierarchy hierarchy, VsHierarchyPropID property, T defaultValue, out T result)
        {
            return GetProperty(hierarchy, HierarchyId.Root, property, defaultValue, out result);
        }

        /// <summary>
        ///     Gets the value of the specified property of the specified item if the hierarchy supports it, or returns a HRESULT if there was an error.
        /// </summary>
        public static int GetProperty<T>(this IVsHierarchy hierarchy, HierarchyId item, VsHierarchyPropID property, T defaultValue, out T result)
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

            if (hr == VSConstants.DISP_E_MEMBERNOTFOUND)
            {
                result = defaultValue;
                return HResult.OK;
            }

            result = default(T);
            return hr;
        }

        /// <summary>
        /// Convenient way to get to the UnconfiguredProject from the hierarchy
        /// </summary>
        public static UnconfiguredProject GetUnconfiguredProject(this IVsHierarchy hierarchy)
        {
            UIThreadHelper.VerifyOnUIThread();

            var context = hierarchy as IVsBrowseObjectContext;
            if (context == null)
            {
                EnvDTE.Project dteProject = hierarchy.GetDTEProject();
                if (dteProject != null)
                {
                    context = dteProject.Object as IVsBrowseObjectContext;
                }
            }

            return context?.UnconfiguredProject;
        }

        /// <summary>
        /// Returns EnvDTE.Project object for the hierarchy
        /// </summary>
        public static EnvDTE.Project GetDTEProject(this IVsHierarchy hierarchy)
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
        public static string GetProjectFilePath(this IVsHierarchy hierarchy)
        {
            if(ErrorHandler.Succeeded(((IVsProject)hierarchy).GetMkDocument(VSConstants.VSITEMID_ROOT, out string projectPath)))
            {
                return projectPath;
            }

            return null;
        }
    }
}

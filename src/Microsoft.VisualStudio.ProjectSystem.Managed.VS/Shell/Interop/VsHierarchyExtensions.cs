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

            Guid result;
            HResult hr = hierarchy.GetGuidProperty(HierarchyId.Root, (int)property, out result);
            if (hr.Failed)
                throw hr.Exception;

            return result;
        }

        /// <summary>
        ///     Gets the value of the specified property if the hierarchy supports it.
        /// </summary>
        public static T GetProperty<T>(this IVsHierarchy hierarchy, VsHierarchyPropID property, T defaultValue)
        {
            return GetProperty(hierarchy, HierarchyId.Root, property, defaultValue);
        }

        /// <summary>
        ///     Gets the value of the specified property if the hierarchy supports it.
        /// </summary>
        public static T GetProperty<T>(this IVsHierarchy hierarchy, HierarchyId item, VsHierarchyPropID property, T defaultValue)
        {
            Requires.NotNull(hierarchy, nameof(hierarchy));

            if (item.IsNilOrEmpty || item.IsSelection)
                throw new ArgumentException(null, nameof(item));

            object resultObject;
            HResult hr = hierarchy.GetProperty(item, (int)property, out resultObject);
            if (hr == VSConstants.DISP_E_MEMBERNOTFOUND)
                return defaultValue;

            if (hr.Failed)
                throw hr.Exception;

            // NOTE: We consider it a bug in the underlying project system or the caller if this cast fails
            return (T)resultObject;
        }

        /// <summary>
        /// Convenient way to get to the UnconfiguredProject from the hierarchy
        /// </summary>
        public static UnconfiguredProject GetUnconfiguredProject(this IVsHierarchy hierarchy)
        {
            UIThreadHelper.VerifyOnUIThread();

            IVsBrowseObjectContext context = hierarchy as IVsBrowseObjectContext;
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

            object extObject;
            if (ErrorHandler.Succeeded(hierarchy.GetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ExtObject, out extObject)))
            {
                return extObject as EnvDTE.Project;
            }

            return null;
        }
    }
}

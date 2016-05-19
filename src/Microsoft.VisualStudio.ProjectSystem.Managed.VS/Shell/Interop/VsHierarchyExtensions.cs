// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.ProjectSystem.VS;

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
        /// Returns EnvDTE.ProjectItem object for the given filename. Returns null if file is not in the project
        /// or fails.
        /// </summary>
        public static EnvDTE.ProjectItem GetDTEProjectItemForFile(this IVsHierarchy hierarchy, string mkDocument)
        {
            return hierarchy.GetHierarchyPropertyForFile<EnvDTE.ProjectItem>((int)__VSHPROPID.VSHPROPID_ExtObject, mkDocument);
        }

        /// <summary>
        /// Returns any hierarhcy property for a file
        /// </summary>
        public static T GetHierarchyPropertyForFile<T>(this IVsHierarchy hierarchy, int property, string mkDocument) where T : class
        {
            //UIThreadHelper.VerifyOnUIThread();
            object propertyValue;
            IVsProject proj = hierarchy as IVsProject;
            if (proj != null)
            {
                int hr;
                int isFound = 0;
                uint itemid = VSConstants.VSITEMID_NIL;
                VSDOCUMENTPRIORITY[] priority = new VSDOCUMENTPRIORITY[1];
                hr = proj.IsDocumentInProject(mkDocument, out isFound, priority, out itemid);

                if (ErrorHandler.Succeeded(hr) && isFound != 0 && itemid != VSConstants.VSITEMID_NIL)
                {
                    if (ErrorHandler.Succeeded(hierarchy.GetProperty(itemid, property, out propertyValue)))
                    {
                        return propertyValue as T;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Returns EnvDTE.Project object for the hierarchy
        /// </summary>
        public static EnvDTE.Project GetDTEProject(this IVsHierarchy hierarchy)
        {

            //UIThreadHelper.VerifyOnUIThread();
            object extObject;
            if (ErrorHandler.Succeeded(hierarchy.GetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ExtObject, out extObject)))
            {
                return extObject as EnvDTE.Project;
            }
            return null;
        }

        /// <summary>
        /// Returns the default namespace for the given filename. Returns null if file is not in the project
        /// or fails.
        /// </summary>
        public static string GetDefaultNamespaceForFile(this IVsHierarchy hierarchy, string mkDocument)
        {
            return hierarchy.GetHierarchyPropertyForFile<string>((int)__VSHPROPID.VSHPROPID_DefaultNamespace, mkDocument);
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.VS.Properties;
using BCLDebug = System.Diagnostics.Debug;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Automation
{
    /// <summary>
    ///     An abstract class that maps <see cref="IExtenderCATIDProvider"/> to a more correct model.
    /// </summary>
    internal abstract class AbstractExtenderCATIDProvider : IExtenderCATIDProvider
    {
        public string? GetExtenderCATID(ExtenderCATIDType extenderCATIDType, IProjectTree? treeNode)
        {
            // CPS's implementation of ExtenderCATIDType incorrectly treats the same "instances" as distinct items based 
            // where they are accessed in CPS. It also incorrectly maps "HierarchyExtensionObject" and "HierarchyBrowseObject" 
            // as only applying to the hierarchy, when they take ITEMIDs indicating the node they apply to.
            //
            // See https://docs.microsoft.com/en-us/visualstudio/extensibility/internals/extending-the-object-model-of-the-base-project.
            //
            // The latter issue we can do nothing about, however, to address the former, map these types to a truer form it makes 
            // it easier on implementors and maintainers of this to understand the objects we're talking about.

            return extenderCATIDType switch
            {
                ExtenderCATIDType.HierarchyExtensionObject or ExtenderCATIDType.AutomationProject => GetExtenderCATID(ExtendeeObject.Project),               // IVsHierarchy.GetProperty(VSITEMID.Root, VSHPROPID_ExtObjectCATID)
                                                                                                                                                             // DTE.Project
                ExtenderCATIDType.HierarchyBrowseObject or ExtenderCATIDType.ProjectBrowseObject => GetExtenderCATID(ExtendeeObject.ProjectBrowseObject),    // IVsHierarchy.GetProperty(VSHPROPID_BrowseObjectCATID)
                                                                                                                                                             // EnvDTE.Project.Properties
                ExtenderCATIDType.ConfigurationBrowseObject => GetExtenderCATID(ExtendeeObject.Configuration),                                               // IVsCfgProvider2.GetCfgProviderProperty(VSCFGPROPID_IntrinsicExtenderCATID)/DTE.Configuration
                ExtenderCATIDType.HierarchyConfigurationBrowseObject or ExtenderCATIDType.ProjectConfigurationBrowseObject => GetExtenderCATID(ExtendeeObject.ConfigurationBrowseObject), // IVsHierarchy.GetProperty(VSHPROPID_CfgBrowseObjectCATID)
                                                                                                                                                             // EnvDTE.Configuration.Properties
                ExtenderCATIDType.AutomationProjectItem => GetExtenderCATID(ExtendeeObject.ProjectItem),                                                     // EnvDTE.ProjectItem
                ExtenderCATIDType.AutomationReference or ExtenderCATIDType.ReferenceBrowseObject => GetExtenderCATID(ExtendeeObject.ReferenceBrowseObject),  // VSLangProject.Reference
                                                                                                                                                             // EnvDTE.ProjectItem.Properties (when reference)
                ExtenderCATIDType.FileBrowseObject => GetExtenderCATID(ExtendeeObject.FileBrowseObject),                                                     // EnvDTE.ProjectItem.Properties (when file)
                ExtenderCATIDType.AutomationFolderProperties or ExtenderCATIDType.FolderBrowseObject => GetExtenderCATID(ExtendeeObject.FolderBrowseObject), // FolderProperties or EnvDTE.ProjectItem.Properties (when folder)
                ExtenderCATIDType.Unknown or _ => UnknownOrDefault()                                                                                         // EnvDTE.ProjectItem.Properties (when not file, folder, reference)
            };

            string? UnknownOrDefault()
            {
                BCLDebug.Assert(extenderCATIDType == ExtenderCATIDType.Unknown, $"Unrecognized CATID type {extenderCATIDType}");
                return null;
            }
        }

        protected abstract string GetExtenderCATID(ExtendeeObject extendee);

        protected enum ExtendeeObject
        {
            /// <summary>
            ///     Represents <see cref="EnvDTE.Project"/>.
            /// </summary>
            Project,

            /// <summary>
            ///     Represents <see cref="EnvDTE.Project.Properties"/>.
            /// </summary>
            ProjectBrowseObject,

            /// <summary>
            ///     Represents <see cref="EnvDTE.Configuration"/>.
            /// </summary>
            Configuration,

            /// <summary>
            ///     Represents <see cref="EnvDTE.Configuration.Properties"/>.
            /// </summary>
            ConfigurationBrowseObject,

            /// <summary>
            ///     Represents <see cref="EnvDTE.ProjectItem"/>.
            /// </summary>
            ProjectItem,

            /// <summary>
            ///     Represents <see cref="EnvDTE.ProjectItem.Properties"/> when a file.
            /// </summary>
            FileBrowseObject,

            /// <summary>
            ///     Represents <see cref="EnvDTE.ProjectItem.Properties"/> when a folder.
            /// </summary>
            FolderBrowseObject,

            /// <summary>
            ///     Represents <see cref="VSLangProj.Reference"/> or <see cref="EnvDTE.ProjectItem.Properties"/> when a reference.
            /// </summary>
            ReferenceBrowseObject,
        }
    }
}

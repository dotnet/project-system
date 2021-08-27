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

            switch (extenderCATIDType)
            {
                case ExtenderCATIDType.HierarchyExtensionObject:                                // IVsHierarchy.GetProperty(VSITEMID.Root, VSHPROPID_ExtObjectCATID)
                case ExtenderCATIDType.AutomationProject:                                       // DTE.Project
                    return GetExtenderCATID(ExtendeeObject.Project);

                case ExtenderCATIDType.HierarchyBrowseObject:                                   // IVsHierarchy.GetProperty(VSHPROPID_BrowseObjectCATID)
                case ExtenderCATIDType.ProjectBrowseObject:                                     // EnvDTE.Project.Properties
                    return GetExtenderCATID(ExtendeeObject.ProjectBrowseObject);

                case ExtenderCATIDType.ConfigurationBrowseObject:                               // IVsCfgProvider2.GetCfgProviderProperty(VSCFGPROPID_IntrinsicExtenderCATID)/DTE.Configuration
                    return GetExtenderCATID(ExtendeeObject.Configuration);

                case ExtenderCATIDType.HierarchyConfigurationBrowseObject:                      // IVsHierarchy.GetProperty(VSHPROPID_CfgBrowseObjectCATID)
                case ExtenderCATIDType.ProjectConfigurationBrowseObject:                        // EnvDTE.Configuration.Properties
                    return GetExtenderCATID(ExtendeeObject.ConfigurationBrowseObject);

                case ExtenderCATIDType.AutomationProjectItem:                                   // EnvDTE.ProjectItem
                    return GetExtenderCATID(ExtendeeObject.ProjectItem);

                case ExtenderCATIDType.AutomationReference:                                     // VSLangProject.Reference
                case ExtenderCATIDType.ReferenceBrowseObject:                                   // EnvDTE.ProjectItem.Properties (when reference)
                    return GetExtenderCATID(ExtendeeObject.ReferenceBrowseObject);

                case ExtenderCATIDType.FileBrowseObject:                                        // EnvDTE.ProjectItem.Properties (when file)
                    return GetExtenderCATID(ExtendeeObject.FileBrowseObject);

                case ExtenderCATIDType.AutomationFolderProperties:                              // FolderProperties
                case ExtenderCATIDType.FolderBrowseObject:                                      // EnvDTE.ProjectItem.Properties (when folder)
                    return GetExtenderCATID(ExtendeeObject.FolderBrowseObject);

                default:
                case ExtenderCATIDType.Unknown:                                                 // EnvDTE.ProjectItem.Properties (when not file, folder, reference)
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

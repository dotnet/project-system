// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.VS.Properties;
using VSLangProj;
using BCLDebug = System.Diagnostics.Debug;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Automation.VisualBasic
{
    [Export(typeof(IExtenderCATIDProvider))]
    [AppliesTo(ProjectCapability.VisualBasic)]
    internal class VisualBasicExtenderCATIDProvider : AbstractExtenderCATIDProvider
    {
        [ImportingConstructor]
        public VisualBasicExtenderCATIDProvider()
        {
        }

        protected override string GetExtenderCATID(ExtendeeObject extendee)
        {
            return extendee switch
            {
                ExtendeeObject.Project =>                   PrjCATID.prjCATIDProject,
                ExtendeeObject.ProjectBrowseObject =>       PrjBrowseObjectCATID.prjCATIDVBProjectBrowseObject,
                ExtendeeObject.Configuration =>             PrjBrowseObjectCATID.prjCATIDVBConfig,
                ExtendeeObject.ConfigurationBrowseObject => PrjBrowseObjectCATID.prjCATIDVBProjectConfigBrowseObject,
                ExtendeeObject.ProjectItem =>               PrjCATID.prjCATIDProjectItem,
                ExtendeeObject.FolderBrowseObject =>        PrjBrowseObjectCATID.prjCATIDVBFolderBrowseObject,
                ExtendeeObject.ReferenceBrowseObject =>     PrjBrowseObjectCATID.prjCATIDVBReferenceBrowseObject,
                ExtendeeObject.FileBrowseObject or _ =>     FileBrowseObjectOrDefault()
            };

            string FileBrowseObjectOrDefault()
            {
                BCLDebug.Assert(extendee == ExtendeeObject.FileBrowseObject);
                return PrjBrowseObjectCATID.prjCATIDVBFileBrowseObject;
            }
        }
    }
}

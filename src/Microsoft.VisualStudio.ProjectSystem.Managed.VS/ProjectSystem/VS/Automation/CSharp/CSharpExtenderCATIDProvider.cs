// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.VS.Properties;
using VSLangProj;
using BCLDebug = System.Diagnostics.Debug;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Automation.CSharp
{
    [Export(typeof(IExtenderCATIDProvider))]
    [AppliesTo(ProjectCapability.CSharp)]
    internal class CSharpExtenderCATIDProvider : AbstractExtenderCATIDProvider
    {
        [ImportingConstructor]
        public CSharpExtenderCATIDProvider()
        {
        }

        protected override string GetExtenderCATID(ExtendeeObject extendee)
        {
            return extendee switch
            {
                ExtendeeObject.Project =>                   PrjCATID.prjCATIDProject,
                ExtendeeObject.ProjectBrowseObject =>       PrjBrowseObjectCATID.prjCATIDCSharpProjectBrowseObject,
                ExtendeeObject.Configuration =>             PrjBrowseObjectCATID.prjCATIDCSharpConfig,
                ExtendeeObject.ConfigurationBrowseObject => PrjBrowseObjectCATID.prjCATIDCSharpProjectConfigBrowseObject,
                ExtendeeObject.ProjectItem =>               PrjCATID.prjCATIDProjectItem,
                ExtendeeObject.FolderBrowseObject =>        PrjBrowseObjectCATID.prjCATIDCSharpFolderBrowseObject,
                ExtendeeObject.ReferenceBrowseObject =>     PrjBrowseObjectCATID.prjCATIDCSharpReferenceBrowseObject,
                ExtendeeObject.FileBrowseObject or _ =>     FileBrowseObjectOrDefault()
            };

            string FileBrowseObjectOrDefault()
            {
                BCLDebug.Assert(extendee == ExtendeeObject.FileBrowseObject);
                return PrjBrowseObjectCATID.prjCATIDCSharpFileBrowseObject;
            }
        }
    }
}

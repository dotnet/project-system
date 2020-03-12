// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel.Composition;
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
            switch (extendee)
            {
                case ExtendeeObject.Project:
                    return PrjCATID.prjCATIDProject;

                case ExtendeeObject.ProjectBrowseObject:
                    return PrjBrowseObjectCATID.prjCATIDCSharpProjectBrowseObject;

                case ExtendeeObject.Configuration:
                    return PrjBrowseObjectCATID.prjCATIDCSharpConfig;

                case ExtendeeObject.ConfigurationBrowseObject:
                    return PrjBrowseObjectCATID.prjCATIDCSharpProjectConfigBrowseObject;

                case ExtendeeObject.ProjectItem:
                    return PrjCATID.prjCATIDProjectItem;

                case ExtendeeObject.FolderBrowseObject:
                    return PrjBrowseObjectCATID.prjCATIDCSharpFolderBrowseObject;

                case ExtendeeObject.ReferenceBrowseObject:
                    return PrjBrowseObjectCATID.prjCATIDCSharpReferenceBrowseObject;

                default:
                case ExtendeeObject.FileBrowseObject:
                    BCLDebug.Assert(extendee == ExtendeeObject.FileBrowseObject);
                    return PrjBrowseObjectCATID.prjCATIDCSharpFileBrowseObject;
            }
        }
    }
}

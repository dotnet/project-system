// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
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
            switch (extendee)
            {
                case ExtendeeObject.Project:
                    return PrjCATID.prjCATIDProject;

                case ExtendeeObject.ProjectBrowseObject:
                    return PrjBrowseObjectCATID.prjCATIDVBProjectBrowseObject;

                case ExtendeeObject.Configuration:
                    return PrjBrowseObjectCATID.prjCATIDVBConfig;

                case ExtendeeObject.ConfigurationBrowseObject:
                    return PrjBrowseObjectCATID.prjCATIDVBProjectConfigBrowseObject;

                case ExtendeeObject.ProjectItem:
                    return PrjCATID.prjCATIDProjectItem;

                case ExtendeeObject.FolderBrowseObject:
                    return PrjBrowseObjectCATID.prjCATIDVBFolderBrowseObject;

                case ExtendeeObject.ReferenceBrowseObject:
                    return PrjBrowseObjectCATID.prjCATIDVBReferenceBrowseObject;

                default:
                case ExtendeeObject.FileBrowseObject:
                    BCLDebug.Assert(extendee == ExtendeeObject.FileBrowseObject);
                    return PrjBrowseObjectCATID.prjCATIDVBFileBrowseObject;
            }
        }
    }
}

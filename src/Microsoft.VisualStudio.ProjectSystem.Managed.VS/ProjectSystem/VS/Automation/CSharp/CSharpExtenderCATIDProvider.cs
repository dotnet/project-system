// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

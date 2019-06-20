// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

namespace Microsoft.VisualStudio.ProjectSystem.VS.WindowsForms
{
    internal partial class WindowsFormEditorProvider
    {
        private class SubTypeDescriptor
        { 
            public readonly string SubType;

            public readonly string DisplayName;

            public readonly string DesignerCategoryForPersistence;

            public readonly bool UseDesignerByDefault;

            public SubTypeDescriptor(string subType, string displayName, string designerCategoryForPersistence, bool useDesignerByDefault)
            {
                SubType = subType;
                DisplayName = displayName;
                DesignerCategoryForPersistence = designerCategoryForPersistence;
                UseDesignerByDefault = useDesignerByDefault;
            }
        }
    }
}

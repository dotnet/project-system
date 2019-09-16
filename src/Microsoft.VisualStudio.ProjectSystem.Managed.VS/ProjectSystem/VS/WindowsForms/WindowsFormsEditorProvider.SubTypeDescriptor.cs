// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.WindowsForms
{
    internal partial class WindowsFormsEditorProvider
    {
        private class SubTypeDescriptor
        {
            public readonly string SubType;

            public readonly string DisplayName;

            public readonly bool UseDesignerByDefault;

            public SubTypeDescriptor(string subType, string displayName, bool useDesignerByDefault)
            {
                SubType = subType;
                DisplayName = displayName;
                UseDesignerByDefault = useDesignerByDefault;
            }
        }
    }
}

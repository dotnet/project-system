// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

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

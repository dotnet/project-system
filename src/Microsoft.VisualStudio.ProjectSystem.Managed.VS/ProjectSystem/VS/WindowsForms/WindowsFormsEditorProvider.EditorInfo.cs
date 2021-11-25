// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.WindowsForms
{
    internal partial class WindowsFormsEditorProvider
    {
        private class EditorInfo : IProjectSpecificEditorInfo
        {
            public EditorInfo(Guid editor, string displayName, bool isDefaultEditor)
            {
                Requires.NotEmpty(editor, nameof(editor));
                Requires.NotNullOrEmpty(displayName, nameof(displayName));

                EditorFactory = editor;
                DisplayName = displayName;
                IsDefaultEditor = isDefaultEditor;
            }

            public Guid EditorFactory { get; }

            public bool IsDefaultEditor { get; }

            public string DisplayName { get; }

            public Guid DefaultView => VSConstants.LOGVIEWID.Designer_guid;
        }
    }
}

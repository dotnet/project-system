// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

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

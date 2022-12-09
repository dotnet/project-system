// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    internal readonly struct ContextState
    {
        public ContextState(bool isActiveEditorContext, bool isActiveConfiguration)
        {
            IsActiveEditorContext = isActiveEditorContext;
            IsActiveConfiguration = isActiveConfiguration;
        }

        /// <summary>
        ///     Gets a value indicating whether the related language service project serves as the active "context"
        ///     for the editor.
        /// </summary>
        /// <remarks>
        ///     The "active" context for the editor is the one that Roslyn uses to drive IntelliSense, refactorings
        ///     and code fixes. This is typically controlled by the user via the project drop down in the top-left
        ///     of the editor, but can be changed in reaction to other factors.
        /// </remarks>
        public bool IsActiveEditorContext { get; }

        /// <summary>
        ///     Gets a value indicating whether the related language service project is context in the active
        ///     configuration for a project.
        /// </summary>
        /// <remarks>
        ///     The context in the active configuration for the project is the one that Roslyn uses to analyze types 
        ///     to push "SubType"  metadata of source files to the project system. This avoids having conflicting 
        ///     metadata between contexts of the same source file in multi-targeted projects. 
        /// </remarks>
        public bool IsActiveConfiguration { get; }
    }
}

// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.VS.UI
{
    /// <summary>
    ///     Provides methods for opening the Add New Item or Add Existing Items dialogs.
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.UnconfiguredProject, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
    internal interface IAddItemDialogService
    {
        /// <summary>
        ///     Returns a value indicating whether the specified node can have new or existing items added to it.
        /// </summary>
        bool CanAddNewOrExistingItemTo(IProjectTree node);

        /// <summary>
        ///     Shows the "Add New Item" dialog for the specified node.
        /// </summary>
        /// <param name="node">
        ///     The <see cref="IProjectTree"/> that the new item will be added to.
        /// </param>
        /// <returns>
        ///     <see langword="true"/> if the user selected an item and clicked OK, otherwise, <see langword="false"/> if the cancelled the dialog.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///     <paramref name="node"/> is marked with <see cref="ProjectTreeFlags.Common.DisableAddItemFolder"/> or <see cref="ProjectTreeFlags.Common.DisableAddItemRecursiveFolder"/>.
        /// </exception>
        Task<bool> ShowAddNewItemDialogAsync(IProjectTree node);

        /// <summary>
        ///     Shows the "Add New Item" dialog for the specified node and selecting the specified template.
        /// </summary>
        /// <param name="node">
        ///     The <see cref="IProjectTree"/> that the new item will be added to.
        /// </param>
        /// <param name="localizedDirectoryName">
        ///     The localized name of the directory that contains the template to select.
        /// </param>
        /// <param name="localizedTemplateName">
        ///     The localized name of the template to select.
        /// </param>
        /// <returns>
        ///     <see langword="true"/> if the user selected an item and clicked OK, otherwise, <see langword="false"/> if the cancelled the dialog.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="localizedDirectoryName"/> is <see langword="null"/>.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <paramref name="localizedTemplateName"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     <paramref name="localizedDirectoryName"/> is an empty string ("").
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <paramref name="localizedTemplateName"/> is an empty string ("").
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <paramref name="node"/> is marked with <see cref="ProjectTreeFlags.Common.DisableAddItemFolder"/> or <see cref="ProjectTreeFlags.Common.DisableAddItemRecursiveFolder"/>.
        /// </exception>
        Task<bool> ShowAddNewItemDialogAsync(IProjectTree node, string localizedDirectoryName, string localizedTemplateName);

        /// <summary>
        ///     Shows the "Add Existing Items" dialog for the specified node.
        /// </summary>
        /// <param name="node">
        ///     The <see cref="IProjectTree"/> that the existing items will be added to.
        /// </param>
        /// <returns>
        ///     <see langword="true"/> if the user selected an item and clicked OK, otherwise, <see langword="false"/> if the cancelled the dialog.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///     <paramref name="node"/> is marked with <see cref="ProjectTreeFlags.Common.DisableAddItemFolder"/> or <see cref="ProjectTreeFlags.Common.DisableAddItemRecursiveFolder"/>.
        /// </exception>
        Task<bool> ShowAddExistingItemsDialogAsync(IProjectTree node);
    }
}

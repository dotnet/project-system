namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices.Handlers.Rename
{
    /// <summary>
    /// General abstraction of type that needs to be notified when a file rename occurs in the project system
    /// </summary>
    internal interface IFileRenameHandler
    {
        /// <summary>
        /// Notifies the handler that the given file has beeen renamed from oldFilePath to newFilePath.
        /// </summary>
        /// <param name="oldFilePath">The original path and name of the file</param>
        /// <param name="newFilePath">The new path and name of the file</param>
        void HandleRename(string oldFilePath, string newFilePath);
    }
}

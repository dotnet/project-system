namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    /// <summary>
    ///     Provides an implementation of <see cref="ILaunchProfile"/> with an
    ///     additional property <see cref="EnableUnmanagedDebugging"/> for controlling
    ///     mixed mode debugging.
    /// </summary>
    public interface ILaunchProfile2 : ILaunchProfile
    {
        /// <summary>
        /// Enable or disable unmanaged (mixed mode) debugging
        /// </summary>
        bool EnableUnmanagedDebugging { get; }
    }
}

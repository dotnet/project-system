using System.Collections.Immutable;

using Microsoft.VisualStudio.LanguageServices.ProjectSystem;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    /// <summary>
    ///     Provides <see cref="ICommandLineHandler"/> and <see cref="IProjectEvaluationHandler"/> instances for 
    ///     <see cref="IWorkspaceProjectContext"/> instances.
    /// </summary>
    internal interface IContextHandlerProvider
    {
        /// <summary>
        ///     Gets the evaluation rules for all <see cref="IProjectEvaluationHandler"/> instances.
        /// </summary>
        ImmutableArray<string> EvaluationRuleNames { get; }

        /// <summary>
        ///     Returns the array of <see cref="ICommandLineHandler"/> instances for the specified <see cref="IWorkspaceProjectContext"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="context"/> is <see langword="null"/>.
        /// </exception>
        ImmutableArray<ICommandLineHandler> GetCommandLineHandlers(IWorkspaceProjectContext context);

        /// <summary>
        ///     Returns the array of <see cref="IProjectEvaluationHandler"/> instances and their evaluation rule names
        ///     for the specified <see cref="IWorkspaceProjectContext"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="context"/> is <see langword="null"/>.
        /// </exception>
        ImmutableArray<(IProjectEvaluationHandler handler, string evaluationRuleName)> GetEvaluationHandlers(IWorkspaceProjectContext context);

        /// <summary>
        ///     Releases the handlers for the specified <see cref="IWorkspaceProjectContext"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="context"/> is <see langword="null"/>.
        /// </exception>
        void ReleaseHandlers(IWorkspaceProjectContext context);
    }
}

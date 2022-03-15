// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

#pragma warning disable RS0030 // Do not used banned APIs (wraps banned APIs)

using System.Threading.Tasks.Dataflow;

namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    ///     Produces <see cref="IDataflowBlock"/> instances.
    /// </summary>
    internal static class DataflowBlockFactory
    {
        /// <summary>
        ///     Provides a dataflow block that invokes a provided <see cref="Action{T}"/> delegate for every data element received.
        /// </summary>
        public static ITargetBlock<TInput> CreateActionBlock<TInput>(Action<TInput> target, UnconfiguredProject project, ProjectFaultSeverity severity = ProjectFaultSeverity.Recoverable, string? nameFormat = null, bool skipIntermediateInputData = false)
        {
            Requires.NotNull(target, nameof(target));
            Requires.NotNull(project, nameof(project));

            ITargetBlock<TInput> block = DataflowBlockSlim.CreateActionBlock(target, nameFormat, skipIntermediateInputData: skipIntermediateInputData);

            RegisterFaultHandler(block, project, severity);

            return block;
        }

        /// <summary>
        ///     Provides a dataflow block that invokes a provided <see cref="Func{T, TResult}"/> delegate for every data element received.
        /// </summary>
        public static ITargetBlock<TInput> CreateActionBlock<TInput>(Func<TInput, Task> target, UnconfiguredProject project, ProjectFaultSeverity severity = ProjectFaultSeverity.Recoverable, string? nameFormat = null, bool skipIntermediateInputData = false)
        {
            Requires.NotNull(target, nameof(target));
            Requires.NotNull(project, nameof(project));

            ITargetBlock<TInput> block = DataflowBlockSlim.CreateActionBlock(target, nameFormat, skipIntermediateInputData: skipIntermediateInputData);

            RegisterFaultHandler(block, project, severity);

            return block;
        }

        private static void RegisterFaultHandler(IDataflowBlock block, UnconfiguredProject project, ProjectFaultSeverity severity)
        {
            IProjectFaultHandlerService faultHandlerService = project.Services.FaultHandler;

            Task faultTask = faultHandlerService.RegisterFaultHandlerAsync(block, project, severity);

            // We don't actually care about the result of reporting the fault if one occurs
            faultHandlerService.Forget(faultTask, project);
        }
    }
}

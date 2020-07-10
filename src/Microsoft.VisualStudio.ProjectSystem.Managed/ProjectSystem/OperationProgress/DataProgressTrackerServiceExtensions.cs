// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.OperationProgress
{
    /// <summary>
    ///     Provides extension methods for <see cref="IDataProgressTrackerService"/>.
    /// </summary>
    internal static class DataProgressTrackerServiceExtensions
    {
        /// <summary>
        ///    Registers an output to be monitored for the "IntelliSense" stage. The returned registration needs to be notified when new data is calculated.
        /// </summary>
        public static IDataProgressTrackerServiceRegistration RegisterForIntelliSense(this IDataProgressTrackerService service, object owner, ConfiguredProject project, string name)
        {
            // We deliberately do not want these individual operations in a stage (such as pushing evaluation
            // results to Roslyn) to be visible to the user, so we avoiding specifying a display message.
            var dataSource = new ProgressTrackerOutputDataSource(owner, project, operationProgressStageId: OperationProgressStageId.IntelliSense, name, displayMessage: "");

            return service.RegisterOutputDataSource(dataSource);
        }
    }
}

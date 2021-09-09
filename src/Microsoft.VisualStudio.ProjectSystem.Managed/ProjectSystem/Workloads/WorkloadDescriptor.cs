// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Workloads
{
    /// <summary>
    ///     Represents a mapping from a .NET workload to a Visual Studio component.
    /// </summary>
    internal readonly struct WorkloadDescriptor
    {
        /// <summary>
        /// An empty workload descriptor is used to indicate an unknown workload
        /// e.g. when a design-time build fails or when no additional workloads
        /// are required by a project.
        /// </summary>
        internal static readonly WorkloadDescriptor Empty = new(string.Empty, string.Empty);

        public WorkloadDescriptor(string workloadName, string visualStudioComponentId)
        {
            WorkloadName = workloadName;
            VisualStudioComponentId = visualStudioComponentId;
        }

        /// <summary>
        ///     Gets the name of the .NET workload.
        /// </summary>
        public string WorkloadName { get; }

        /// <summary>
        ///     Gets the Visual Studio setup component ID corresponding to the .NET workload.
        /// </summary>
        public string VisualStudioComponentId { get; }

        public bool Equals(WorkloadDescriptor other)
        {
            return StringComparers.WorkloadNames.Equals(WorkloadName, other.WorkloadName)
                && StringComparers.VisualStudioSetupComponentIds.Equals(VisualStudioComponentId, other.VisualStudioComponentId);
        }

        public override bool Equals(object obj)
        {
            if (obj is WorkloadDescriptor workloadDescriptor)
            {
                return Equals(workloadDescriptor);
            }

            return false;
        }

        public override int GetHashCode()
        {
            int hashCode = StringComparers.WorkloadNames.GetHashCode(WorkloadName) * -1521134295;
            hashCode += StringComparers.VisualStudioSetupComponentIds.GetHashCode(VisualStudioComponentId);
            return hashCode;
        }
    }
}

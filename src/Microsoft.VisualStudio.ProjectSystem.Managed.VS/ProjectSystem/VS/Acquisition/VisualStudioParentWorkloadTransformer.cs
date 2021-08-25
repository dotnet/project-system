// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Acquisition
{
    /// <summary>
    ///     Transforms a collection of Visual Studio component IDs.
    ///     Installing only the MAUI .NET workloads may not be sufficient for a productive
    ///     design-time experience. This class adds the .NET cross platform development
    ///     workload to the list of components to be installed whenever a .NET MAUI workload
    ///     is in the collection of components to be installed.
    /// </summary>
    [Export(typeof(IVisualStudioComponentIdTransformer))]
    internal class VisualStudioParentWorkloadTransformer : IVisualStudioComponentIdTransformer
    {
        private const string NetCrossPlatVisualStudioWorkloadName = "Microsoft.VisualStudio.Workload.NetCrossPlat";

        private const string MauiAndroidWorkloadName = "maui.android";
        private const string MauiIOSWorkloadName = "maui.ios";
        private const string MauiMacCatalystWorkloadName = "maui.maccatalyst";
        private const string MauiWindowsWorkloadName = "maui.windows";

        private static readonly HashSet<string> s_mauiWorkloads = new(StringComparers.WorkloadNames)
        {
            { MauiAndroidWorkloadName },
            { MauiIOSWorkloadName },
            { MauiMacCatalystWorkloadName },
            { MauiWindowsWorkloadName },
        };

        public Task<IReadOnlyCollection<string>> TransformVisualStudioComponentIdsAsync(IEnumerable<string> vsComponentIds)
        {
            HashSet<string> finalVsComponentIdSet = new(StringComparers.VisualStudioSetupComponentIds);

            foreach (string vsComponentId in  vsComponentIds)
            {
                finalVsComponentIdSet.Add(vsComponentId);

                if (s_mauiWorkloads.Contains(vsComponentId))
                {
                    finalVsComponentIdSet.Add(NetCrossPlatVisualStudioWorkloadName);
                }
            }

            return Task.FromResult((IReadOnlyCollection<string>)finalVsComponentIdSet);
        }
    }
}

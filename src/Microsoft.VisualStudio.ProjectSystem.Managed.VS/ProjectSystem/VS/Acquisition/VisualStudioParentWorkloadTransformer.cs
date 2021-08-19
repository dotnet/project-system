// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Acquisition
{
    /// <summary>
    ///     Transforms a collection of Visual Studio component IDs.
    /// </summary>
    [Export(typeof(IVisualStudioComponentIdTransformer))]
    internal class VisualStudioParentWorkloadTransformer : IVisualStudioComponentIdTransformer
    {
        private const string MauiAndroidWorkloadName = "maui.android";
        private const string MauiIOSWorkloadName = "maui.ios";
        private const string MauiMacCatalystWorkloadName = "maui.maccatalyst";

        private static readonly string[] s_mauiAndroidComponents = new string[] {
            "maui.android",
            "android.aot",
            "Component.OpenJDK",
            "Component.Android.SDK30",
            "Microsoft.VisualStudio.Android.SdkManager",
            "Microsoft.VisualStudio.Android.DeviceManager",
            "Microsoft.VisualStudio.Component.MonoDebugger",
            "Microsoft.VisualStudio.Component.Merq",
            "Xamarin.VisualStudio",
            "Xamarin.VisualStudio.Android.Deploy",
            "Xamarin.VisualStudio.Android.Designer",
            "Xamarin.VisualStudio.Designer",
            "Xamarin.VisualStudio.Forms.Editor",
            "Xamarin.VisualStudio.Onboarding",
        };

        private static readonly string[] s_mauiIOSComponents = new string[] {
            "maui.ios",
            "ios",
            "Component.Xamarin.RemotedSimulator",
            "Microsoft.VisualStudio.Component.MonoDebugger",
            "Microsoft.VisualStudio.Component.Merq",
            "Xamarin.VisualStudio",
            "Xamarin.VisualStudio.Designer",
            "Xamarin.VisualStudio.Forms.Editor",
            "Xamarin.VisualStudio.Onboarding",
        };

        private static readonly string[] s_mauiMacCatalystComponents = new string[] {
            "maui.maccatalyst",
            "maccatalyst",
            "Microsoft.VisualStudio.Component.MonoDebugger",
            "Microsoft.VisualStudio.Component.Merq",
            "Xamarin.VisualStudio",
            "Xamarin.VisualStudio.Designer",
            "Xamarin.VisualStudio.Forms.Editor",
            "Xamarin.VisualStudio.Onboarding",
        };

        private static readonly Dictionary<string, IReadOnlyCollection<string>> s_vsComponentIdToParentComponentsMap = new(StringComparers.VisualStudioSetupComponentIdComparer)
        {
            { MauiAndroidWorkloadName, s_mauiAndroidComponents },
            { MauiIOSWorkloadName, s_mauiIOSComponents },
            { MauiMacCatalystWorkloadName, s_mauiMacCatalystComponents }
        };

        public Task<IReadOnlyCollection<string>> TransformVisualStudioComponentIdsAsync(IReadOnlyCollection<string> vsComponentIds)
        {
            HashSet<string> finalVsComponentIdSet = new(StringComparers.VisualStudioSetupComponentIdComparer);

            foreach (string vsComponentId in  vsComponentIds)
            {
                finalVsComponentIdSet.Add(vsComponentId);

                if (s_vsComponentIdToParentComponentsMap.TryGetValue(vsComponentId, out IReadOnlyCollection<string> mappedVsComponentIds))
                {
                    finalVsComponentIdSet.AddRange(mappedVsComponentIds);
                }
            }

            return Task.FromResult((IReadOnlyCollection<string>)finalVsComponentIdSet);
        }
    }
}

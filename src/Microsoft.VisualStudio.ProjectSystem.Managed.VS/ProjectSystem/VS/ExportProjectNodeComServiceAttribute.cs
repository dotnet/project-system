// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    /// <summary>
    ///     Exports a service to be aggregated with the project node.
    /// </summary>
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Interface, AllowMultiple = true)]
    internal class ExportProjectNodeComServiceAttribute : ExportAttribute
    {
        public ExportProjectNodeComServiceAttribute(params Type[] comTypes)
        : base(ExportContractNames.VsTypes.ProjectNodeComExtension)
        {
            Iid = GetIids(comTypes);
        }

        public string[] Iid { get; }

        public static bool IncludeInherited
        {
            get => false;
        }

        private static string[] GetIids(Type[] comTypes)
        {
            Requires.NotNull(comTypes, nameof(comTypes));

            // Reuse ComServiceIdAttribute's logic for calculating IIDs.
            return comTypes.Select(type => new ComServiceIidAttribute(type))
                           .SelectMany(attribute => attribute.Iid)
                           .ToArray();
        }
    }
}

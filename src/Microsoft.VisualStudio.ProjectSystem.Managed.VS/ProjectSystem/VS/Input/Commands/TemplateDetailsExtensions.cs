// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using static Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands.AbstractAddItemCommandHandler;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands
{
    internal static class TemplateDetailsExtensions
    {
        /// <summary>
        ///  Creates a TemplateDetails record where both resources come from a single package
        /// </summary>
        public static ImmutableDictionary<long, ImmutableArray<TemplateDetails>> CreateTemplateDetails(this ImmutableDictionary<long, ImmutableArray<TemplateDetails>> map, long commandId, string capability, Guid resourcePackage, Enum dirNameId, Enum templateNameId)
        {
            return CreateTemplateDetails(map, commandId, capability, resourcePackage, dirNameId, resourcePackage, templateNameId);
        }

        /// <summary>
        ///  Creates a TemplateDetails record where both resources come from a single package, and combines the two capabilities with &amp; before checking.
        /// </summary>
        public static ImmutableDictionary<long, ImmutableArray<TemplateDetails>> CreateTemplateDetails(this ImmutableDictionary<long, ImmutableArray<TemplateDetails>> map, long commandId, string capability, string extraCapability, Guid resourcePackage, Enum dirNameId, Enum templateNameId)
        {
            return CreateTemplateDetails(map, commandId, capability + " & " + extraCapability, resourcePackage, dirNameId, resourcePackage, templateNameId);
        }

        /// <summary>
        ///  Creates a TemplateDetails record by combining the two capabilities with &amp; before checking.
        /// </summary>
        public static ImmutableDictionary<long, ImmutableArray<TemplateDetails>> CreateTemplateDetails(this ImmutableDictionary<long, ImmutableArray<TemplateDetails>> map, long commandId, string capability, string extraCapability, Guid dirNamePackage, Enum dirNameId, Guid templateNamePackage, Enum templateNameId)
        {
            return CreateTemplateDetails(map, commandId, capability + " & " + extraCapability, dirNamePackage, dirNameId, templateNamePackage, templateNameId);
        }

        /// <summary>
        ///  Creates a new TemplateDetails record
        /// </summary>
        public static ImmutableDictionary<long, ImmutableArray<TemplateDetails>> CreateTemplateDetails(this ImmutableDictionary<long, ImmutableArray<TemplateDetails>> map, long commandId, string capability, Guid dirNamePackage, Enum dirNameId, Guid templateNamePackage, Enum templateNameId)
        {
            if (!map.TryGetValue(commandId, out ImmutableArray<TemplateDetails> list))
            {
                list = ImmutableArray<TemplateDetails>.Empty;
            }

            list = list.Add(new TemplateDetails(capability, dirNamePackage, Convert.ToUInt32(dirNameId), templateNamePackage, Convert.ToUInt32(templateNameId)));

            return map.SetItem(commandId, list);
        }
    }
}

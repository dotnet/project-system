// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using static Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands.AbstractAddItemCommandHandler;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands
{
    internal static class TemplateDetailsExtensions
    {
        public static void Add(this Dictionary<long, List<TemplateDetails>> map, long commandId, string capability, Guid resourcePackage, Enum dirNameId, Enum templateNameId)
        {
            Add(map, commandId, capability, resourcePackage, dirNameId, resourcePackage, templateNameId);
        }

        public static void Add(this Dictionary<long, List<TemplateDetails>> map, long commandId, string capability, string extraCapability, Guid resourcePackage, Enum dirNameId, Enum templateNameId)
        {
            Add(map, commandId, capability + " & " + extraCapability, resourcePackage, dirNameId, resourcePackage, templateNameId);
        }

        public static void Add(this Dictionary<long, List<TemplateDetails>> map, long commandId, string capability, string extraCapability, Guid dirNamePackage, Enum dirNameId, Guid templateNamePackage, Enum templateNameId)
        {
            Add(map, commandId, capability + " & " + extraCapability, dirNamePackage, dirNameId, templateNamePackage, templateNameId);
        }

        public static void Add(this Dictionary<long, List<TemplateDetails>> map, long commandId, string capability,Guid dirNamePackage, Enum dirNameId, Guid templateNamePackage, Enum templateNameId)
        {
            if (!map.ContainsKey(commandId))
            {
                map.Add(commandId, new List<TemplateDetails>());
            }
            map[commandId].Add(new TemplateDetails(capability, dirNamePackage, Convert.ToUInt32(dirNameId), templateNamePackage, Convert.ToUInt32(templateNameId)));
        }
    }
}

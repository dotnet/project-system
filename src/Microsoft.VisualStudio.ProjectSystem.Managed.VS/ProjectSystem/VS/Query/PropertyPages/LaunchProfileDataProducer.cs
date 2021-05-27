// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.Frameworks;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel.Implementation;
using Microsoft.VisualStudio.ProjectSystem.Query.QueryExecution;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    internal static class LaunchProfileDataProducer
    {
        public static IEntityValue CreateLaunchProfileValue(IQueryExecutionContext queryExecutionContext, EntityIdentity id, QueryProjectPropertiesContext propertiesContext, Rule rule, int order, IProjectState cache, ILaunchProfilePropertiesAvailableStatus properties)
        {
            LaunchProfileValue newLaunchProfile = new(queryExecutionContext.EntityRuntime, id, new LaunchProfilePropertiesAvailableStatus());

            if (properties.Name)
            {
                newLaunchProfile.Name = propertiesContext.ItemName;
            }

            if (properties.CommandName)
            {
                if (rule.Metadata.TryGetValue("CommandName", out object? commandNameObj)
                    && commandNameObj is string commandName)
                {
                    newLaunchProfile.CommandName = commandName;
                }
            }

            if (properties.Order)
            {
                newLaunchProfile.Order = order;
            }

            ((IEntityValueFromProvider)newLaunchProfile).ProviderState = new ContextAndRuleProviderState(cache, propertiesContext, rule);

            return newLaunchProfile;
        }

        public static EntityIdentity CreateLaunchProfileId(IEntityValue parent, string itemType, string itemName)
        {
            return new EntityIdentity(
                ((IEntityWithId)parent).Id,
                new Dictionary<string, string>
                {
                    { ProjectModelIdentityKeys.SourceItemType, itemType },
                    { ProjectModelIdentityKeys.SourceItemName, itemName }
                });
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

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
        public static IEntityValue CreateLaunchProfileValue(IQueryExecutionContext executionContext, EntityIdentity id, QueryProjectPropertiesContext context, Rule rule, int order, IPropertyPageQueryCache cache, ILaunchProfilePropertiesAvailableStatus properties)
        {
            LaunchProfileValue newLaunchProfile = new(executionContext.EntityRuntime, id, new LaunchProfilePropertiesAvailableStatus());

            if (properties.Name)
            {
                newLaunchProfile.Name = context.ItemName;
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

            ((IEntityValueFromProvider)newLaunchProfile).ProviderState = new ContextAndRuleProviderState(cache, context, rule);

            return newLaunchProfile;
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.Frameworks;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel.Implementation;
using Microsoft.VisualStudio.ProjectSystem.Query.QueryExecution;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    /// <summary>
    /// Handles the creation of <see cref="IPropertyPage"/> instances and populating the requested members.
    /// </summary>
    internal abstract class PropertyPageDataProducer : QueryDataProducerBase<IEntityValue>
    {
        protected PropertyPageDataProducer(IPropertyPagePropertiesAvailableStatus properties)
        {
            Requires.NotNull(properties, nameof(properties));
            Properties = properties;
        }

        protected IPropertyPagePropertiesAvailableStatus Properties { get; }

        protected Task<IEntityValue> CreatePropertyPageValueAsync(IEntityValue entity, PropertyPageQueryCache context, Rule rule)
        {
            Requires.NotNull(entity, nameof(entity));
            Requires.NotNull(rule, nameof(rule));

            var identity = new EntityIdentity(
                ((IEntityWithId)entity).Id,
                new KeyValuePair<string, string>[]
                {
                        new KeyValuePair<string, string>(ProjectModelIdentityKeys.PropertyPageName, rule.Name)
                });

            return CreatePropertyPageValueAsync(entity.EntityRuntime, identity, context, rule);
        }

        protected Task<IEntityValue> CreatePropertyPageValueAsync(IEntityRuntimeModel runtimeModel, EntityIdentity id, PropertyPageQueryCache context, Rule rule)
        {
            Requires.NotNull(rule, nameof(rule));
            var newPropertyPage = new PropertyPageValue(runtimeModel, id, new PropertyPagePropertiesAvailableStatus());

            if (Properties.Name)
            {
                newPropertyPage.Name = rule.Name;
            }

            if (Properties.DisplayName)
            {
                newPropertyPage.DisplayName = rule.DisplayName;
            }

            if (Properties.Order)
            {
                newPropertyPage.Order = rule.Order;
            }

            if (Properties.Kind)
            {
                newPropertyPage.Kind = rule.PageTemplate;
            }

            ((IEntityValueFromProvider)newPropertyPage).ProviderState = (context, rule);

            return Task.FromResult<IEntityValue>(newPropertyPage);
        }
    }
}

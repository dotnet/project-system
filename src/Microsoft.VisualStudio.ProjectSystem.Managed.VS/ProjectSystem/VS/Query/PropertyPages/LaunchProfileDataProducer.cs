// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.Framework;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    internal static class LaunchProfileDataProducer
    {
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

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IProjectVersionedValueFactory
    {
        public static IProjectVersionedValue<IProjectSubscriptionUpdate> CreateEmpty()
        {
            return FromJson("{}");
        }

        public static IProjectVersionedValue<IProjectSubscriptionUpdate> FromJson(string jsonString)
        {
            return FromJson(version: 1, jsonString: jsonString);
        }

        public static IProjectVersionedValue<IProjectSubscriptionUpdate> FromJson(IComparable version, string jsonString)
        {
            var update = IProjectSubscriptionUpdateFactory.FromJson(jsonString);

            // Every IProjectSubscriptionUpdate contains the version of the configured project
            return IProjectVersionedValueFactory<IProjectSubscriptionUpdate>.Create(update,
                                                                                    identity: ProjectDataSources.ConfiguredProjectVersion,
                                                                                    version: version);
        }
    }
}

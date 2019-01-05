// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;

using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies;

using Moq;

using Newtonsoft.Json.Linq;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal static class IDependencyModelFactory
    {
        public static IDependencyModel Create()
        {
            return Mock.Of<IDependencyModel>();
        }

        public static IDependencyModel Implement(string providerType = null,
                                                 string id = null,
                                                 string originalItemSpec = null,
                                                 string path = null,
                                                 string caption = null,
                                                 IEnumerable<string> dependencyIDs = null,
                                                 bool? resolved = null,
                                                 MockBehavior mockBehavior = MockBehavior.Default)
        {
            var mock = new Mock<IDependencyModel>(mockBehavior);

            if (providerType != null)
            {
                mock.Setup(x => x.ProviderType).Returns(providerType);
            }

            if (id != null)
            {
                mock.Setup(x => x.Id).Returns(id);
            }

            if (originalItemSpec != null)
            {
                mock.Setup(x => x.OriginalItemSpec).Returns(originalItemSpec);
            }

            if (path != null)
            {
                mock.Setup(x => x.Path).Returns(path);
            }

            if (caption != null)
            {
                mock.Setup(x => x.Caption).Returns(caption);
            }

            if (dependencyIDs != null)
            {
                mock.Setup(x => x.DependencyIDs).Returns(ImmutableList<string>.Empty.AddRange(dependencyIDs));
            }

            if (resolved.HasValue)
            {
                mock.Setup(x => x.Resolved).Returns(resolved.Value);
            }

            return mock.Object;
        }

        public static TestDependencyModel FromJson(
            string jsonString,
            ProjectTreeFlags? flags = null,
            ImageMoniker? icon = null,
            ImageMoniker? expandedIcon = null,
            ImageMoniker? unresolvedIcon = null,
            ImageMoniker? unresolvedExpandedIcon = null,
            Dictionary<string, string> properties = null,
            IEnumerable<string> dependenciesIds = null)
        {
            if (string.IsNullOrEmpty(jsonString))
            {
                return null;
            }

            var json = JObject.Parse(jsonString);
            var data = json.ToObject<TestDependencyModel>();

            if (flags.HasValue)
            {
                data.Flags += flags.Value;
            }

            data.Flags += data.Resolved
                ? DependencyTreeFlags.ResolvedFlags
                : DependencyTreeFlags.UnresolvedFlags;

            if (icon.HasValue)
            {
                data.Icon = icon.Value;
            }

            if (expandedIcon.HasValue)
            {
                data.ExpandedIcon = expandedIcon.Value;
            }

            if (unresolvedIcon.HasValue)
            {
                data.UnresolvedIcon = unresolvedIcon.Value;
            }

            if (unresolvedExpandedIcon.HasValue)
            {
                data.UnresolvedExpandedIcon = unresolvedExpandedIcon.Value;
            }

            if (properties != null)
            {
                data.Properties = ImmutableStringDictionary<string>.EmptyOrdinal.AddRange(properties);
            }

            if (dependenciesIds != null)
            {
                data.DependencyIDs = ImmutableList<string>.Empty.AddRange(dependenciesIds);
            }

            return data;
        }
    }
}

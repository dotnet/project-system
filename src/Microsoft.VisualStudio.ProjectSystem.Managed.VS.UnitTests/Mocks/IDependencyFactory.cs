// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;
using Moq;
using Newtonsoft.Json.Linq;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal class IDependencyFactory
    {
        public static IDependency Create()
        {
            return Mock.Of<IDependency>();
        }

        public static Mock<IDependency> Implement(string providerType = null,
                                            string id = null,
                                            string originalItemSpec = null,
                                            string path = null,
                                            string name = null,
                                            string caption = null,
                                            string alias = null,
                                            IEnumerable<string> dependencyIDs = null,
                                            bool? resolved = null,
                                            bool? topLevel = null,
                                            ProjectTreeFlags? flags = null,
                                            string setPropertiesCaption = null,
                                            bool? equals = null,
                                            IImmutableList<string> setPropertiesDependencyIDs = null,
                                            MockBehavior? mockBehavior = null)
        {
            var behavior = mockBehavior ?? MockBehavior.Strict;
            var mock = new Mock<IDependency>(behavior);

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

            if (name != null)
            {
                mock.Setup(x => x.Name).Returns(name);
            }

            if (caption != null)
            {
                mock.Setup(x => x.Caption).Returns(caption);
            }

            if (alias != null)
            {
                mock.Setup(x => x.Alias).Returns(alias);
            }

            if (dependencyIDs != null)
            {
                mock.Setup(x => x.DependencyIDs).Returns(ImmutableList<string>.Empty.AddRange(dependencyIDs));
            }

            if (resolved != null && resolved.HasValue)
            {
                mock.Setup(x => x.Resolved).Returns(resolved.Value);
            }

            if (topLevel != null && topLevel.HasValue)
            {
                mock.Setup(x => x.TopLevel).Returns(topLevel.Value);
            }

            if (flags != null && flags.HasValue)
            {
                mock.Setup(x => x.Flags).Returns(flags.Value);
            }

            if (setPropertiesCaption != null)
            {
                mock.Setup(x => x.SetProperties(setPropertiesCaption, null, null, null)).Returns(mock.Object);
            }

            if (setPropertiesDependencyIDs != null)
            {
                mock.Setup(x => x.SetProperties(null, null, null, setPropertiesDependencyIDs)).Returns(mock.Object);
            }

            if (equals != null && equals.HasValue && equals.Value)
            {
                mock.Setup(t => t.Equals(It.IsAny<IDependency>())).Returns(true);
            }

            return mock;
        }
        
        public static IDependency FromJson(
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
            var data = json.ToObject<TestDependency>();

            if (flags != null && flags.HasValue)
            {
                data.Flags = data.Flags.Union(flags.Value);
            }

            if (icon != null && icon.HasValue)
            {
                data.Icon = icon.Value;
            }

            if (expandedIcon != null && expandedIcon.HasValue)
            {
                data.ExpandedIcon = expandedIcon.Value;
            }

            if (unresolvedIcon != null && unresolvedIcon.HasValue)
            {
                data.UnresolvedIcon = unresolvedIcon.Value;
            }

            if (unresolvedExpandedIcon != null && unresolvedExpandedIcon.HasValue)
            {
                data.UnresolvedExpandedIcon = unresolvedExpandedIcon.Value;
            }

            if (properties != null)
            {
                data.Properties = ImmutableDictionary<string, string>.Empty.AddRange(properties);
            }

            if (dependenciesIds != null)
            {
                data.DependencyIDs = ImmutableList<string>.Empty.AddRange(dependenciesIds);
            }

            return data;
        }

        private class TestDependency : IDependency
        {
            public TestDependency()
            {
            }

            public string ProviderType { get; set; }
            public string Name { get; set; }
            public string Caption { get; set; }
            public string OriginalItemSpec { get; set; }
            public string Path { get; set; }
            public string SchemaName { get; set; }
            public string SchemaItemType { get; set; }
            public string Version { get; set; }
            public bool Resolved { get; set; } = false;
            public bool TopLevel { get; set; } = true;
            public bool Implicit { get; set; } = false;
            public bool Visible { get; set; } = true;
            public int Priority { get; set; } = 0;
            public ImageMoniker Icon { get; set; }
            public ImageMoniker ExpandedIcon { get; set; }
            public ImageMoniker UnresolvedIcon { get; set; }
            public ImageMoniker UnresolvedExpandedIcon { get; set; }
            public IImmutableDictionary<string, string> Properties { get; set; }
            public IImmutableList<string> DependencyIDs { get; set; } = ImmutableList<string>.Empty;
            public ProjectTreeFlags Flags { get; set; } = ProjectTreeFlags.Empty;
            public string Id { get; set; }
            public bool HasUnresolvedDependency { get; set; }
            public string Alias { get; set; }
            public IEnumerable<IDependency> Dependencies { get; set; }
            public ITargetedDependenciesSnapshot Snapshot { get; set; }

            public IDependency SetProperties(
                string caption = null,
                bool? resolved = null,
                ProjectTreeFlags? flags = null,
                IImmutableList<string> dependencyIDs = null)
            {
                return this;
            }

            public override int GetHashCode()
            {
                return unchecked(Id.ToLowerInvariant().GetHashCode());
            }

            public override bool Equals(object obj)
            {
                if (obj is IDependency other)
                {
                    return Equals(other);
                }

                return false;
            }

            public bool Equals(IDependency other)
            {
                if (other != null && other.Id.Equals(Id, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                return false;
            }

            public int CompareTo(IDependency other)
            {
                if (other == null)
                {
                    return 1;
                }

                return StringComparer.OrdinalIgnoreCase.Compare(Id, other.Id);
            }
        }
    }
}
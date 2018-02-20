// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

using Moq;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal static class ProjectConfigurationFactory
    {
        public static ProjectConfiguration Create()
        {
            return Mock.Of<ProjectConfiguration>();
        }

        public static ProjectConfiguration Implement(
                        string name = null,
                        IDictionary<string, string> dimensions = null,
                        MockBehavior mockBehavior = MockBehavior.Default)
        {
            var mock = new Mock<ProjectConfiguration>(mockBehavior);

            if (name != null)
            {
                mock.Setup(x => x.Name).Returns(name);
            }

            if (dimensions != null)
            {
                mock.Setup(x => x.Dimensions).Returns(dimensions.ToImmutableDictionary());
            }

            return mock.Object;
        }

        public static ProjectConfiguration FromJson(string jsonString)
        {
            var model = new ProjectConfigurationModel();
            return model.FromJson(jsonString);
        }
    }

    internal class ProjectConfigurationModel : JsonModel<ProjectConfiguration>
    {
        public IImmutableDictionary<string, string> Dimensions { get; set; }

        public string Name { get; set; }

        public override ProjectConfiguration ToActualModel()
        {
            return new ActualModel
            {
                Dimensions = Dimensions,
                Name = Name
            };
        }

        private class ActualModel : ProjectConfiguration
        {
            public IImmutableDictionary<string, string> Dimensions { get; set; }

            public string Name { get; set; }

            public bool Equals(ProjectConfiguration other)
            {
                throw new NotImplementedException();
            }
        }
    }

}

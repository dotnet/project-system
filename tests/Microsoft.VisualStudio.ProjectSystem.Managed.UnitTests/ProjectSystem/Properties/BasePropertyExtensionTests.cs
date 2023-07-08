// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Build.Framework.XamlTypes;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    public class BasePropertyExtensionTests
    {
        [Fact]
        public void WhenThePropertyIsNull_GetMetadataValueThrows()
        {
            TestProperty testProperty = null!;

            Assert.Throws<ArgumentNullException>(() => testProperty.GetMetadataValueOrNull(metadataName: "DoesntMatter"));
        }

        [Fact]
        public void WhenTheMetadataNameIsNull_GetMetadataValueThrows()
        {

            TestProperty testProperty = new TestProperty() { Metadata = new() };
            string metadataName = null!;

            Assert.Throws<ArgumentNullException>(() => testProperty.GetMetadataValueOrNull(metadataName));
        }

        [Fact]
        public void WhenTheMetadataNameIsTheEmptyString_GetMetadataValueThrows()
        {
            TestProperty testProperty = new TestProperty() { Metadata = new() };
            string metadataName = string.Empty;

            Assert.Throws<ArgumentException>(() => testProperty.GetMetadataValueOrNull(metadataName));
        }

        [Fact]
        public void WhenTheRequestedMetadataIsNotFound_GetMetadataValueReturnsNull()
        {
            TestProperty testProperty = new TestProperty() { Metadata = new() };
            string metadataName = "MyMetadata";

            Assert.Null(testProperty.GetMetadataValueOrNull(metadataName));
        }

        [Fact]
        public void WhenTheRequestedMetadataIsFound_GetMetadataValueReturnsTheValue()
        {
            TestProperty testProperty = new TestProperty()
            {
                Metadata = new()
                {
                    new() { Name = "Alpha", Value = "Kangaroo" },
                    new() { Name = "Beta", Value = "Wallaby" }
                }
            };

            string metadataName = "Beta";

            Assert.Equal(expected: "Wallaby", actual: testProperty.GetMetadataValueOrNull(metadataName));
        }

        [Fact]
        public void WhenTheRequestedMetadataIsSpecifiedMoreThanOnce_GetMetadataValueReturnsTheFirstValue()
        {
            TestProperty testProperty = new TestProperty()
            {
                Metadata = new()
                {
                    new() { Name = "Beta", Value = "Wallaby" },
                    new() { Name = "Alpha", Value = "Dingo" },
                    new() { Name = "Alpha", Value = "Kangaroo" }
                }
            };

            string metadataName = "Alpha";

            Assert.Equal(expected: "Dingo", actual: testProperty.GetMetadataValueOrNull(metadataName));
        }

        /// <summary>
        /// <see cref="BaseProperty" /> is abstract, so we need a concrete implementation
        /// even though there is nothing interesting to implement.
        /// </summary>
        private class TestProperty : BaseProperty
        {
        }
    }
}

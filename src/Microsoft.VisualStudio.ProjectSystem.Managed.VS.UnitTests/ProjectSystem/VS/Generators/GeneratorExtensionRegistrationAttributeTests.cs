// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Shell;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Generators
{
    [ProjectSystemTrait]
    public class GeneratorExtensionRegistrationAttributeTests
    {
        private const string testGuid = "DB18C134-E0E3-4065-9079-2D6B00F4E639";
        private const string testGuid2 = "147BD90C-8D59-48C3-BFB4-95AC8F38C5C4";
        private const string testGuid3 = "CEE02EE1-D8DD-4D41-9D38-A23132643BA4";

        [Fact]
        public void GeneratorExtensionRegistrationAttribute_NullAsExtension_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>("extension", () => new GeneratorExtensionRegistrationAttribute(null, "ResXFileCodeGenerator", testGuid));
        }


        [Fact]
        public void GeneratorExtensionRegistrationAttribute_NullAsGenerator_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>("generator", () => new GeneratorExtensionRegistrationAttribute(".resx", null, testGuid));
        }

        [Fact]
        public void GeneratorExtensionRegistrationAttribute_NullAsContextGuid_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>("contextGuid", () => new GeneratorExtensionRegistrationAttribute(".resx", "ResXFileCodeGenerator", null));
        }

        [Theory]
        [InlineData(".resx", "ResXCodeFileGenerator", testGuid)]
        [InlineData(".resx",  "PublicResXCodeFileGenerator", testGuid2)]
        [InlineData(".tt", "TextTemplateFileGenerator", testGuid3)]
        public void GeneratorExtensionRegistrationAttribute_ValidRegistration_CreatesCorrectKeys(string extension, string generator, string contextGuid)
        {
            var numTimesCreateKeyCalled = 0;
            var numTimesSetupValueCalled = 0;
            var createdKey = "";
            var createdSubkey = "";
            object subkeyValue = "";

            var registration = RegistrationContextFactory.CreateInstance(key =>
            {
                numTimesCreateKeyCalled++;
                createdKey = key;
            }, (subkey, value) =>
            {
                numTimesSetupValueCalled++;
                createdSubkey = subkey;
                subkeyValue = value;
            });

            var attr = new GeneratorExtensionRegistrationAttribute(extension, generator, contextGuid);
            attr.Register(registration);

            Assert.Equal(1, numTimesCreateKeyCalled);
            Assert.Equal($"Generators\\{contextGuid}\\{extension}", createdKey);
            Assert.Equal(1, numTimesSetupValueCalled);
            Assert.Equal(string.Empty, createdSubkey);
            Assert.Equal(generator, subkeyValue);
        }
    }
}

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.VisualStudio.Shell;

using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Generators
{
    [Trait("UnitTest", "ProjectSystem")]
    public class RemoteCodeGenerationRegistrationAttributeTests
    {
        private const string testGuid = "DB18C134-E0E3-4065-9079-2D6B00F4E639";
        private const string testGuid2 = "147BD90C-8D59-48C3-BFB4-95AC8F38C5C4";
        private const string testGuid3 = "CEE02EE1-D8DD-4D41-9D38-A23132643BA4";
        private const string testGuid4 = "B8026ED7-DA10-4AFA-BD7B-157AAAEE0719";

        [Fact]
        public void NullAsGeneratorGuid_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>("generatorGuid", () => new RemoteCodeGeneratorRegistrationAttribute(null, "ResXFileCodeGenerator", testGuid));
        }


        [Fact]
        public void NullAsGeneratorClassName_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>("generatorClassName", () => new RemoteCodeGeneratorRegistrationAttribute(testGuid2, null, "ResXFileCodeGenerator", testGuid));
        }

        [Fact]
        public void NullAsGeneratorName_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>("generatorName", () => new RemoteCodeGeneratorRegistrationAttribute(testGuid2, "ResXFileCodeGenerator", null, testGuid));
        }

        [Fact]
        public void NullAsContextGuid_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>("contextGuid", () => new RemoteCodeGeneratorRegistrationAttribute(testGuid, "ResXFileCodeGenerator", null));
        }

        [Fact]
        public void BadGuidAsContextGuid_ThrowsArgument()
        {
            Assert.Throws<ArgumentException>(() => new RemoteCodeGeneratorRegistrationAttribute(".resx", "ResXFileCodeGenerator",
                "Not a guid"));
        }

        [Theory]
        [InlineData(testGuid, "ResXCodeFileGenerator", testGuid3, false, false)]
        [InlineData(testGuid2, "PublicResXCodeFileGenerator", testGuid3, true, false)]
        [InlineData(testGuid4, "TextTemplateCodeFileGenerator", testGuid3, false, true)]
        [InlineData(testGuid4, "TextTemplateCodeFilePreprocessor", testGuid2, true, true)]
        public void ValidRegistration_CreatesCorrectKeys(string generatorGuid, string generatorName, string contextGuid, bool generatesDesignTimeSource, bool generatesSharedDesignTimeSource)
        {
            var numTimesCreateKeyCalled = 0;
            var createdKey = "";
            Guid.TryParse(generatorGuid, out Guid parsedGeneratorGuid);

            var setValueCallCounts = new Dictionary<string, int>();
            var setValueValues = new Dictionary<string, object>();
            var expectedSubkeys = 2 + (generatesDesignTimeSource ? 1 : 0) + (generatesSharedDesignTimeSource ? 1 : 0);

            var registration = RegistrationContextFactory.CreateInstance(key =>
            {
                numTimesCreateKeyCalled++;
                createdKey = key;
            }, (subkey, value) =>
            {
                setValueCallCounts[subkey] = setValueCallCounts.ContainsKey(subkey) ? setValueCallCounts[subkey] + 1 : 1;
                setValueValues[subkey] = value;
            });

            var attr = new RemoteCodeGeneratorRegistrationAttribute(generatorGuid, generatorName, contextGuid)
            {
                GeneratesDesignTimeSource = generatesDesignTimeSource,
                GeneratesSharedDesignTimeSource = generatesSharedDesignTimeSource
            };
            attr.Register(registration);

            Assert.Equal(1, numTimesCreateKeyCalled);
            Assert.Equal($"Generators\\{contextGuid}\\{generatorName}", createdKey);
            Assert.Equal(expectedSubkeys, setValueCallCounts.Count);

            setValueCallCounts.ToList().ForEach(pair =>
            {
                Assert.Equal(1, pair.Value);
            });

            Assert.Equal(generatorName, setValueValues[string.Empty]);
            Assert.Equal(parsedGeneratorGuid.ToString("B"), setValueValues["CLSID"]);
            if (generatesDesignTimeSource)
                Assert.Equal(1, setValueValues["GeneratesDesignTimeSource"]);
            if (generatesSharedDesignTimeSource)
                Assert.Equal(1, setValueValues["GeneratesSharedDesignTimeSource"]);
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using VSLangProj;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties.VisualBasic
{
    public class MapDynamicEnumValuesProviderTests
    {
        [Fact]
        public void MapDynamicEnumValuesProvider_AssertNull()
        {
            Assert.Throws<ArgumentNullException>("valueMap", () =>
            {
                new MapDynamicEnumValuesProvider(null!);
            });
        }

        [Fact]
        public async Task OptionStrictEnumProviderTest()
        {
            var dynamicEnumValuesGenerator = await new OptionStrictEnumProvider().GetProviderAsync(null);
            var values = await dynamicEnumValuesGenerator.GetListedValuesAsync();

            var pageEnumRawValues = new List<Tuple<string, string, bool>>
            {
                Tuple.Create("0", "Off", true),
                Tuple.Create("1", "On", false)
            };
            var pageEnumValues = CreateEnumValueInstances(pageEnumRawValues);

            VerifySameValue(values, pageEnumValues);

            var persistencePageEnumRawValues = new List<Tuple<string, string, bool>>
            {
                Tuple.Create("Off", "", false),
                Tuple.Create("On", "", false)
            };
            var persistencePageEnumValues = CreateEnumValueInstances(persistencePageEnumRawValues);

            var keys = new List<string>() { "0", "1" };
            var persistencePageEnumMap = CreateEnumValueMap(keys, persistencePageEnumValues);

            await VerifySameValueOnQueryAsync(dynamicEnumValuesGenerator, persistencePageEnumMap, checkMapNameOnly: true);
        }

        [Fact]
        public async Task WarningLevelEnumProviderTest()
        {
            var dynamicEnumValuesGenerator = await new WarningLevelEnumProvider().GetProviderAsync(null);
            var values = await dynamicEnumValuesGenerator.GetListedValuesAsync();

            var pageEnumRawValues = new List<Tuple<string, string, bool>>
            {
                Tuple.Create("0", "", false),
                Tuple.Create("1", "", false),
                Tuple.Create("2", "", false),
                Tuple.Create("3", "", false),
                Tuple.Create("4", "", false)
            };
            var pageEnumValues = CreateEnumValueInstances(pageEnumRawValues);

            VerifySameValue(values, pageEnumValues, checkMapNameOnly: true);

            var keys = new List<string>()
                       {
                            nameof(prjWarningLevel.prjWarningLevel0),
                            nameof(prjWarningLevel.prjWarningLevel1),
                            nameof(prjWarningLevel.prjWarningLevel2),
                            nameof(prjWarningLevel.prjWarningLevel3),
                            nameof(prjWarningLevel.prjWarningLevel4),
                       };
            var persistencePageEnumMap = CreateEnumValueMap(keys, pageEnumValues);

            await VerifySameValueOnQueryAsync(dynamicEnumValuesGenerator, persistencePageEnumMap, checkMapNameOnly: true);
        }

        private static void VerifySameValue(IEnumValue? actual, IEnumValue expected, bool checkMapNameOnly = false)
        {
            Assert.NotNull(actual);
            Assert.Equal(expected.Name, actual.Name);

            if (!checkMapNameOnly)
            {
                Assert.Equal(expected.DisplayName, actual.DisplayName);
                Assert.True(actual.IsDefault == expected.IsDefault);
            }
        }

        private static void VerifySameValue(IEnumerable<IEnumValue> actual, IEnumerable<IEnumValue> expected, bool checkMapNameOnly = false)
        {
            var actualAsArray = actual.ToArray();
            var expectedAsArray = expected.ToArray();

            Assert.Equal(actualAsArray.Length, expectedAsArray.Length);

            for (var i = 0; i < actualAsArray.Length; ++i)
            {
                VerifySameValue(actualAsArray[i], expectedAsArray[i], checkMapNameOnly);
            }
        }

        private static async Task VerifySameValueOnQueryAsync(IDynamicEnumValuesGenerator generator, Dictionary<string, IEnumValue> persistencePageEnumMap, bool checkMapNameOnly = false)
        {
            foreach (var key in persistencePageEnumMap.Keys)
            {
                VerifySameValue(await generator.TryCreateEnumValueAsync(key), persistencePageEnumMap[key], checkMapNameOnly);
            }
        }

        private static PageEnumValue CreateEnumValueInstance(string name, string displayName, bool isDefault = false)
        {
            return new PageEnumValue(new EnumValue { Name = name, DisplayName = displayName, IsDefault = isDefault });
        }

        private static IEnumerable<PageEnumValue> CreateEnumValueInstances(List<Tuple<string, string, bool>> pageEnumValues)
        {
            foreach ((string name, string displayName, bool isDefault) in pageEnumValues)
            {
                yield return CreateEnumValueInstance(name, displayName, isDefault);
            }
        }

        private static Dictionary<string, IEnumValue> CreateEnumValueMap(List<string> keys, IEnumerable<PageEnumValue> pageEnumValues)
        {
            int index = 0;
            Dictionary<string, IEnumValue> map = new();

            foreach (var item in pageEnumValues)
            {
                map[keys[index]] = item;
                index++;
            }

            Assert.Equal(index, keys.Count);

            return map;
        }
    }
}

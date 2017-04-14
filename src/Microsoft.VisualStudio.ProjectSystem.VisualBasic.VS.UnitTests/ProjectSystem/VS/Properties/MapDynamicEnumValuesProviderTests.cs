using System;
using System.Linq;
using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Xunit;
using System.Collections.Generic;
using System.Threading.Tasks;
using VSLangProj;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties
{
    [ProjectSystemTrait]
    public class MapDynamicEnumValuesProviderTests
    {
        [Fact]
        public void MapDynamicEnumValuesProvider_AssertNull()
        {
            Assert.Throws<ArgumentNullException>("getValueMap", () =>
            {
                new MapDynamicEnumValuesProvider(null);
            });
        }

        [Fact]
        public async void OptionCompareEnumProviderTest()
        {
            var dynamicEnumValuesGenerator = await new OptionCompareEnumProvider().GetProviderAsync(null);
            var values = await dynamicEnumValuesGenerator.GetListedValuesAsync();

            var pageEnumRawValues = new List<Tuple<string, string, bool>>
            {
                Tuple.Create("Binary", "Binary", true),
                Tuple.Create("Text", "Text", false)
            };
            var pageEnumValues = CreateEnumValueInstances(pageEnumRawValues);

            VerifySameValue(values, pageEnumValues);

            var keys = new List<string>() { "Binary", "Text" };
            var persistencePageEnumMap = CreateEnumValueMap(keys, pageEnumValues);

            await VerifySameValueOnQueryAsync(dynamicEnumValuesGenerator, persistencePageEnumMap);
        }

        [Fact]
        public async void OptionExplicitEnumProviderTest()
        {
            var dynamicEnumValuesGenerator = await new OptionExplicitEnumProvider().GetProviderAsync(null);
            var values = await dynamicEnumValuesGenerator.GetListedValuesAsync();

            var pageEnumRawValues = new List<Tuple<string, string, bool>>
            {
                Tuple.Create("Off", "Off", false),
                Tuple.Create("On", "On", true)
            };
            var pageEnumValues = CreateEnumValueInstances(pageEnumRawValues);

            VerifySameValue(values, pageEnumValues);

            var keys = new List<string>() { "Off", "On" };
            var persistencePageEnumMap = CreateEnumValueMap(keys, pageEnumValues);

            await VerifySameValueOnQueryAsync(dynamicEnumValuesGenerator, persistencePageEnumMap);
        }

        [Fact]
        public async void OptionInferEnumProviderTest()
        {
            var dynamicEnumValuesGenerator = await new OptionInferEnumProvider().GetProviderAsync(null);
            var values = await dynamicEnumValuesGenerator.GetListedValuesAsync();

            var pageEnumRawValues = new List<Tuple<string, string, bool>>
            {
                Tuple.Create("Off", "Off", false),
                Tuple.Create("On", "On", true)
            };
            var pageEnumValues = CreateEnumValueInstances(pageEnumRawValues);

            VerifySameValue(values, pageEnumValues);

            var keys = new List<string>() { "Off", "On" };
            var persistencePageEnumMap = CreateEnumValueMap(keys, pageEnumValues);

            await VerifySameValueOnQueryAsync(dynamicEnumValuesGenerator, persistencePageEnumMap);
        }

        [Fact]
        public async void OptionStrictEnumProviderTest()
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
        public async void WarningLevelEnumProviderTest()
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


        private static void VerifySameValue(IEnumValue actual, IEnumValue expected, bool checkMapNameOnly = false)
        {
            Assert.True(string.Compare(actual.Name, expected.Name) == 0);

            if (!checkMapNameOnly)
            {
                Assert.True(string.Compare(actual.DisplayName, expected.DisplayName) == 0);
                Assert.True(actual.IsDefault == expected.IsDefault);
            }
        }

        private static void VerifySameValue(IEnumerable<IEnumValue> actual, IEnumerable<IEnumValue> expected, bool checkMapNameOnly = false)
        {
            Assert.Equal(actual.Count(), expected.Count());
            for (var i = 0; i < actual.Count(); ++i)
            {
                VerifySameValue(actual.ElementAt(i), expected.ElementAt(i), checkMapNameOnly);
            }
        }

        private async Task VerifySameValueOnQueryAsync(IDynamicEnumValuesGenerator generator, Dictionary<string, IEnumValue> persistencePageEnumMap, bool checkMapNameOnly = false)
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
            foreach (var item in pageEnumValues)
            {
                yield return CreateEnumValueInstance(item.Item1, item.Item2, item.Item3);
            }
        }

        private Dictionary<string, IEnumValue> CreateEnumValueMap(List<string> keys, IEnumerable<PageEnumValue> pageEnumValues)
        {
            Assert.True(keys.Count == pageEnumValues.Count(), "This is a test authoring error");

            var dict = new Dictionary<string, IEnumValue>();
            for (int i = 0; i < keys.Count; i++)
            {
                dict.Add(keys[i], pageEnumValues.ElementAt(i));
            }

            return dict;
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    public class LaunchProfileExtensionsTests
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void IsInMemoryProfile_ILaunchProfile(bool isInMemory)
        {
            var data = new LaunchProfile(
                name: null,
                commandName: null,
                doNotPersist: isInMemory);

            var lp = (ILaunchProfile)data;
            Assert.Equal(isInMemory, lp.IsInMemoryObject());
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void IsInMemoryProfile_IWritableLaunchProfile(bool isInMemory)
        {
            var data = new WritableLaunchProfile()
            {
                DoNotPersist = isInMemory
            };

            var lp = (IWritableLaunchProfile)data;
            Assert.Equal(isInMemory, lp.IsInMemoryObject());
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(true, true)]
        [InlineData(null, false)]
        public void IsNativeDebuggingEnabled_ILaunchProfile(bool? value, bool expected)
        {
            var data = new LaunchProfile(
                name: null,
                commandName: null,
                otherSettings: value == null ? ImmutableArray<(string, object)>.Empty : ImmutableArray.Create<(string, object)>((LaunchProfileExtensions.NativeDebuggingProperty, value.Value)));

            Assert.Equal(expected, data.IsNativeDebuggingEnabled());
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(true, true)]
        [InlineData(null, false)]
        public void IsSqlDebuggingEnabled_ILaunchProfile(bool? value, bool expected)
        {
            var data = new LaunchProfile(
                name: null,
                commandName: null,
                otherSettings: value == null ? ImmutableArray<(string, object)>.Empty : ImmutableArray.Create<(string, object)>((LaunchProfileExtensions.SqlDebuggingProperty, value.Value)));

            Assert.Equal(expected, data.IsSqlDebuggingEnabled());
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(true, true)]
        [InlineData(null, false)]
        public void IsRemoteDebugEnabled_ILaunchProfile(bool? value, bool expected)
        {
            var data = new LaunchProfile(
                name: null,
                commandName: null,
                otherSettings: value == null ? ImmutableArray<(string, object)>.Empty : ImmutableArray.Create<(string, object)>((LaunchProfileExtensions.RemoteDebugEnabledProperty, value.Value)));

            Assert.Equal(expected, data.IsRemoteDebugEnabled());
        }

        [Theory]
        [InlineData("host", "host")]
        [InlineData("", "")]
        [InlineData(null, null)]
        public void RemoteDebugMachine_ILaunchProfile(string? value, string? expected)
        {
            var data = new LaunchProfile(
                name: null,
                commandName: null,
                otherSettings: value is null ? ImmutableArray<(string, object)>.Empty : ImmutableArray.Create<(string, object)>((LaunchProfileExtensions.RemoteDebugMachineProperty, value)));

            Assert.Equal(expected, data.RemoteDebugMachine());
        }

        [Fact]
        public void TryGetSetting_ILaunchProfile()
        {
            var mock = new Mock<ILaunchProfile>();
            mock.SetupGet(lp => lp.OtherSettings).Returns(() => ImmutableDictionary<string, object>.Empty.Add("A", 1));

            Assert.True(mock.Object.TryGetSetting("A", out object? o));
            Assert.Equal(1, o);

            Assert.False(mock.Object.TryGetSetting("B", out o));
            Assert.Null(o);
        }

        [Fact]
        public void TryGetSetting_ILaunchProfile2()
        {
            var mock = new Mock<ILaunchProfile2>();
            mock.SetupGet(lp => lp.OtherSettings).Returns(() => ImmutableArray.Create<(string, object)>(("A", 1)));

            Assert.True(mock.Object.TryGetSetting("A", out object? o));
            Assert.Equal(1, o);

            Assert.False(mock.Object.TryGetSetting("B", out o));
            Assert.Null(o);
        }

        [Fact]
        public void FlattenEnvironmentVariables_ILaunchProfile_Null()
        {
            var mock = new Mock<ILaunchProfile>();
            mock.SetupGet(lp => lp.EnvironmentVariables).Returns(() => null);

            Assert.Empty(mock.Object.FlattenEnvironmentVariables());
        }

        [Fact]
        public void FlattenEnvironmentVariables_ILaunchProfile_Empty()
        {
            var mock = new Mock<ILaunchProfile>();
            mock.SetupGet(lp => lp.EnvironmentVariables).Returns(() => ImmutableDictionary<string, string>.Empty);
            Assert.Empty(mock.Object.FlattenEnvironmentVariables());
        }

        [Fact]
        public void FlattenEnvironmentVariables_ILaunchProfile_WithItems()
        {
            var mock = new Mock<ILaunchProfile>();
            mock.SetupGet(lp => lp.EnvironmentVariables).Returns(() => ImmutableDictionary<string, string>.Empty.Add("A", "1"));

            Assert.Equal(new[] { ("A", "1") }, mock.Object.FlattenEnvironmentVariables());
        }

        [Fact]
        public void FlattenEnvironmentVariables_ILaunchProfile2()
        {
            var mock = new Mock<ILaunchProfile2>();
            mock.SetupGet(lp => lp.EnvironmentVariables).Returns(() => ImmutableArray.Create(("A", "1")));

            Assert.Equal(new[] { ("A", "1") }, mock.Object.FlattenEnvironmentVariables());
        }

        [Fact]
        public void FlattenOtherSettings_ILaunchProfile_Null()
        {
            var mock = new Mock<ILaunchProfile>();
            mock.SetupGet(lp => lp.OtherSettings).Returns(() => null);

            Assert.Empty(mock.Object.FlattenOtherSettings());
        }

        [Fact]
        public void FlattenOtherSettings_ILaunchProfile_Empty()
        {
            var mock = new Mock<ILaunchProfile>();
            mock.SetupGet(lp => lp.OtherSettings).Returns(() => ImmutableDictionary<string, object>.Empty);
            Assert.Empty(mock.Object.FlattenOtherSettings());
        }

        [Fact]
        public void FlattenOtherSettings_ILaunchProfile_WithItems()
        {
            var mock = new Mock<ILaunchProfile>();
            mock.SetupGet(lp => lp.OtherSettings).Returns(() => ImmutableDictionary<string, object>.Empty.Add("A", 1));

            Assert.Equal(new[] { ("A", (object)1) }, mock.Object.FlattenOtherSettings());
        }

        [Fact]
        public void FlattenOtherSettings_ILaunchProfile2()
        {
            var mock = new Mock<ILaunchProfile2>();
            mock.SetupGet(lp => lp.OtherSettings).Returns(() => ImmutableArray.Create(("A", (object)1)));

            Assert.Equal(new[] { ("A", (object)1) }, mock.Object.FlattenOtherSettings());
        }
    }
}

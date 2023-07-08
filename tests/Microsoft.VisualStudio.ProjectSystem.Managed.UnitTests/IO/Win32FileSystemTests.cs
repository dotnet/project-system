// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.IO
{
    public class Win32FileSystemTests
    {
        [Fact]
        public void GetLastFileWriteTimeOrMinValueUtc_WhenPathDoesNotExist_ReturnsDateTimeMinValue()
        {
            var fileSystem = CreateInstance();

            DateTime result = fileSystem.GetLastFileWriteTimeOrMinValueUtc("This path does not exist!");

            Assert.Equal(result, DateTime.MinValue);
        }

        [Fact]
        public void TryGetLastFileWriteTimeUtc_WhenPathDoesNotExist_ReturnsFalse()
        {
            var fileSystem = CreateInstance();

            bool value = fileSystem.TryGetLastFileWriteTimeUtc("This path does not exist!", out DateTime? result);

            Assert.False(value);
            Assert.Null(result);
        }

        private Win32FileSystem CreateInstance()
        {
            return new Win32FileSystem();
        }
    }
}

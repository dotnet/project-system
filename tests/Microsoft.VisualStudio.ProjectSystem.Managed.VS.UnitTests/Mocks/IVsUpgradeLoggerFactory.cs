// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.Shell.Interop
{
    internal static class IVsUpgradeLoggerFactory
    {
        public static IVsUpgradeLogger CreateLogger(IList<LogMessage> messages)
        {
            var mock = new Mock<IVsUpgradeLogger>();

            mock.Setup(pl => pl.LogMessage(It.IsAny<uint>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Callback<uint, string, string, string>((level, project, file, message) => messages.Add(new LogMessage(level, project, file, message)));

            return mock.Object;
        }
    }

    internal class LogMessage
    {
        public uint Level { get; }
        public string Project { get; }
        public string File { get; }
        public string Message { get; }

        public LogMessage(uint level, string project, string file, string message)
        {
            Level = level;
            Project = project;
            File = file;
            Message = message;
        }

        public override bool Equals(object obj)
        {
            if (obj is null)
            {
                return false;
            }

            if (obj is not LogMessage other)
            {
                return false;
            }

            return Level == other.Level && Project.Equals(other.Project) && File.Equals(other.File) && Message.Equals(other.Message);
        }

        public override int GetHashCode()
        {
            return (Level.GetHashCode() * 31) + (Project.GetHashCode() * 3) + (File.GetHashCode() * 7) + (Message.GetHashCode() * 5);
        }
    }
}

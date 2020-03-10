// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Subscriptions
{
    internal static class ProjectItemMetadata
    {
        // General Metadata
        public const string Name = "Name";
        public const string Type = "Type";
        public const string Version = "Version";
        public const string FileGroup = "FileGroup";
        public const string Path = "Path";
        public const string Resolved = "Resolved";
        public const string Dependencies = "Dependencies";
        public const string IsImplicitlyDefined = "IsImplicitlyDefined";
        public const string Severity = "Severity";
        public const string DiagnosticCode = "DiagnosticCode";
        public const string OriginalItemSpec = "OriginalItemSpec";

        // Target Metadata
        public const string RuntimeIdentifier = "RuntimeIdentifier";
        public const string TargetFrameworkMoniker = "TargetFrameworkMoniker";
        public const string FrameworkName = "FrameworkName";
        public const string FrameworkVersion = "FrameworkVersion";
    }
}

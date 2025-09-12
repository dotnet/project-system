// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.Retargeting;

internal partial class ProjectRetargetHandler
{
    internal class TargetChange() : IProjectTargetChange
    {
       public Guid NewTargetId { get; init; }

        public Guid CurrentTargetId { get; init; }

        public bool ReloadProjectOnSuccess => false;

        public bool UnloadOnFailure => false;

        public bool UnloadOnCancel => false;
    }
}

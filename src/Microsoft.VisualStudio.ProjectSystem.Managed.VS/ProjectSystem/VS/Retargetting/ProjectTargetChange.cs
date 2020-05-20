// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Retargetting
{
    internal class ProjectTargetChange : IProjectTargetChange
    {
        private readonly TargetDescriptionBase _targetDescription;
        private readonly IProjectRetargetCheckProvider _provider;

        public ProjectTargetChange(TargetDescriptionBase targetDescription, IProjectRetargetCheckProvider provider)
        {
            _targetDescription = targetDescription;
            _provider = provider;
        }

        public TargetDescriptionBase Description => _targetDescription;

        public IProjectRetargetCheckProvider RetargetProvider => _provider;

        public Guid NewTargetId => _targetDescription.TargetId;

        public Guid CurrentTargetId => Guid.Empty;

        public bool ReloadProjectOnSuccess => true;

        public bool UnloadOnFailure => true;

        public bool UnloadOnCancel => true;
    }
}

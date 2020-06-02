// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Retargetting
{
    internal class ProjectTargetChange : IProjectTargetChange
    {
        private readonly TargetDescriptionBase? _newTargetDescription;
        private readonly TargetDescriptionBase? _currentTargetDescription;
        private readonly IProjectRetargetCheckProvider? _provider;

        public ProjectTargetChange(TargetDescriptionBase currentTargetDescription)
        {
            _currentTargetDescription = currentTargetDescription;
        }

        public ProjectTargetChange(TargetDescriptionBase newTargetDescription, IProjectRetargetCheckProvider provider)
        {
            _newTargetDescription = newTargetDescription;
            _provider = provider;
        }

        public TargetDescriptionBase? NewTargetDescription => _newTargetDescription;
        public TargetDescriptionBase? CurrentTargetDescription => _currentTargetDescription;

        public IProjectRetargetCheckProvider? RetargetProvider => _provider;

        public Guid NewTargetId => _newTargetDescription?.TargetId ?? Guid.Empty;

        public Guid CurrentTargetId => _currentTargetDescription?.TargetId ?? Guid.Empty;

        public bool ReloadProjectOnSuccess => true;

        public bool UnloadOnFailure => true;

        public bool UnloadOnCancel => true;
    }
}

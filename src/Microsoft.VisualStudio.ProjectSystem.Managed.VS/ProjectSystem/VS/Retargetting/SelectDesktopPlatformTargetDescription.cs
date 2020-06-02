// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

/*
 * 
 *  IGNORE THIS, JUST FOR TESTING
 * 
 */
 
using System;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Retargetting
{
    internal class SelectDesktopPlatformTargetDescription : RetargetProjectTargetDescriptionBase
    {
        private readonly Guid _targetDescriptionId = new Guid("08A73DA5-B2C7-4DEF-8E1F-5D7C903ED3A3");
        public string? SelectedPlatform { get; private set; }

        public override Guid TargetId => _targetDescriptionId;

        public override string DisplayName => "Missing Desktop Platform";

        public override string RetargetingTitle => "No Desktop Platform Selected";

        public override string RetargetingDescription => "You have selected the Windows Desktop SDK but you have not selected to target Winforms or WPF. You must do this to load the project.";

        public override Array GetRetargetParameters() => new[] { "Platform" };

        public override Array GetPossibleParameterValues(string parameter)
        {
            return new string[] { "WindowsForms", "WPF" };
        }

        public override string GetValueDisplayName(string parameter, string pValue) => pValue;

        public override void PutParameterValue(string parameter, string pValue)
        {
            SelectedPlatform = pValue;
        }

        public override void ResetSelectedValues()
        {
            SelectedPlatform = null;
        }
    }
}



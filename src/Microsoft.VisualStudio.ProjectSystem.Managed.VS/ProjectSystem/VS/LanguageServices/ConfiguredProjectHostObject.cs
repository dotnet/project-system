// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.ProjectSystem.LanguageServices;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.LanguageServices
{
    /// <summary>
    /// Language service host object for a cross-targeting configured project that wraps the underlying unconfigured project host object.
    /// </summary>
    internal sealed class ConfiguredProjectHostObject : AbstractHostObject, IConfiguredProjectHostObject
    {
        private readonly UnconfiguredProjectHostObject _unconfiguredProjectHostObject;
        private readonly string _projectDisplayName;

        public ConfiguredProjectHostObject(UnconfiguredProjectHostObject unconfiguredProjectHostObject, string projectDisplayName)
            : base (innerHierarchy: unconfiguredProjectHostObject, innerVsProject: unconfiguredProjectHostObject)
        {
            Requires.NotNull(unconfiguredProjectHostObject, nameof(unconfiguredProjectHostObject));
            Requires.NotNullOrEmpty(projectDisplayName, nameof(projectDisplayName));

            _unconfiguredProjectHostObject = unconfiguredProjectHostObject;
            _projectDisplayName = projectDisplayName;
        }

        public String ProjectDisplayName => _projectDisplayName;
        public override String ActiveIntellisenseProjectDisplayName => _unconfiguredProjectHostObject.ActiveIntellisenseProjectDisplayName;

        #region IVsHierarchy overrides

        public override int GetProperty(uint itemid, int propid, out Object pvar)
        {
            switch (propid)
            {
                case (int)__VSHPROPID7.VSHPROPID_IsSharedItem:
                    // In our world everything is shared
                    pvar = true;
                    return VSConstants.S_OK;

                case (int)__VSHPROPID7.VSHPROPID_SharedProjectHierarchy:
                    pvar = InnerHierarchy;
                    return VSConstants.S_OK;

                default:
                    return base.GetProperty(itemid, propid, out pvar);
            }
        }

        #endregion
    }
}

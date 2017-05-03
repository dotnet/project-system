// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.ProjectSystem.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    [Export(typeof(IProjectSystemOptions))]
    internal class EnvironmentVariableProjectSystemOptions : IProjectSystemOptions
    {
        private readonly IEnvironmentHelper _environment;

        [ImportingConstructor]
        public EnvironmentVariableProjectSystemOptions(IEnvironmentHelper environment)
        {
            Requires.NotNull(environment, nameof(environment));

            _environment = environment;
        }

        public bool IsProjectOutputPaneEnabled
        {
            get { return !IsEnabled("PROJECTSYSTEM_PROJECTOUTPUTPANEENABLED"); }
        }

        private bool IsEnabled(string variable)
        {
            string value = _environment.GetEnvironmentVariable(variable);

            return string.Equals(value, "1", StringComparison.OrdinalIgnoreCase);
        }
    }
}

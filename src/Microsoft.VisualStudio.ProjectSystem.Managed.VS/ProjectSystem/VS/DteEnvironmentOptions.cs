// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    /// <summary>
    ///     Provides an implementation of <see cref="IEnvironmentOptions"/> that calls into <see cref="DTE"/>.
    /// </summary>
    [Export(typeof(IEnvironmentOptions))]
    internal class DteEnvironmentOptions : IEnvironmentOptions
    {
        private readonly IVsUIService<DTE2> _dte;

        [ImportingConstructor]
        public DteEnvironmentOptions(IVsUIService<SDTE, DTE2> dte)
        {
            _dte = dte;
        }

        public T GetOption<T>(string category, string page, string option, T defaultValue)
        {
            EnvDTE.Properties? properties = _dte.Value.get_Properties(category, page);

            if (properties is not null)
            {
                return (T)properties.Item(option).Value;
            }

            return defaultValue;
        }

        public void SetOption<T>(string category, string page, string option, T newValue)
        {
            EnvDTE.Properties? properties = _dte.Value.get_Properties(category, page);

            if (properties is not null)
            {
                properties.Item(option).Value = newValue;
            }
        }
    }
}

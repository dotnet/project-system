// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Composition;
using EnvDTE;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    /// <summary>
    ///     Provides an implementation of <see cref="IEnvironmentOptions"/> that calls into <see cref="DTE"/>.
    /// </summary>
    [Export(typeof(IEnvironmentOptions))]
    internal class DteEnvironmentOptions : IEnvironmentOptions
    {
        private readonly IDteServices _dteServices;

        [ImportingConstructor]
        public DteEnvironmentOptions(IDteServices dteServices)
        {
            Requires.NotNull(dteServices, nameof(dteServices));

            _dteServices = dteServices;
        }

        public T GetOption<T>(string category, string page, string option, T defaultValue)
        {
            var properties = _dteServices.Dte.Properties[category, page];
            if (properties != null)
            {
                return ((T)properties.Item(option).Value);
            }

            return defaultValue;
        }
    }
}

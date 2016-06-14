// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.Shell;


namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    [Export(typeof(IOptionsSettings))]
    internal class OptionsSettings : IOptionsSettings
    {
        private readonly SVsServiceProvider _serviceProvider;
        
        [ImportingConstructor]
        public OptionsSettings(SVsServiceProvider serviceProvider)
        {
            Requires.NotNull(serviceProvider, nameof(serviceProvider));
            _serviceProvider = serviceProvider;
        }

        public T GetPropertiesValue<T>(string category, string page, string property, T defaultValue)
        {
            //TODO : VerifyOnUIThread();

            EnvDTE.DTE dte = _serviceProvider.GetService<EnvDTE.DTE, EnvDTE.DTE>();
            var props = dte.Properties[category, page];
            if (props != null)
            {
                return ((T)props.Item(property).Value);
            }
            return defaultValue;
        }
    }
}

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.Shell;


namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    /// <summary>
    ///     Provides an implementation of <see cref="IOptionsSettings"/> that delegates onto 
    /// </summary>
    [Export(typeof(IOptionsSettings))]
    internal class OptionsSettings : IOptionsSettings
    {
        private readonly SVsServiceProvider _serviceProvider;
        private readonly IProjectThreadingService _threadingService;
       
        [ImportingConstructor]
        public OptionsSettings(SVsServiceProvider serviceProvider, IProjectThreadingService threadingService)
        {
            Requires.NotNull(serviceProvider, nameof(serviceProvider));
            Requires.NotNull(threadingService, nameof(threadingService));
            _serviceProvider = serviceProvider;
            _threadingService = threadingService;
        }

        public async Task<T> GetPropertiesValueAsync<T>(string category, string page, string property, T defaultValue)
        {
            await _threadingService.SwitchToUIThread();

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

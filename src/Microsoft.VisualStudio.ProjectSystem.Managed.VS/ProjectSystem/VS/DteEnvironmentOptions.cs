// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.Shell;
using EnvDTE;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    /// <summary>
    ///     Provides an implementation of <see cref="IEnvironmentOptions"/> that calls into <see cref="DTE"/>.
    /// </summary>
    [Export(typeof(IEnvironmentOptions))]
    internal class DteEnvironmentOptions : IEnvironmentOptions
    {
        private readonly SVsServiceProvider _serviceProvider;
        private readonly IProjectThreadingService _threadingService;

        [ImportingConstructor]
        public DteEnvironmentOptions(SVsServiceProvider serviceProvider, IProjectThreadingService threadingService)
        {
            Requires.NotNull(serviceProvider, nameof(serviceProvider));
            Requires.NotNull(threadingService, nameof(threadingService));
            _serviceProvider = serviceProvider;
            _threadingService = threadingService;
        }

        public T GetPropertiesValue<T>(string category, string page, string property, T defaultValue)
        {
            _threadingService.VerifyOnUIThread();

            DTE dte = _serviceProvider.GetService<DTE, DTE>();
            var props = dte.Properties[category, page];
            if (props != null)
            {
                return ((T)props.Item(property).Value);
            }
            return defaultValue;
        }
    }
}

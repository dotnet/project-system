// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.ProjectSystem.Properties;
using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    /// <summary>
    ///     Provides debuggers with information agbout the target output that the project generates.
    /// </summary>
    [Export(typeof(IDebuggerImageTypeService))]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasic + " & !" + ProjectCapability.LaunchProfiles )]
    internal class ManagedDebuggerImageTypeService : IDebuggerImageTypeService
    {
        private readonly ProjectProperties _properties;

        [ImportingConstructor]
        public ManagedDebuggerImageTypeService(ProjectProperties properties)
        {
            Requires.NotNull(properties, nameof(properties));

            _properties = properties;
        }

        public ImageClrType TargetImageClrType
        {
            get { return ImageClrType.Managed; }            // Used by "attach" when engine is set to auto-detect
        }

        public async Task<bool> GetIsConsoleAppAsync()
        {
            // Used by default Windows debugger to figure out whether to add an extra
            // pause to end of window when CTRL+F5'ing a console application
            var configuration = await _properties.GetConfigurationGeneralPropertiesAsync()
                                                 .ConfigureAwait(false);


            IEnumValue outputType = (IEnumValue)await configuration.OutputType.GetValueAsync()
                                                                      .ConfigureAwait(false);

            return StringComparers.PropertyValues.Equals(outputType.Name, ConfigurationGeneral.OutputTypeValues.exe);
        }

        
        public Task<bool> GetIs64BitAsync()
        {
            throw new NotSupportedException();              // GetIs64BitAsync isn't used *at all*
        }
        
        public string AppUserModelID
        {
            get { throw new NotSupportedException(); }      // AppUserModelID and PackageMoniker are used only by AppContainer-based debuggers
        }

        public string PackageMoniker
        {
            get { throw new NotSupportedException(); }
        }
    }
}

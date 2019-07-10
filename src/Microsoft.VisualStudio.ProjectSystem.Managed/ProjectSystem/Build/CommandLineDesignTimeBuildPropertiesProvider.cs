// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.Build
{
    /// <summary>
    ///     Influences design-time builds that causes CoreCompile to return the command-line 
    ///     arguments that it would have passed to the compiler, instead of calling it.
    /// </summary>
    [ExportBuildGlobalPropertiesProvider(designTimeBuildProperties: true)]
    [AppliesTo(ProjectCapability.DotNet)]
    internal class CommandLineDesignTimeBuildPropertiesProvider : StaticGlobalPropertiesProviderBase
    {
        private static readonly Task<IImmutableDictionary<string, string>> s_buildProperties = Task.FromResult<IImmutableDictionary<string, string>>(
            Empty.PropertiesMap.Add(BuildProperty.SkipCompilerExecution, "true")     // Don't run the compiler
                               .Add(BuildProperty.ProvideCommandLineArgs, "true"));  // Get csc/vbc to output command-line args

        [ImportingConstructor]
        public CommandLineDesignTimeBuildPropertiesProvider(IProjectService projectService)
            : base(projectService.Services)
        {
        }

        public override Task<IImmutableDictionary<string, string>> GetGlobalPropertiesAsync(CancellationToken cancellationToken)
        {
            return s_buildProperties;
        }
    }
}

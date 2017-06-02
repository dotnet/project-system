// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.Build
{
    /// <summary>
    ///     Turns on SuppressOutOfDateMessageOnBuild to prevent dialog popping up during builds, see: https://github.com/dotnet/project-system/issues/2358.
    /// </summary>
    [Export(typeof(IProjectGlobalPropertiesProvider))]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasicOrFSharpLanguageService)]
    internal class SuppressOutOfDateMessageOnBuildProvider : StaticGlobalPropertiesProviderBase
    {
        private static readonly Task<IImmutableDictionary<string, string>> BuildProperties = Task.FromResult<IImmutableDictionary<string, string>>(
            Empty.PropertiesMap.Add(BuildProperty.SuppressOutOfDateMessageOnBuild, "true"));

        [ImportingConstructor]
        public SuppressOutOfDateMessageOnBuildProvider(IProjectService projectService)
            : base(projectService.Services)
        {
        }

        public override Task<IImmutableDictionary<string, string>> GetGlobalPropertiesAsync(CancellationToken cancellationToken)
        {
            return BuildProperties;
        }
    }
}

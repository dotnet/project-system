// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties
{
    /// OutputTypeEx acts as a converter for the MSBuild OutputType value expressed as <see cref="VSLangProj110.prjOutputTypeEx"/>.
    [ExportInterceptingPropertyValueProvider("OutputTypeEx", ExportInterceptingPropertyValueProviderFile.ProjectFile)]
    internal sealed class OutputTypeExValueProvider : OutputTypeValueProviderBase
    {
        private readonly ProjectProperties _properties;

        private static readonly ImmutableDictionary<string, string> s_getOutputTypeExMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            {"WinExe",          "0" },
            {"Exe",             "1" },
            {"Library",         "2" },
            {"AppContainerExe", "3" },
            {"WinMDObj",        "4" },
        }.ToImmutableDictionary();

        private static readonly ImmutableDictionary<string, string> s_setOutputTypeExMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            {"0", "WinExe" },
            {"1", "Exe" },
            {"2", "Library" },
            {"3", "AppContainerExe" },
            {"4", "WinMDObj"},
        }.ToImmutableDictionary();

        protected override ImmutableDictionary<string, string> GetMap => s_getOutputTypeExMap;

        protected override ImmutableDictionary<string, string> SetMap => s_setOutputTypeExMap;

        [ImportingConstructor]
        public OutputTypeExValueProvider(ProjectProperties properties)
            : base(properties)
        {
        }
    }
}

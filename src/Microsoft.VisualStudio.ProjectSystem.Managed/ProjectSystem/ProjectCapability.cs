// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    ///     Provides common well-known project flags.
    /// </summary>
    internal static class ProjectCapability
    {
        public const string AlwaysAvailable = ProjectCapabilities.AlwaysApplicable;
        public const string CSharp = ProjectCapabilities.CSharp;
        public const string VisualBasic = ProjectCapabilities.VB;
        public const string VisualBasicLanguageService = ProjectCapabilities.VB + " & (" + ProjectCapabilities.LanguageService + " & !" + LanguageService2 + ")";
        public const string CSharpLanguageService = ProjectCapabilities.CSharp + " & (" + ProjectCapabilities.LanguageService + " & !" + LanguageService2 + ")";
        public const string CSharpOrVisualBasic = ProjectCapabilities.CSharp + " | " + ProjectCapabilities.VB;
        public const string CSharpOrVisualBasicLanguageService = "(" + ProjectCapabilities.CSharp + " | " + ProjectCapabilities.VB + ") & " + ProjectCapabilities.LanguageService;
        public const string CSharpOrVisualBasicLanguageService2 = "(" + ProjectCapabilities.CSharp + " | " + ProjectCapabilities.VB + ") & " + LanguageService2;
        public const string AppDesigner = nameof(AppDesigner);
        public const string DependenciesTree = nameof(DependenciesTree);
        public const string LanguageService2 = nameof(LanguageService2);
        public const string LaunchProfiles = "LaunchProfiles";
    }
}

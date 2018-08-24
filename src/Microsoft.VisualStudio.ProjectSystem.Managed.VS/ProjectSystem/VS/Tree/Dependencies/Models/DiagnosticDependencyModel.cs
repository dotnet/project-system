// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models
{
    internal enum DiagnosticMessageSeverity
    {
        Info,
        Warning,
        Error,
    }

    internal class DiagnosticDependencyModel : DependencyModel
    {
        private static readonly DependencyIconSet s_errorIconSet = new DependencyIconSet(
            icon: ManagedImageMonikers.ErrorSmall,
            expandedIcon: ManagedImageMonikers.ErrorSmall,
            unresolvedIcon: ManagedImageMonikers.ErrorSmall,
            unresolvedExpandedIcon: ManagedImageMonikers.ErrorSmall);

        private static readonly DependencyIconSet s_warningIconSet = new DependencyIconSet(
            icon: ManagedImageMonikers.WarningSmall,
            expandedIcon: ManagedImageMonikers.WarningSmall,
            unresolvedIcon: ManagedImageMonikers.WarningSmall,
            unresolvedExpandedIcon: ManagedImageMonikers.WarningSmall);

        public DiagnosticDependencyModel(
            string providerType,
            string originalItemSpec,
            DiagnosticMessageSeverity severity,
            string code,
            string message,
            ProjectTreeFlags flags,
            bool isVisible,
            IImmutableDictionary<string, string> properties)
            : base(providerType, originalItemSpec, originalItemSpec, flags, resolved: false, isImplicit: false, properties: properties)
        {
            Requires.NotNullOrEmpty(originalItemSpec, nameof(originalItemSpec));
            Requires.NotNullOrEmpty(message, nameof(message));

            code = code ?? string.Empty;
            Name = message;
            Caption = $"{code.ToUpperInvariant()} {message}".TrimStart();
            TopLevel = false;
            Visible = isVisible;
            Flags = Flags.Union(DependencyTreeFlags.DiagnosticNodeFlags);

            if (severity == DiagnosticMessageSeverity.Error)
            {
                IconSet = s_errorIconSet;
                Flags = Flags.Union(DependencyTreeFlags.DiagnosticErrorNodeFlags);
                Priority = Dependency.DiagnosticsErrorNodePriority;
            }
            else
            {
                IconSet = s_warningIconSet;
                Flags = Flags.Union(DependencyTreeFlags.DiagnosticWarningNodeFlags);
                Priority = Dependency.DiagnosticsWarningNodePriority;
            }
        }

        private string _id;
        public override string Id
        {
            get
            {
                if (_id == null)
                {
                    _id = OriginalItemSpec;
                }

                return _id;
            }
        }
    }
}

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
                Icon = ManagedImageMonikers.ErrorSmall;
                Flags = Flags.Union(DependencyTreeFlags.DiagnosticErrorNodeFlags);
                Priority = Dependency.DiagnosticsErrorNodePriority;
            }
            else
            {
                Icon = ManagedImageMonikers.WarningSmall;
                Flags = Flags.Union(DependencyTreeFlags.DiagnosticWarningNodeFlags);
                Priority = Dependency.DiagnosticsWarningNodePriority;
            }

            ExpandedIcon = Icon;
            UnresolvedIcon = Icon;
            UnresolvedExpandedIcon = Icon;
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

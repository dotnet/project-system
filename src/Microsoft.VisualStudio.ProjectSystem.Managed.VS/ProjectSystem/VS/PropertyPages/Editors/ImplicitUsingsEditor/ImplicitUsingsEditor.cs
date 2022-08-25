// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.VS.Implementation.PropertyPages.Designer;
using Microsoft.VisualStudio.ProjectSystem.VS.PropertyPages.Designer;

namespace Microsoft.VisualStudio.ProjectSystem.Properties.Controls;

[Export(typeof(IPropertyEditor))]
[ExportMetadata("Name", "CSharpImplicitUsingsEditor")]
internal sealed class ImplicitUsingsEditor : PropertyEditorBase
{
    public ImplicitUsingsEditor()
        : base("UnconfiguredCSharpImplicitUsingsTemplate", "ConfiguredCSharpImplicitUsingsEditorTemplate")
    {
    }

    public override bool ShowUnevaluatedValue => false;

    public override object DefaultValue => string.Empty;

    public override bool IsChangedByEvaluation(string unevaluatedValue, object? evaluatedValue, ImmutableDictionary<string, string> editorMetadata) => false;
}

// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using System.Windows;
using Microsoft.VisualStudio.ProjectSystem.VS.PropertyPages.Designer;
using Microsoft.VisualStudio.ProjectSystem.VS.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Implementation.PropertyPages.Designer
{

    /// <summary>
    /// Base class for property editors that ship with the Project Properties UI.
    /// </summary>
    internal abstract class PropertyEditorBase : IPropertyEditor
    {
        private static readonly Lazy<ResourceDictionary> LazyResources;
        private static readonly Lazy<DataTemplate> LazyGenericPropertyTemplate;

        // DataTemplates must be sourced on the UI thread. We use lazy access here
        // as they will be queried via WPF binding on the UI thread. This allows
        // us to construct the editor on a worker thread.

        private readonly Lazy<DataTemplate>? lazyPropertyDataTemplate;
        private readonly Lazy<DataTemplate?>? lazyUnconfiguredDataTemplate;
        private readonly Lazy<DataTemplate?>? lazyConfiguredDataTemplate;

        static PropertyEditorBase()
        {
            LazyResources = new Lazy<ResourceDictionary>(
                () =>
                {
                    if (!UriParser.IsKnownScheme("pack"))
                    {
                        UriParser.Register(new GenericUriParser(GenericUriParserOptions.GenericAuthority), "pack", defaultPort: -1);
                    }

                    return new ResourceDictionary
                    {
                        Source = new Uri("pack://application:,,,/Microsoft.VisualStudio.ProjectSystem.Managed.VS;component/ProjectSystem/VS/PropertyPages/Editors/PropertyEditorTemplates.xaml", UriKind.RelativeOrAbsolute)
                    };
                },
                LazyThreadSafetyMode.ExecutionAndPublication);

            LazyGenericPropertyTemplate = new Lazy<DataTemplate>(
                () =>
                {
                    var template = (DataTemplate?)LazyResources.Value["GenericPropertyTemplate"];
                    Assumes.NotNull(template);
                    return template;
                });
        }

        protected PropertyEditorBase(string? unconfiguredDataTemplateName, string? configuredDataTemplateName, string? propertyDataTemplateName = null)
        {
            if (propertyDataTemplateName != null)
            {
                lazyPropertyDataTemplate = new(() =>
                {
                    UIThreadHelper.VerifyOnUIThread();
                    var propertyDataTemplate = (DataTemplate?)LazyResources.Value[propertyDataTemplateName];
                    Assumes.NotNull(propertyDataTemplate);
                    return propertyDataTemplate;
                });
            }

            if (unconfiguredDataTemplateName != null)
            {
                lazyUnconfiguredDataTemplate = new(() =>
                {
                    UIThreadHelper.VerifyOnUIThread();
                    return (DataTemplate?)LazyResources.Value[unconfiguredDataTemplateName];
                });
            }

            if (configuredDataTemplateName != null)
            {
                lazyConfiguredDataTemplate = new(() =>
                {
                    UIThreadHelper.VerifyOnUIThread();
                    return (DataTemplate?)LazyResources.Value[configuredDataTemplateName];
                });
            }
        }

        public static DataTemplate GenericPropertyTemplate => LazyGenericPropertyTemplate.Value;

        public virtual bool ShowUnevaluatedValue => false;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public DataTemplate? PropertyDataTemplate
        {
            get
            {
                UIThreadHelper.VerifyOnUIThread();
                return lazyPropertyDataTemplate?.Value;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public DataTemplate? UnconfiguredDataTemplate
        {
            get
            {
                UIThreadHelper.VerifyOnUIThread();
                return lazyUnconfiguredDataTemplate?.Value;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public DataTemplate? ConfiguredDataTemplate
        {
            get
            {
                UIThreadHelper.VerifyOnUIThread();
                return lazyConfiguredDataTemplate?.Value;
            }
        }

        public abstract object? DefaultValue { get; }

        public virtual bool IsPseudoProperty => false;

        public virtual bool ShouldShowDescription(int valueCount) => true;

        public abstract bool IsChangedByEvaluation(string unevaluatedValue, object? evaluatedValue, ImmutableDictionary<string, string> editorMetadata);
    }
}

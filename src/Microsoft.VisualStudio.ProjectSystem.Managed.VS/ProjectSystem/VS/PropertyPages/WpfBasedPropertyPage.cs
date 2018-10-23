// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Forms;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PropertyPages
{
    internal abstract partial class WpfBasedPropertyPage : PropertyPage
    {
#pragma warning disable CA2213 // WPF Controls implement IDisposable 
        private PropertyPageElementHost _host;
#pragma warning restore CA2213
        private PropertyPageControl _control;
        private PropertyPageViewModel _viewModel;

        protected WpfBasedPropertyPage()
        {
            InitializeComponent();
        }

        protected abstract PropertyPageViewModel CreatePropertyPageViewModel();

        protected abstract PropertyPageControl CreatePropertyPageControl();

        protected override async Task OnSetObjects(bool isClosing)
        {
            if (isClosing)
            {
                _control.DetachViewModel();
                return;
            }
            else
            {
                //viewModel can be non-null when the configuration is changed.
                if (_control == null)
                {
                    _control = CreatePropertyPageControl();
                }
            }

            _viewModel = CreatePropertyPageViewModel();
            _viewModel.Project = UnconfiguredProject;
            await _viewModel.Initialize();
            _control.InitializePropertyPage(_viewModel);
        }

        protected override Task<int> OnApply()
        {
            return _control.Apply();
        }

        protected override Task OnDeactivate()
        {
            if (IsDirty)
            {
                return OnApply();
            }

            return Task.CompletedTask;
        }

        private void WpfPropertyPage_Load(object sender, EventArgs e)
        {
            SuspendLayout();

            _host = new PropertyPageElementHost
            {
                AutoSize = false,
                Dock = DockStyle.Fill
            };

            if (_control == null)
            {
                _control = CreatePropertyPageControl();
            }

            var viewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                Focusable = false,
            };

            viewer.Content = _control;
            _host.Child = viewer;

            wpfHostPanel.Dock = DockStyle.Fill;
            wpfHostPanel.Controls.Add(_host);

            ResumeLayout(true);
            _control.StatusChanged += OnControlStatusChanged;
        }

        private void OnControlStatusChanged(object sender, EventArgs e)
        {
            if (IsDirty != _control.IsDirty)
            {
                IsDirty = _control.IsDirty;
            }
        }
    }
}

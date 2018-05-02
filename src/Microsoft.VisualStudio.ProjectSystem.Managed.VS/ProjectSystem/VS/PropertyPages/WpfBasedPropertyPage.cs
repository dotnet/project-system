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

        public WpfBasedPropertyPage()
        {
            InitializeComponent();
        }

        // For unit testing
        internal WpfBasedPropertyPage(bool useJoinableTaskFactory) : base(useJoinableTaskFactory)
        {
            InitializeComponent();
        }

        protected abstract PropertyPageViewModel CreatePropertyPageViewModel();

        protected abstract PropertyPageControl CreatePropertyPageControl();

        protected async override Task OnSetObjects(bool isClosing)
        {
            if (isClosing)
            {
                _control.DetachViewModel();
                return;
            }
            else
            {
                //viewModel can be non-null when the configuration is chaged. 
                if (_control == null)
                {
                    _control = CreatePropertyPageControl();
                }
            }

            _viewModel = CreatePropertyPageViewModel();
            _viewModel.Project = UnconfiguredProject;
            await _viewModel.Initialize().ConfigureAwait(false);
            _control.InitializePropertyPage(_viewModel);
        }

        protected async override Task<int> OnApply()
        {
            return await _control.Apply().ConfigureAwait(false);
        }

        protected async override Task OnDeactivate()
        {
            if (IsDirty)
            {
                await OnApply().ConfigureAwait(false);
            }
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
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
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

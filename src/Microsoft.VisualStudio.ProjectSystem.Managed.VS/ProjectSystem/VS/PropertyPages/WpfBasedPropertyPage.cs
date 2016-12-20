// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Forms;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PropertyPages
{
    internal abstract partial class WpfBasedPropertyPage : PropertyPage
    {
        private PropertyPageElementHost host;
        private PropertyPageControl control;
        private PropertyPageViewModel viewModel;

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
                control.DetachViewModel();
                return;
            }
            else
            {
                //viewModel can be non-null when the configuration is chaged. 
                if (control == null)
                {
                    control = CreatePropertyPageControl();
                }
            }

            viewModel = CreatePropertyPageViewModel();
            viewModel.UnconfiguredProject = base.UnconfiguredProject;
            await viewModel.Initialize().ConfigureAwait(false);
            control.InitializePropertyPage(viewModel);
        }

        protected async override Task<int> OnApply()
        {
            return await control.Apply().ConfigureAwait(false);
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

            host = new PropertyPageElementHost();
            host.AutoSize = false;
            host.Dock = DockStyle.Fill;

            if (control == null)
            {
                control = CreatePropertyPageControl();
            }

            ScrollViewer viewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            viewer.Content = control;
            host.Child = viewer;

            wpfHostPanel.Dock = DockStyle.Fill;
            wpfHostPanel.Controls.Add(host);

            ResumeLayout(true);
            control.StatusChanged += OnControlStatusChanged;
        }

        private void OnControlStatusChanged(object sender, EventArgs e)
        {
            if (IsDirty != control.IsDirty)
            {
                IsDirty = control.IsDirty;
            }
        }
    }
}
// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PropertyPages
{
    internal class PropertyPageControl : UserControl
    {
        private bool _isDirty;
        private bool _ignoreEvents;

        public event EventHandler? StatusChanged;

        public PropertyPageViewModel? ViewModel
        {
            get
            {
                return DataContext as PropertyPageViewModel;
            }
            set
            {
                DataContext = value;
            }
        }

        public bool IsDirty
        {
            get { return _isDirty; }
            set
            {
                // Only process real changes
                if (value != _isDirty && !_ignoreEvents)
                {
                    _isDirty = value;
                    OnStatusChanged(EventArgs.Empty);
                }
            }
        }

        public virtual void InitializePropertyPage(PropertyPageViewModel viewModel)
        {
            _ignoreEvents = true;
            IsDirty = false;
            ViewModel = viewModel;
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            _ignoreEvents = false;
        }

        public virtual void DetachViewModel()
        {
            Assumes.NotNull(ViewModel);

            _ignoreEvents = true;
            IsDirty = false;
            ViewModel.PropertyChanged -= ViewModel_PropertyChanged;

            // Let the view model know we are done.
            ViewModel.ViewModelDetached();
            ViewModel = null;
            _ignoreEvents = false;
        }

        public async Task<int> ApplyAsync()
        {
            HResult result = HResult.OK;

            if (IsDirty)
            {
                result = await OnApplyAsync();
                if (result.IsOK)
                {
                    IsDirty = false;
                }
            }

            return result;
        }

        protected virtual Task<int> OnApplyAsync()
        {
            Assumes.NotNull(ViewModel);

            return ViewModel.SaveAsync();
        }

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            Assumes.NotNull(ViewModel);

            if (!_ignoreEvents && !ViewModel.IgnoreEvents)
            {
                IsDirty = true;
            }
        }

        protected virtual void OnStatusChanged(EventArgs args)
        {
            StatusChanged?.Invoke(this, args);
        }
    }
}

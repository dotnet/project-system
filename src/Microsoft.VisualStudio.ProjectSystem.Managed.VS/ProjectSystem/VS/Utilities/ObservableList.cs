// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Utilities
{
    internal class ObservableList<T> : ObservableCollection<T>
    {
        public event EventHandler? ValidationStatusChanged;

        private void OnItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged(new PropertyChangedEventArgs("Items"));
        }

        public void RaiseValidationStatus(bool validationSuccessful)
        {
            ValidationStatusChanged?.Invoke(this, new ValidationStatusChangedEventArgs(validationSuccessful));
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (INotifyPropertyChanged item in e.OldItems)
                    item.PropertyChanged -= OnItemPropertyChanged;
            }

            if (e.NewItems != null)
            {
                foreach (INotifyPropertyChanged item in e.NewItems)
                    item.PropertyChanged += OnItemPropertyChanged;
            }

            base.OnCollectionChanged(e);
        }
    }

    internal class ValidationStatusChangedEventArgs : EventArgs
    {
        public bool ValidationStatus { get; }

        public ValidationStatusChangedEventArgs(bool validationStatus)
        {
            ValidationStatus = validationStatus;
        }
    }
}

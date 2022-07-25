// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.ProjectSystem.VS.PropertyPages;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Utilities
{
    internal class NameValuePair : INotifyPropertyChanged, IDataErrorInfo
    {
        private readonly ObservableList<NameValuePair>? _parentCollection;

        private string? _name;
        private string? _value;

        public bool HasValidationError { get; set; }

        public NameValuePair(string? name, string? value, ObservableList<NameValuePair>? parentCollection = null)
        {
            _parentCollection = parentCollection;
            Name = name;
            Value = value;
        }

        public string? Name
        {
            get { return _name; }
            set { NotifyPropertyChanged(ref _name, value); }
        }

        public string? Value
        {
            get { return _value; }
            set { NotifyPropertyChanged(ref _value, value); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void NotifyPropertyChanged<T>(ref T refProperty, T value, [CallerMemberName] string? propertyName = null)
        {
            if (!Equals(refProperty, value))
            {
                refProperty = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #region IDataErrorInfo

        public string Error
        {
            get
            {
                System.Diagnostics.Debug.Fail("Checking for EntireRow of NameValuePair is not supposed to be invoked");
                throw new NotImplementedException();
            }
        }

        public string? this[string propertyName]
        {
            get
            {
                // Reset error condition
                string? error = null;
                HasValidationError = false;

                if (propertyName.Equals(nameof(Name), StringComparisons.UIPropertyNames))
                {
                    if (IsNamePropertyEmpty())
                    {
                        error = PropertyPageResources.NameCannotBeEmpty;
                        HasValidationError = true;
                    }
                    else if (IsNamePropertyDuplicate())
                    {
                        error = PropertyPageResources.DuplicateKey;
                        HasValidationError = true;
                    }
                    else
                    {
                        // We are doing Row Validation - make sure that in addition to Name - Value is valid
                        HasValidationError = IsValuePropertyEmpty();
                    }
                }
                else if (propertyName.Equals(nameof(Value), StringComparisons.UIPropertyNames))
                {
                    if (string.IsNullOrWhiteSpace(Value))
                    {
                        error = PropertyPageResources.ValueCannotBeEmpty;
                        HasValidationError = true;
                    }
                    else
                    {
                        // We are doing Row Validation - make sure that in addition to Value - Name is valid
                        HasValidationError = IsNamePropertyEmpty() || IsNamePropertyDuplicate();
                    }
                }

                _parentCollection?.RaiseValidationStatus(!HasValidationError);
                return error;
            }
        }

        private bool IsNamePropertyEmpty() => string.IsNullOrWhiteSpace(Name);

        private bool IsValuePropertyEmpty() => string.IsNullOrWhiteSpace(Value);

        private bool IsNamePropertyDuplicate()
        {
            if (_parentCollection is not null)
            {
                foreach (NameValuePair nvp in _parentCollection)
                {
                    if (!ReferenceEquals(this, nvp))
                    {
                        if (string.Equals(nvp.Name, Name, StringComparisons.UIPropertyNames))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        #endregion
    }
}

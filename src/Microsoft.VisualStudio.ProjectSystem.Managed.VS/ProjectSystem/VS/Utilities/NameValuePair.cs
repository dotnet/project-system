// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.Utilities
{
    using Microsoft.VisualStudio.ProjectSystem.VS.PropertyPages;
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;

    internal class NameValuePair : INotifyPropertyChanged, IDataErrorInfo
    {
        public ObservableList<NameValuePair> ParentCollection;
        public bool HasValidationError { get; set; }

        public NameValuePair(ObservableList<NameValuePair> parentCollection = null) { ParentCollection = parentCollection; }

        public NameValuePair(string name, string value, ObservableList<NameValuePair> parentCollection = null)
        {
            ParentCollection = parentCollection; Name = name; Value = value;
        }

        public NameValuePair(NameValuePair nvPair, ObservableList<NameValuePair> parentCollection = null)
        {
            ParentCollection = parentCollection; Name = nvPair.Name; Value = nvPair.Value;
        }

        private string _name;
        public string Name
        {
            get { return _name; }
            set { NotifyPropertyChanged(ref _name, value); }
        }

        private string _value;
        public string Value
        {
            get { return _value; }
            set { NotifyPropertyChanged(ref _value, value); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public bool NotifyPropertyChanged<T>(ref T refProperty, T value, [CallerMemberName] string propertyName = null)
        {
            if (!Object.Equals(refProperty, value))
            {
                refProperty = value;
                NotifyPropertyChanged(propertyName);
                return true;
            }

            return false;
        }

        public void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #region IDataErrorInfo

        public string Error
        {
            get
            {
                Debug.Fail("Checking for EntireRow of NameValuePair is not supposed to be invoked");
                throw new NotImplementedException();
            }
        }

        public string this[string propertyName]
        {
            get
            {
                //Reset error condition
                string error = null;
                HasValidationError = false;

                if (propertyName.Equals("Name", StringComparison.OrdinalIgnoreCase))
                {
                    if (IsNamePropertyEmpty())
                    {
                        error = PropertyPageResources.NameCannotBeEmpty;
                        HasValidationError = true;
                    }
                    else
                    {
                        if (IsNamePropertyDuplicate())
                        {
                            error = PropertyPageResources.DuplicateKey;
                            HasValidationError = true;
                        }
                    }
                    //We are doing Row Validation - make sure that in addition to Name - Value is valid
                    if (!HasValidationError) { HasValidationError = IsValuePropertyEmpty(); }
                }
                if (propertyName.Equals("Value", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrWhiteSpace(Value))
                    {
                        error = PropertyPageResources.ValueCannotBeEmpty;
                        HasValidationError = true;
                    }
                    //We are doing Row Validation - make sure that in addition to Value - Name is valid
                    if (!HasValidationError) { HasValidationError = IsNamePropertyEmpty() || IsNamePropertyDuplicate(); }
                }
                SendNotificationAfterValidation();
                return error;
            }
        }

        private bool IsNamePropertyEmpty()
        {
            return string.IsNullOrWhiteSpace(Name);
        }

        private bool IsNamePropertyDuplicate()
        {
            if (ParentCollection != null)
            {
                foreach (NameValuePair nvp in ParentCollection)
                {
                    if (!nvp.Equals(this))
                    {
                        if (string.Compare(nvp.Name, Name, StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private bool IsValuePropertyEmpty()
        {
            return string.IsNullOrWhiteSpace(Value);
        }

        private void SendNotificationAfterValidation()
        {
            if (ParentCollection != null)
            {
                ParentCollection.RaiseValidationStatus(!HasValidationError);
            }
        }

        #endregion

    }

}

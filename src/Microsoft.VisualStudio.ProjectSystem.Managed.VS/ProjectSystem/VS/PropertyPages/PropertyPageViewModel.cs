// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PropertyPages
{
    internal abstract class PropertyPageViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public UnconfiguredProject? Project { get; set; }
        public PropertyPageControl? ParentControl { get; set; }

        /// <summary>
        /// Since calls to ignore events can be nested, a downstream call could change the outer 
        /// value.  To guard against this, IgnoreEvents returns true if the count is > 0 and there is no setter. 
        /// PushIgnoreEvents\PopIgnoreEvents  are used instead to control the count.
        /// </summary>
        private int _ignoreEventsNestingCount = 0;

        public bool IgnoreEvents => _ignoreEventsNestingCount > 0;

        public void PushIgnoreEvents()
        {
            _ignoreEventsNestingCount++;
        }

        public void PopIgnoreEvents()
        {
            if (_ignoreEventsNestingCount > 0)
            {
                _ignoreEventsNestingCount--;
            }
        }

        public abstract Task Initialize();

        public abstract Task<int> Save();

        protected virtual void OnPropertyChanged(string? propertyName, bool suppressInvalidation = false)
        {
            // For some properties we don't want to invalidate the property page
            if (suppressInvalidation)
            {
                PushIgnoreEvents();
            }

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            if (suppressInvalidation)
            {
                PopIgnoreEvents();
            }
        }

        protected virtual bool OnPropertyChanged<T>(ref T propertyRef, T value, bool suppressInvalidation, [CallerMemberName] string? propertyName = null)
        {
            if (!Equals(propertyRef, value))
            {
                propertyRef = value;
                OnPropertyChanged(propertyName, suppressInvalidation);
                return true;
            }

            return false;
        }

        protected virtual bool OnPropertyChanged<T>(ref T propertyRef, T value, [CallerMemberName] string? propertyName = null)
        {
            return OnPropertyChanged(ref propertyRef, value, suppressInvalidation: false, propertyName: propertyName);
        }

        protected void SetBooleanProperty(ref bool property, string value, bool defaultValue, bool invert = false)
        {
            if (!string.IsNullOrEmpty(value))
            {
                property = bool.Parse(value);

                if (invert)
                {
                    property = !property;
                }
            }
            else
            {
                property = defaultValue;
            }
        }

        /// <summary>
        /// Override to do cleanup
        /// </summary>
        public virtual void ViewModelDetached()
        {

        }
    }
}

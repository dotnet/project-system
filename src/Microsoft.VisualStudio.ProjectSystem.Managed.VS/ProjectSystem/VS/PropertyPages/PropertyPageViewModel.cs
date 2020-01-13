// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PropertyPages
{
    internal abstract class PropertyPageViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected PropertyPageViewModel(UnconfiguredProject project)
        {
            Project = Requires.NotNull(project, nameof(project));
        }

        protected UnconfiguredProject Project { get; }

        /// <summary>
        /// Since calls to ignore events can be nested, a downstream call could change the outer 
        /// value.  To guard against this, IgnoreEvents returns true if the count is > 0 and there is no setter. 
        /// PushIgnoreEvents\PopIgnoreEvents  are used instead to control the count.
        /// </summary>
        private int _ignoreEventsNestingCount = 0;

        public bool IgnoreEvents => _ignoreEventsNestingCount > 0;

        protected void PushIgnoreEvents()
        {
            _ignoreEventsNestingCount++;
        }

        protected void PopIgnoreEvents()
        {
            if (_ignoreEventsNestingCount > 0)
            {
                _ignoreEventsNestingCount--;
            }
        }

        public abstract Task<int> SaveAsync();

        protected void OnPropertyChanged(string? propertyName, bool suppressInvalidation = false)
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

        protected bool OnPropertyChanged<T>(ref T propertyRef, T value, bool suppressInvalidation, [CallerMemberName] string? propertyName = null)
        {
            if (!Equals(propertyRef, value))
            {
                propertyRef = value;
                OnPropertyChanged(propertyName, suppressInvalidation);
                return true;
            }

            return false;
        }

        protected bool OnPropertyChanged<T>(ref T propertyRef, T value, [CallerMemberName] string? propertyName = null)
        {
            return OnPropertyChanged(ref propertyRef, value, suppressInvalidation: false, propertyName: propertyName);
        }

        /// <summary>
        /// Override to do cleanup
        /// </summary>
        public virtual void ViewModelDetached()
        {

        }
    }
}

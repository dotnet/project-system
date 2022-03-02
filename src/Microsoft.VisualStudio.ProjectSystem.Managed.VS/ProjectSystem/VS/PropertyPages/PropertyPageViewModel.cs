// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PropertyPages
{
    internal abstract class PropertyPageViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public UnconfiguredProject? Project { get; set; }

        /// <summary>
        /// Since calls to ignore events can be nested, a downstream call could change the outer
        /// value.  To guard against this, IgnoreEvents returns true if the count is > 0 and there is no setter.
        /// PushIgnoreEvents\PopIgnoreEvents  are used instead to control the count.
        /// </summary>
        private int _ignoreEventsNestingCount;

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

        public abstract Task InitializeAsync();

        public abstract Task<int> SaveAsync();

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

        /// <summary>
        /// Override to do cleanup
        /// </summary>
        public virtual void ViewModelDetached()
        {
        }
    }
}

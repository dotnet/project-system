// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.ProjectSystem.VS.PropertyPages.Designer;

namespace Microsoft.VisualStudio.ProjectSystem.Properties.Controls
{
    internal sealed class CSharpImplicitUsingsControl : Control
    {
        public static readonly DependencyProperty AddUserUsingCommandProperty = DependencyProperty.Register(
            nameof(AddUserUsingCommand),
            typeof(ICommand),
            typeof(CSharpImplicitUsingsControl),
            new PropertyMetadata());

        public static readonly DependencyProperty StringListProperty = DependencyProperty.Register(
            nameof(StringList),
            typeof(string),
            typeof(CSharpImplicitUsingsControl),
            new PropertyMetadata(string.Empty, (o, e) => ((MultiStringSelectorControl)o).OnStringListOrSupportedValuesPropertyChanged()));

        public static readonly DependencyProperty UsingCollectionStateProperty = DependencyProperty.Register(
            nameof(UsingCollectionState),
            typeof(ObservableCollection<ImplicitUsingsValueProvider.ImplicitUsing>),
            typeof(CSharpImplicitUsingsControl),
            new PropertyMetadata());

        // Used to suppress event handling during our own updates, breaking infinite loops.
        private bool updating;

        public CSharpImplicitUsingsControl()
        {
            UsingCollectionState = new ObservableCollection<ImplicitUsingsValueProvider.ImplicitUsing>();

            AddUserUsingCommand = new DelegateCommand<ImplicitUsingsValueProvider.ImplicitUsing>(newUsing =>
            {
                if (!UsingCollectionState.Any(implicitUsing =>
                        string.Equals(implicitUsing.Name, newUsing.Include, StringComparison.Ordinal)))
                {
                    UsingCollectionState.Add(newUsing);
                    NotifyPairChanged();
                }

                this.EnteredUserString = string.Empty;
            });

            this.OnStringListOrSupportedValuesPropertyChanged();
        }

        public ICommand AddUserStringCommand
        {
            get => (ICommand)this.GetValue(AddUserStringCommandProperty);
            set => this.SetValue(AddUserStringCommandProperty, value);
        }

        public bool AllowsCustomStrings
        {
            get => (bool)this.GetValue(AllowsCustomStringsProperty);
            set => this.SetValue(AllowsCustomStringsProperty, value);
        }

        public string MultiStringSelectorTypeDescriptorText
        {
            get => (string)this.GetValue(MultiStringSelectorTypeDescriptorTextProperty);
            set => this.SetValue(MultiStringSelectorTypeDescriptorTextProperty, value);
        }

        public string EnteredUserString
        {
            get => (string)this.GetValue(EnteredUserStringProperty);
            set => this.SetValue(EnteredUserStringProperty, value);
        }

        public string StringList
        {
            get => (string)this.GetValue(StringListProperty);
            set => this.SetValue(StringListProperty, value);
        }

        public IList<SupportedValue>? SupportedValues
        {
            get => (IList<SupportedValue>?)this.GetValue(SupportedValuesProperty);
            set => this.SetValue(SupportedValuesProperty, value);
        }

        public ObservableCollection<CheckableString> StringsCheckedState
        {
            get => (ObservableCollection<CheckableString>)this.GetValue(StringsCheckedStateProperty);
            set => this.SetValue(StringsCheckedStateProperty, value);
        }

        private void OnStringListOrSupportedValuesPropertyChanged()
        {
            if (this.updating)
            {
                return;
            }

            this.updating = true;

            try
            {
                Dictionary<string, bool> checkedStrings = this.encoding.Parse(this.StringList)
                    .Where(pair => bool.TryParse(pair.Value, out bool _))
                    .ToDictionary(pair => pair.Name, pair => bool.Parse(pair.Value), StringComparer.Ordinal);

                ISet<string>? supportedValues = this.SupportedValues
                    ?.Where((supportedValue, _) => !checkedStrings.ContainsKey(supportedValue.Value))
                    .Select(value => value.Value).ToHashSet(StringComparer.Ordinal);

                Dictionary<string, CheckableString> checkableStrings = new Dictionary<string, CheckableString>(StringComparer.Ordinal);

                foreach ((string? name, bool isReadOnly) in checkedStrings)
                {
                    if (name.Length > 0)
                    {
                        checkableStrings[name] = new CheckableString(name: name, isChecked: true, isReadOnly: isReadOnly, parent: this);
                    }
                }

                if (supportedValues != null)
                {
                    foreach (string supportedValue in supportedValues)
                    {
                        if (supportedValue.Length > 0 && !checkableStrings.ContainsKey(supportedValue))
                        {
                            checkableStrings.Add(supportedValue, new CheckableString(name: supportedValue, isChecked: false, isReadOnly: false, parent: this));
                        }
                    }
                }

                if (this.StringsCheckedState.Count == 0)
                {
                    foreach (CheckableString checkableString in checkableStrings.Values)
                    {
                        this.StringsCheckedState.Add(checkableString);
                    }
                }
                else
                {
                    int stringIndex = 0;

                    foreach (CheckableString checkableString in checkableStrings.Values)
                    {
                        if (stringIndex < this.StringsCheckedState.Count)
                        {
                            if (!this.StringsCheckedState[stringIndex].Equals(checkableString))
                            {
                                this.StringsCheckedState[stringIndex] = checkableString;
                            }
                        }
                        else
                        {
                            this.StringsCheckedState.Add(checkableString);
                        }

                        stringIndex++;
                    }

                    if (stringIndex < this.StringsCheckedState.Count - 1)
                    {
                        for (int i = this.StringsCheckedState.Count - 1; i >= stringIndex + 1; i--)
                        {
                            this.StringsCheckedState.RemoveAt(i);
                        }
                    }
                }
            }
            finally
            {
                this.updating = false;
            }
        }

        public void NotifyPairChanged()
        {
            if (this.updating)
            {
                return;
            }

            this.StringList = this.encoding.Format(
                this.StringsCheckedState
                    .Where(checkableString => checkableString.IsChecked && checkableString.Name.Length > 0)
                    .Select(checkableString => (checkableString.Name, Value: checkableString.IsReadOnly.ToString())));
        }
    }

    internal sealed class CheckableString : INotifyPropertyChanged
    {
        private static readonly PropertyChangedEventArgs NameChangeArgs = new(nameof(Name));
        private static readonly PropertyChangedEventArgs IsCheckedChangeArgs = new(nameof(IsChecked));
        private static readonly PropertyChangedEventArgs IsReadOnlyChangeArgs = new(nameof(IsReadOnly));
        private readonly MultiStringSelectorControl parent;

        private string name;
        private bool isChecked;
        private bool isReadOnly;

        public CheckableString(string name, bool isChecked, bool isReadOnly, MultiStringSelectorControl parent)
        {
            this.name = name;
            this.isChecked = isChecked;
            this.isReadOnly = isReadOnly;
            this.parent = parent;

            this.ToggleStringCommand = new DelegateCommand<CheckableString>(checkableString =>
            {
                checkableString.parent.NotifyPairChanged();
            });
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public ICommand ToggleStringCommand { get; }

        public string Name
        {
            get => this.name;
            set
            {
                // Name may not be empty, unless it was set as such in the constructor (for a new row)
                if (string.IsNullOrWhiteSpace(value))
                {
                    return;
                }

                if (string.Equals(this.name, value, StringComparison.Ordinal))
                {
                    return;
                }

                this.name = value;
                this.PropertyChanged?.Invoke(this, NameChangeArgs);
                this.parent.NotifyPairChanged();
            }
        }

        public bool IsChecked
        {
            get => this.isChecked;
            set
            {
                if (this.isChecked == value)
                {
                    return;
                }

                this.isChecked = value;
                this.PropertyChanged?.Invoke(this, IsCheckedChangeArgs);
            }
        }

        public bool IsReadOnly
        {
            get => this.isReadOnly;
            set
            {
                if (this.isReadOnly == value)
                {
                    return;
                }

                this.isReadOnly = value;
                this.PropertyChanged?.Invoke(this, IsReadOnlyChangeArgs);
                this.parent.NotifyPairChanged();
            }
        }

        public override bool Equals(object obj)
        {
            return obj is CheckableString otherCheckableString &&
                   string.Equals(this.Name, otherCheckableString.Name, StringComparison.Ordinal) &&
                   this.IsChecked == otherCheckableString.IsChecked
                   && this.IsReadOnly == otherCheckableString.IsReadOnly;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = this.parent.GetHashCode();
                hashCode = (hashCode * 397) ^ this.name.GetHashCode();
                hashCode = (hashCode * 397) ^ this.isChecked.GetHashCode();
                return hashCode;
            }
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.VisualStudio.PlatformUI;
using Newtonsoft.Json;

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
            new PropertyMetadata(string.Empty, (o, e) => ((CSharpImplicitUsingsControl)o).OnStringListOrSupportedValuesPropertyChanged()));

        public static readonly DependencyProperty UsingCollectionStateProperty = DependencyProperty.Register(
            nameof(UsingCollectionState),
            typeof(ObservableCollection<ImplicitUsingModel>),
            typeof(CSharpImplicitUsingsControl),
            new PropertyMetadata(new ObservableCollection<ImplicitUsingModel>()));

        public static readonly DependencyProperty EnteredUserUsingIncludesProperty = DependencyProperty.Register(
            nameof(EnteredUserUsingIncludes),
            typeof(string),
            typeof(CSharpImplicitUsingsControl),
            new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty EnteredUserUsingAliasProperty = DependencyProperty.Register(
            nameof(EnteredUserUsingAlias),
            typeof(string),
            typeof(CSharpImplicitUsingsControl),
            new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty EnteredUserUsingIsStaticProperty = DependencyProperty.Register(
            nameof(EnteredUserUsingIsStatic),
            typeof(bool),
            typeof(CSharpImplicitUsingsControl),
            new PropertyMetadata(false));
        
        public static readonly DependencyProperty ShouldShowReadonlyUsingsProperty = DependencyProperty.Register(
            nameof(ShouldShowReadonlyUsings),
            typeof(bool),
            typeof(CSharpImplicitUsingsControl),
            new PropertyMetadata(true, (o, e) => ((CSharpImplicitUsingsControl)o).OnStringListOrSupportedValuesPropertyChanged()));
        
        // Used to suppress event handling during our own updates, breaking infinite loops.
        private bool _isUpdating;

        public CSharpImplicitUsingsControl()
        {
            UsingCollectionState = new ObservableCollection<ImplicitUsingModel>();

#pragma warning disable VSTHRD012
            AddUserUsingCommand = new DelegateCommand(_ =>
            {
                string usingIncludes = EnteredUserUsingIncludes;
                if (usingIncludes.Length == 0)
                {
                    return;
                }

                if (!UsingCollectionState.Any(implicitUsing =>
                        string.Equals(implicitUsing.Include, usingIncludes, StringComparison.Ordinal)))
                {
                    UsingCollectionState.Add(new ImplicitUsingModel(usingIncludes, EnteredUserUsingAlias, EnteredUserUsingIsStatic, false, this));
                    NotifyPairChanged();
                }

                EnteredUserUsingIncludes = string.Empty;
                EnteredUserUsingAlias = string.Empty;
                EnteredUserUsingIsStatic = false;
            });

#pragma warning restore VSTHRD012

            OnStringListOrSupportedValuesPropertyChanged();
        }

        public ICommand AddUserUsingCommand
        {
            get => (ICommand)GetValue(AddUserUsingCommandProperty);
            set => SetValue(AddUserUsingCommandProperty, value);
        }

        public string EnteredUserUsingIncludes
        {
            get => (string)GetValue(EnteredUserUsingIncludesProperty);
            set => SetValue(EnteredUserUsingIncludesProperty, value);
        }

        public string EnteredUserUsingAlias
        {
            get => (string)GetValue(EnteredUserUsingAliasProperty);
            set => SetValue(EnteredUserUsingAliasProperty, value);
        }

        public bool EnteredUserUsingIsStatic
        {
            get => (bool)GetValue(EnteredUserUsingIsStaticProperty);
            set => SetValue(EnteredUserUsingIsStaticProperty, value);
        }
        
        public bool ShouldShowReadonlyUsings
        {
            get => (bool)GetValue(ShouldShowReadonlyUsingsProperty);
            set => SetValue(ShouldShowReadonlyUsingsProperty, value);
        }
        
        public string StringList
        {
            get => (string)GetValue(StringListProperty);
            set => SetValue(StringListProperty, value);
        }

        public ObservableCollection<ImplicitUsingModel> UsingCollectionState
        {
            get => (ObservableCollection<ImplicitUsingModel>)GetValue(UsingCollectionStateProperty);
            set => SetValue(UsingCollectionStateProperty, value);
        }

        private void OnStringListOrSupportedValuesPropertyChanged()
        {
            if (_isUpdating)
            {
                return;
            }

            _isUpdating = true;

            try
            {
                if (JsonConvert.DeserializeObject(StringList, typeof(List<ImplicitUsing>)) is not List<ImplicitUsing> rawImplicitUsings)
                {
                    return;
                }
                
                rawImplicitUsings.Sort((x, y) =>
                {
                    if ((x.IsReadOnly && y.IsReadOnly) || (!x.IsReadOnly && !y.IsReadOnly))
                    {
                        return string.Compare(x.Include, y.Include, StringComparison.Ordinal);
                    }

                    return x.IsReadOnly ? 1 : -1;
                });

                if (!ShouldShowReadonlyUsings)
                {
                    rawImplicitUsings.RemoveAll(implicitUsing => implicitUsing.IsReadOnly);
                }

                var currentlySetImplicitUsings = rawImplicitUsings.GroupBy(implicitUsing => implicitUsing.Include).Select(group => group.First()).ToList();
                
                if (UsingCollectionState.Count == 0)
                {
                    foreach (ImplicitUsing implicitUsing in currentlySetImplicitUsings)
                    {
                        UsingCollectionState.Add(new ImplicitUsingModel(implicitUsing.Include, implicitUsing.Alias ?? string.Empty, implicitUsing.IsStatic, implicitUsing.IsReadOnly, this));
                    }
                }
                else
                {
                    int stringIndex = 0;

                    foreach (ImplicitUsing implicitUsing in currentlySetImplicitUsings)
                    {
                        if (stringIndex < UsingCollectionState.Count)
                        {
                            ImplicitUsingModel existingUsingModel = UsingCollectionState[stringIndex];
                            if (!existingUsingModel.ToImplicitUsing().Equals(implicitUsing))
                            {
                                if (string.Equals(existingUsingModel.Include, implicitUsing.Include, StringComparison.Ordinal))
                                {
                                    if (!string.Equals(existingUsingModel.Alias, implicitUsing.Alias ?? string.Empty, StringComparison.Ordinal))
                                    {
                                        existingUsingModel.Alias = implicitUsing.Alias ?? string.Empty;
                                    }

                                    if (existingUsingModel.IsStatic != implicitUsing.IsStatic)
                                    {
                                        existingUsingModel.IsStatic = implicitUsing.IsStatic;
                                    }

                                    if (existingUsingModel.IsReadOnly != implicitUsing.IsReadOnly)
                                    {
                                        existingUsingModel.IsReadOnly = implicitUsing.IsReadOnly;
                                    }
                                }
                                else
                                {
                                    UsingCollectionState[stringIndex] = new ImplicitUsingModel(implicitUsing.Include, implicitUsing.Alias ?? string.Empty, implicitUsing.IsStatic, implicitUsing.IsReadOnly, this);
                                }
                            }
                        }
                        else
                        {
                            UsingCollectionState.Add(new ImplicitUsingModel(implicitUsing.Include, implicitUsing.Alias ?? string.Empty, implicitUsing.IsStatic, implicitUsing.IsReadOnly, this));
                        }

                        stringIndex++;
                    }

                    if (stringIndex < UsingCollectionState.Count - 1)
                    {
                        for (int i = UsingCollectionState.Count - 1; i >= stringIndex; i--)
                        {
                            UsingCollectionState.RemoveAt(i);
                        }
                    }
                }
            }
            finally
            {
                _isUpdating = false;
            }
        }

        public void NotifyPairChanged()
        {
            if (_isUpdating)
            {
                return;
            }

            StringList = JsonConvert.SerializeObject(UsingCollectionState.Select(implicitUsingModel => implicitUsingModel.ToImplicitUsing()));
        }
    }

    internal sealed class ImplicitUsingModel : INotifyPropertyChanged
    {
        private readonly CSharpImplicitUsingsControl _parent;
        private static readonly PropertyChangedEventArgs s_includeChangeArgs = new(nameof(Include));
        private static readonly PropertyChangedEventArgs s_aliasChangeArgs = new(nameof(Alias));
        private static readonly PropertyChangedEventArgs s_isStaticChangeArgs = new(nameof(IsStatic));
        private static readonly PropertyChangedEventArgs s_isReadOnlyChangeArgs = new(nameof(IsReadOnly));
        private static readonly PropertyChangedEventArgs s_forceIncludesFocusChangeArgs = new(nameof(ForceIncludesFocus));
        private static readonly PropertyChangedEventArgs s_forceAliasFocusChangeArgs = new(nameof(ForceAliasFocus));
        private static readonly PropertyChangedEventArgs s_forceIsStaticFocusChangeArgs = new(nameof(ForceIsStaticFocus));

        private string _include;
        private string _alias;
        private bool _isStatic;
        private bool _isReadOnly;
        
        private bool _forceIncludesFocus;
        private bool _forceAliasFocus;
        private bool _forceIsStaticFocus;

        public ImplicitUsingModel(string include, string alias, bool isStatic, bool isReadOnly, CSharpImplicitUsingsControl parent)
        {
            _include = include;
            _alias = alias;
            _isStatic = isStatic;
            _isReadOnly = isReadOnly;
            _parent = parent;
            
            _forceIncludesFocus = false;
            _forceAliasFocus = false;
            _forceIsStaticFocus = false;

#pragma warning disable VSTHRD012
            RemoveUsingCommand = new DelegateCommand<ImplicitUsingModel>(model =>
#pragma warning restore VSTHRD012
            {
                model._parent.UsingCollectionState.Remove(model);
                model._parent.NotifyPairChanged();
            });
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public ICommand RemoveUsingCommand { get; }

        public string Include
        {
            get => _include;
            set
            {
                // Name may not be empty, unless it was set as such in the constructor (for a new row)
                if (string.IsNullOrWhiteSpace(value))
                {
                    return;
                }

                if (string.Equals(_include, value, StringComparison.Ordinal))
                {
                    return;
                }

                _include = value;
                PropertyChanged?.Invoke(this, s_includeChangeArgs);
                _parent.NotifyPairChanged();
            }
        }

        public string Alias
        {
            get => _alias;
            set
            {
                if (string.Equals(_alias, value, StringComparison.Ordinal))
                {
                    return;
                }

                _alias = value;
                PropertyChanged?.Invoke(this, s_aliasChangeArgs);
                _parent.NotifyPairChanged();
            }
        }

        public bool IsStatic
        {
            get => _isStatic;
            set
            {
                if (_isStatic == value)
                {
                    return;
                }

                _isStatic = value;
                PropertyChanged?.Invoke(this, s_isStaticChangeArgs);
                _parent.NotifyPairChanged();
            }
        }

        public bool IsReadOnly
        {
            get => _isReadOnly;
            set
            {
                if (_isReadOnly == value)
                {
                    return;
                }

                _isReadOnly = value;
                PropertyChanged?.Invoke(this, s_isReadOnlyChangeArgs);
                _parent.NotifyPairChanged();
            }
        }
        
        public bool ForceIncludesFocus
        {
            get => _forceIncludesFocus;
            set
            {
                _forceIncludesFocus = value;
                PropertyChanged?.Invoke(this, s_forceIncludesFocusChangeArgs);
            }
        }
        
        public bool ForceAliasFocus
        {
            get => _forceAliasFocus;
            set
            {
                _forceAliasFocus = value;
                PropertyChanged?.Invoke(this, s_forceAliasFocusChangeArgs);
            }
        }
        
        public bool ForceIsStaticFocus
        {
            get => _forceIsStaticFocus;
            set
            {
                _forceIsStaticFocus = value;
                PropertyChanged?.Invoke(this, s_forceIsStaticFocusChangeArgs);
            }
        }

        public ImplicitUsing ToImplicitUsing()
        {
            return new ImplicitUsing(
                Include,
                IsStatic ? null : string.IsNullOrWhiteSpace(Alias) ? null : Alias,
                IsStatic,
                IsReadOnly);
        }
    }
}

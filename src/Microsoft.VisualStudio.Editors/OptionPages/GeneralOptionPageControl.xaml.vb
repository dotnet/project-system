' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Windows.Controls
Imports System.Windows.Data

Namespace Microsoft.VisualStudio.Editors.OptionPages
    Partial Friend NotInheritable Class GeneralOptionPageControl
        Inherits OptionPageControl

        Private _generalOptions As GeneralOptions

        Public Sub New(serviceProvider As IServiceProvider)
            MyBase.New()

            _generalOptions = New GeneralOptions(serviceProvider)

            InitializeComponent()

            Dim binding = New Binding() With {
                .Source = _generalOptions,
                .Path = New Windows.PropertyPath(NameOf(GeneralOptions.FastUpToDateCheck)),
                .UpdateSourceTrigger = UpdateSourceTrigger.Explicit
            }

            Dim bindingExpression = FastUpToDateCheck.SetBinding(CheckBox.IsCheckedProperty, binding)
            AddBinding(bindingExpression)

            binding = New Binding() With {
                    .Source = _generalOptions,
                    .Path = New Windows.PropertyPath(NameOf(GeneralOptions.VerboseLogging)),
                    .UpdateSourceTrigger = UpdateSourceTrigger.Explicit
                    }

            bindingExpression = VerboseLogging.SetBinding(CheckBox.IsCheckedProperty, binding)
            AddBinding(bindingExpression)
        End Sub
    End Class
End Namespace
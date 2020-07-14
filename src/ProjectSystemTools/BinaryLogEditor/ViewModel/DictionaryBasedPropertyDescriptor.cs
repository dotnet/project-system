// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BinaryLogEditor.ViewModel
{
    internal sealed class DictionaryBasedPropertyDescriptor : PropertyDescriptor
    {
        private readonly string _value;

        public override Type ComponentType => typeof(Dictionary<string, string>);

        public override bool IsReadOnly => true;

        public override Type PropertyType => typeof(string);

        public override string Category { get; }

        public DictionaryBasedPropertyDescriptor(string name, string value, string category) : base(name, null)
        {
            _value = value;
            Category = category;
        }

        public override bool CanResetValue(object component) => false;

        public override object GetValue(object component) => _value;

        public override void ResetValue(object component) => throw new InvalidOperationException();

        public override void SetValue(object component, object value) => throw new InvalidOperationException();

        public override bool ShouldSerializeValue(object component) => false;
    }
}

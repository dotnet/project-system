// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.VisualStudio.ProjectSystem.LogModel;
using Microsoft.VisualStudio.ProjectSystem.Tools.TableControl;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.TableManager;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BinaryLogEditor.ViewModel
{
    internal sealed class SelectedObjectWrapper : CustomTypeDescriptor, ITableDataSource
    {
        private class DummyDisposable : IDisposable
        {
            public void Dispose()
            {
            }
        }

        private class MessageWrapper : ITableEntry
        {
            private readonly Message _message;

            public object Identity => _message;

            public MessageWrapper(Message message)
            {
                _message = message;
            }

            public bool TryGetValue(string keyName, out object content)
            {
                switch (keyName)
                {
                    case StandardTableKeyNames.ErrorSeverity:
                        content = __VSERRORCATEGORY.EC_MESSAGE;
                        return true;

                    case TableKeyNames.Time:
                        content = _message.Timestamp;
                        return true;

                    case StandardTableKeyNames.Text:
                        content = _message.Text;
                        return true;
                }

                content = null;
                return false;
            }

            public bool TrySetValue(string keyName, object content) => false;

            public bool CanSetValue(string keyName) => false;
        }

        private const string MessageDataSourceDisplayName = "Message Data Source";
        private const string MessageTableDataSourceIdentifier = nameof(MessageTableDataSourceIdentifier);
        private const string MessageTableDataSourceSourceTypeIdentifier = nameof(MessageTableDataSourceSourceTypeIdentifier);

        private readonly string _className;
        private readonly string _componentName;
        private readonly IEnumerable<Message> _messages;
        private readonly IDictionary<string, IDictionary<string, string>> _dictionaries;

        public string SourceTypeIdentifier => MessageTableDataSourceSourceTypeIdentifier;

        public string Identifier => MessageTableDataSourceIdentifier;

        public string DisplayName => MessageDataSourceDisplayName;

        public SelectedObjectWrapper(string className, string componentName, IEnumerable<Message> messages, IDictionary<string, IDictionary<string, string>> dictionaries)
        {
            _className = className;
            _componentName = componentName;
            _messages = messages;
            _dictionaries = dictionaries;
        }

        public override string GetClassName() => _className;

        public override string GetComponentName() => _componentName;

        public override object GetPropertyOwner(PropertyDescriptor pd) => this;

        public override PropertyDescriptorCollection GetProperties() => GetProperties(null);

        public override PropertyDescriptorCollection GetProperties(Attribute[] attributes) =>
            new PropertyDescriptorCollection(
                (from dictionary in _dictionaries
                 where dictionary.Value != null
                 from kvp in dictionary.Value
                 select new DictionaryBasedPropertyDescriptor(kvp.Key, kvp.Value, dictionary.Key))
                .Cast<PropertyDescriptor>().ToArray());

        public IDisposable Subscribe(ITableDataSink sink)
        {
            if (_messages != null)
            {
                sink.AddEntries(_messages.Select(m => new MessageWrapper(m)).ToList(), true);
            }
            // Don't care when they don't need updates any more.
            return new DummyDisposable();
        }
    }
}

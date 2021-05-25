// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;
using System.Runtime.Serialization;

namespace Microsoft.VisualStudio.Serialization
{
    public class SerializationProvider : ValueSerializer<ISerializable>
    {
        private const string TypePropName = "Type";
        private const string NamesPropName = "Names";
        private const string ValuesPropName = "Values";

        public static IValueSerializer Instance { get; } = new SerializationProvider();

        private SerializationProvider() { }

        protected override ISerializable Deserialize(StreamReader reader)
        {
            return reader.ReadObject(r =>
            {
                var typeIdentity = r.ReadTypeIdentity(TypePropName);
                var names = r.ReadArray(NamesPropName, r2 => r2.ReadString());
                var values = r.ReadArray(ValuesPropName, r2 => r2.ReadValue());

                // First, gather data from the pipe.
                var type = reader.TypeIdentityResolution.ResolveType(typeIdentity);
                var context = new StreamingContext(StreamingContextStates.Other);
                var info = new SerializationInfo(type, new FormatterConverter());

                for (int i = 0; i < names.Length; i++)
                {
                    var value = r.ValueSerialization.DeserializeValue(values[i]);
                    info.AddValue(names[i], value);
                }

                // Next, locate the the special serialization constructor.
                var constructor = type.GetConstructor(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                    binder: null,
                    new[] { typeof(SerializationInfo), typeof(StreamingContext) },
                    modifiers: null);

                if (constructor is null)
                {
                    throw new InvalidOperationException(SR.Could_not_find_serialization_constructor);
                }

                // Finally, call the special serialization constructor.
                return (ISerializable)constructor.Invoke(new object[] { info, context });
            });
        }

        protected override void Serialize(ISerializable value, StreamWriter writer)
        {
            // First, gather data from the object.
            var type = value.GetType();
            var context = new StreamingContext(StreamingContextStates.Other);
            var info = new SerializationInfo(type, new FormatterConverter());

            value.GetObjectData(info, context);

            // Next, write the data to the pipe.
            writer.WriteObject(info, (w, i) =>
            {
                using var pooledList1 = ListPool<string>.GetPooledObject();
                using var pooledList2 = ListPool<Value>.GetPooledObject();

                var nameList = pooledList1.Object;
                var valueList = pooledList2.Object;

                foreach (var entry in i)
                {
                    var name = entry.Name;
                    var value = w.ValueSerialization.SerializeValue(entry.Value);

                    nameList.Add(name);
                    valueList.Add(value);
                }

                w.Write(TypePropName, type.GetTypeIdentity());
                w.WriteArray(NamesPropName, nameList, (w, v) => w.Write(v));
                w.WriteArray(ValuesPropName, valueList, (w, v) => w.Write(v));
            });
        }
    }
}

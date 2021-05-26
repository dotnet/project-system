//// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

//using System;
//using System.IO;

//namespace Microsoft.VisualStudio.Serialization
//{
//    public abstract class ValueSerializer<T> : IValueSerializer
//    {
//        public string Name => typeof(T).FullName;

//        public Type Type => typeof(T);

//        protected abstract T Deserialize(StreamReader reader);
//        protected abstract void Serialize(T value, StreamWriter writer);

//        object IValueSerializer.Deserialize(StreamReader reader) => Deserialize(reader);

//        void IValueSerializer.Serialize(object value, StreamWriter writer) => Serialize(value is null ? default : (T)value, writer);
//    }

//    public interface IValueSerializer
//    {
//        string Name { get; }
//        Type Type { get; }

//        void Serialize(object value, StreamWriter writer);
//        object Deserialize(StreamReader reader);
//    }
//}

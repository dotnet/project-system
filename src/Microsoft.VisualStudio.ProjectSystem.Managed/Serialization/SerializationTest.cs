// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;
using System.Runtime.Serialization;
using System.Text;

namespace Microsoft.VisualStudio.Serialization
{
    public static class SerializationTest
    {
        public static void Serialize(Stream stream, object? value)
        {
            if (value is null)
            {
                return;
            }
            using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
            var type = value.GetType();
            writer.Write(type.AssemblyQualifiedName);
            writer.Flush();
            new DataContractSerializer(type).WriteObject(stream, value);
        }

        public static object? Deserialize(Stream stream)
        {
            using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
            var type = Type.GetType(reader.ReadString());
            return new DataContractSerializer(type).ReadObject(stream);
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;
using System.Text.Json;

namespace Microsoft.VisualStudio.Serialization
{
    public static class SerializationTest
    {
        public static void Serialize(Stream stream, object value)
        {
            if(value is null)
            {
                return;
            }
            // Utf8JsonReader/Writer are more efficient
            var writer = new StreamWriter(stream);
            var container = new BasicSerializationContainer
            {
                FullTypeName = value.GetType().FullName,
                Value = value
            };
            var jsonString = JsonSerializer.Serialize(container);
            File.AppendAllLines(@"C:\Workspace\JsonTest-Serialize.txt", new[] { jsonString });
            writer.Write(jsonString);
            writer.Flush();
            stream.Position = 0;
        }

        //private static Type[] AllowedTypes = { typeof(int), typeof(bool), typeof(string), };

        public static object Deserialize(Stream stream)
        {
            var streamAsString = new StreamReader(stream).ReadToEnd();
            File.AppendAllLines(@"C:\Workspace\JsonTest-Deserialize.txt", new[] { streamAsString });
            var container = JsonSerializer.Deserialize<BasicSerializationContainer>(streamAsString);
            var type = Type.GetType(container.FullTypeName);
            return JsonSerializer.Deserialize(((JsonElement)container.Value).ToString(), type);
        }
    }

    public class BasicSerializationContainer
    {
        public string FullTypeName { get; set; }
        public object Value { get; set; }
    }
}

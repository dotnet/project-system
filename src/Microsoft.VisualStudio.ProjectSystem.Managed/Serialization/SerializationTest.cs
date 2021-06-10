// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.VisualStudio.Serialization
{
    public static class SerializationTest
    {
        public static void Serialize(Stream stream, object? value)
        {
            if(value is null)
            {
                return;
            }
            var writer = new StreamWriter(stream);
            var jsonString = JsonSerializer.Serialize(new SerializationContainer(value), SerializationContainer.Options);
            //File.AppendAllLines(@"C:\Workspace\JsonTest-Serialize.txt", new[] { jsonString });
            writer.Write(jsonString);
            writer.Flush();
            stream.Position = 0;
        }

        // TODO: Determine type filtering mechanic
        //private static Type[] AllowedTypes = { typeof(int), typeof(bool), typeof(string), };

        public static object? Deserialize(Stream stream)
        {
            var jsonString = new StreamReader(stream).ReadToEnd();
            //File.AppendAllLines(@"C:\Workspace\JsonTest-Deserialize.txt", new[] { jsonString });
            return JsonSerializer.Deserialize<SerializationContainer>(jsonString, SerializationContainer.Options).Value;
        }
    }

    public class SerializationContainer
    {
        [JsonIgnore]
        public Type Type { get; set; }
        public string AssemblyQualifiedName
        {
            get => Type.AssemblyQualifiedName;
            set => Type = Type.GetType(value);
        }
        public object? Value { get; set; }
        public bool IsISerializable { get; set; }

        private static readonly Type ISerializableType = typeof(ISerializable);

        public SerializationContainer() { }

        public SerializationContainer(object value)
        {
            Type = value.GetType();
            Value = value;
            IsISerializable = ISerializableType.IsAssignableFrom(Type);
        }

        [JsonIgnore]
        public static JsonSerializerOptions Options { get; } =
            new JsonSerializerOptions { Converters = { new SerializationContainerConverter() } };
    }

    //https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-converters-how-to?pivots=dotnet-5-0#deserialize-inferred-types-to-object-properties
    public class ObjectToContainerConverter : JsonConverter<object>
    {
        public override object? Read(ref Utf8JsonReader reader, Type _, JsonSerializerOptions __)
        {
            var jsonString = JsonDocument.ParseValue(ref reader).RootElement.GetRawText();
            return JsonSerializer.Deserialize<SerializationContainer>(jsonString, SerializationContainer.Options).Value;
        }

        public override void Write(Utf8JsonWriter writer, object? value, JsonSerializerOptions _)
        {
            if(value is null)
            {
                writer.WriteNullValue();
                return;
            }

            JsonSerializer.Serialize(writer, new SerializationContainer(value), SerializationContainer.Options);
        }
    }

    public class ObjectArrayToContainerConverter : JsonConverter<object[]>
    {
        public override object?[]? Read(ref Utf8JsonReader reader, Type _, JsonSerializerOptions __)
        {
            var jsonString = JsonDocument.ParseValue(ref reader).RootElement.GetRawText();
            // We use default deserialization (no converters) so that each array element remains as a JsonElement
            var value = JsonSerializer.Deserialize<object?[]?>(jsonString);
            if(value is not null)
            {
                for (var i = 0; i < value.Length; ++i)
                {
                    if (value[i] is JsonElement jsonElement)
                    {
                        value[i] = JsonSerializer.Deserialize<SerializationContainer>(jsonElement.GetRawText(), SerializationContainer.Options).Value;
                    }
                }
            }

            return value;
        }

        public override void Write(Utf8JsonWriter writer, object?[]? value, JsonSerializerOptions _)
        {
            if (value is null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartArray();
            foreach(var element in value)
            {
                if(element is not null)
                {
                    JsonSerializer.Serialize(writer, new SerializationContainer(element), SerializationContainer.Options);
                }
            }
            writer.WriteEndArray();
        }
    }

    //https://github.com/dotnet/runtime/issues/1784
    public class SerializationContainerConverter : JsonConverter<SerializationContainer>
    {
        private static readonly JsonSerializerOptions ObjectOptions =
            new() { Converters = { new ObjectArrayToContainerConverter(), new ObjectToContainerConverter() } };

        public override SerializationContainer Read(ref Utf8JsonReader reader, Type _, JsonSerializerOptions __)
        {
            // We use default deserialization (no converters) so that the container's value remains as a JsonElement
            if (JsonDocument.ParseValue(ref reader).RootElement.GetRawText() is not string jsonString
                || JsonSerializer.Deserialize<SerializationContainer>(jsonString) is not SerializationContainer container
                || container.Value is not JsonElement element
                || element.GetRawText() is not string elementString)
            {
                throw new InvalidDataException();
            }

            object? value = container.IsISerializable
                ? DeserializeISerializable(elementString, container.Type)
                : JsonSerializer.Deserialize(elementString, container.Type, ObjectOptions);
            return new SerializationContainer
            {
                Type = container.Type,
                Value = value,
                IsISerializable = container.IsISerializable
            };

            static object? DeserializeISerializable(string valueJson, Type valueType)
            {
                using var stream = new MemoryStream(new UTF8Encoding().GetBytes(valueJson));
                return new DataContractJsonSerializer(valueType).ReadObject(stream);
            }
        }

        //https://davidsekar.com/javascript/converting-json-date-string-date-to-date-object
        //https://stackoverflow.com/a/115034/294804
        private static readonly DataContractJsonSerializerSettings DateTimeSettings = new() { DateTimeFormat = new DateTimeFormat("o") };

        public override void Write(Utf8JsonWriter writer, SerializationContainer container, JsonSerializerOptions _)
        {
            // Manually serialize the container using different means to serialize Value.
            writer.WriteStartObject();
            writer.WriteString(nameof(SerializationContainer.AssemblyQualifiedName), container.AssemblyQualifiedName);
            writer.WritePropertyName(nameof(SerializationContainer.Value));

            JsonDocument document = container.IsISerializable
                ? SerializeISerializable(container.Value, container.Type)
                : JsonDocument.Parse(JsonSerializer.Serialize(container.Value, container.Type, ObjectOptions));
            document.WriteTo(writer);

            writer.WriteBoolean(nameof(SerializationContainer.IsISerializable), container.IsISerializable);
            writer.WriteEndObject();

            static JsonDocument SerializeISerializable(object? value, Type type)
            {
                using var stream = new MemoryStream();
                new DataContractJsonSerializer(type, DateTimeSettings).WriteObject(stream, value);
                stream.Position = 0;
                return JsonDocument.Parse(stream);
            }
        }
    }
}

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
            var container = new SerializationContainer(value);
            var jsonString = JsonSerializer.Serialize(container, SerializationContainer.Options);
            File.AppendAllLines(@"C:\Workspace\JsonTest-Serialize.txt", new[] { jsonString });
            writer.Write(jsonString);
            writer.Flush();
            stream.Position = 0;
        }

        //private static Type[] AllowedTypes = { typeof(int), typeof(bool), typeof(string), };

        public static object? Deserialize(Stream stream)
        {
            var jsonString = new StreamReader(stream).ReadToEnd();
            File.AppendAllLines(@"C:\Workspace\JsonTest-Deserialize.txt", new[] { jsonString });
            var container = JsonSerializer.Deserialize<SerializationContainer>(jsonString, SerializationContainer.Options);
            return container.Value;
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
        public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var jsonString = JsonDocument.ParseValue(ref reader).RootElement.GetRawText();
            return JsonSerializer.Deserialize<SerializationContainer>(jsonString, SerializationContainer.Options).Value;
        }

        public override void Write(Utf8JsonWriter writer, object? value, JsonSerializerOptions options)
        {
            if(value is null)
            {
                writer.WriteNullValue();
                return;
            }

            var container = new SerializationContainer(value);
            JsonSerializer.Serialize(writer, container, SerializationContainer.Options);
        }
    }

    public class ObjectArrayToContainerConverter : JsonConverter<object[]>
    {
        public override object?[]? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var jsonString = JsonDocument.ParseValue(ref reader).RootElement.GetRawText();
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

        public override void Write(Utf8JsonWriter writer, object?[]? value, JsonSerializerOptions options)
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
                    var container = new SerializationContainer(element);
                    JsonSerializer.Serialize(writer, container, SerializationContainer.Options);
                }
            }
            //for (var i = 0; i < value.Length; ++i)
            //{
            //    if (value[i] is not null)
            //    {
            //        var container = new SerializationContainer(value[i]!);
            //        var newOptions = new JsonSerializerOptions { Converters = { new SerializationContainerConverter() } };
            //        JsonSerializer.Serialize(writer, container, newOptions);
            //    }
            //}
            writer.WriteEndArray();
        }
    }

    //https://github.com/dotnet/runtime/issues/1784
    public class SerializationContainerConverter : JsonConverter<SerializationContainer>
    {
        private static readonly JsonSerializerOptions ObjectOptions =
            new JsonSerializerOptions { Converters = { new ObjectArrayToContainerConverter(), new ObjectToContainerConverter() } };

        public override SerializationContainer Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            //https://github.com/dotnet/runtime/issues/1784
            var jsonString = JsonDocument.ParseValue(ref reader).RootElement.GetRawText();
            if (JsonSerializer.Deserialize<SerializationContainer>(jsonString) is not SerializationContainer container
                || Type.GetType(container.AssemblyQualifiedName) is not Type type
                || container.Value is not JsonElement element
                || element.GetRawText() is not string elementString)
            {
                throw new InvalidDataException();
            }

            if (container.IsISerializable)
            {
                using var stream = new MemoryStream(new UTF8Encoding().GetBytes(elementString));
                var value = new DataContractJsonSerializer(container.Type).ReadObject(stream);
                return new SerializationContainer
                {
                    Type = type,
                    Value = value,
                    IsISerializable = container.IsISerializable
                };
            }

            return new SerializationContainer
            {
                Type = type,
                Value = JsonSerializer.Deserialize(elementString, type, ObjectOptions),
                IsISerializable = container.IsISerializable
            };
        }

        public override void Write(Utf8JsonWriter writer, SerializationContainer container, JsonSerializerOptions options)
        {
            if (container.IsISerializable)
            {
                //https://davidsekar.com/javascript/converting-json-date-string-date-to-date-object
                //https://stackoverflow.com/a/115034/294804
                var settings = new DataContractJsonSerializerSettings { DateTimeFormat = new DateTimeFormat("o") };
                using var stream = new MemoryStream();
                new DataContractJsonSerializer(container.Type, settings).WriteObject(stream, container.Value);
                stream.Position = 0;

                // Manually serialize the container using the DataContract's serialization of Value.
                writer.WriteStartObject();
                writer.WriteString(nameof(SerializationContainer.AssemblyQualifiedName), container.AssemblyQualifiedName);
                writer.WritePropertyName(nameof(SerializationContainer.Value));
                JsonDocument.Parse(stream).WriteTo(writer);
                writer.WriteBoolean(nameof(SerializationContainer.IsISerializable), container.IsISerializable);
                writer.WriteEndObject();

                return;
            }

            writer.WriteStartObject();
            writer.WriteString(nameof(SerializationContainer.AssemblyQualifiedName), container.AssemblyQualifiedName);
            writer.WritePropertyName(nameof(SerializationContainer.Value));
            //https://github.com/dotnet/runtime/issues/1784
            JsonDocument.Parse(JsonSerializer.Serialize(container.Value, container.Type, ObjectOptions)).WriteTo(writer);
            writer.WriteBoolean(nameof(SerializationContainer.IsISerializable), container.IsISerializable);
            writer.WriteEndObject();
        }
    }
}

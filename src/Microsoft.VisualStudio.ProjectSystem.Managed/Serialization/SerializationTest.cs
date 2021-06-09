// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
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
            var container = ContainerizeObjectHierarchy(value.GetType(), value);
            // Utf8JsonReader/Writer are more efficient
            var options = new JsonSerializerOptions { Converters = { new SerializationContainerConverter() } };
            var jsonString = JsonSerializer.Serialize(container, options);
            File.AppendAllLines(@"C:\Workspace\JsonTest-Serialize.txt", new[] { jsonString });
            writer.Write(jsonString);
            writer.Flush();
            stream.Position = 0;
        }

        private static readonly Type ObjectType = typeof(object);
        private static readonly Type ObjectArrayType = typeof(object[]);
        //private static readonly Type StringType = typeof(string);
        private static readonly Type ISerializableType = typeof(ISerializable);

        private static PropertyInfo[] GetProperties(Type type) => type
            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy)
            .Where(pi => pi.CanRead && pi.CanWrite && pi.GetCustomAttribute<JsonIgnoreAttribute>() is null
                && (pi.GetCustomAttribute<BrowsableAttribute>() is null || pi.GetCustomAttribute<BrowsableAttribute>().Browsable))
            .ToArray();

        //// TODO: IsInterface doesn't work since we cannot assign SerializationContainer to an interface property
        //private static bool IsContainerizable(Type type) =>
        //    type == ObjectType; //|| type.IsInterface;

        // https://github.com/Azure/autorest.powershell/blob/67d99227e4cb8b03ca3168731bcbe23ae14d35e5/powershell/resources/psruntime/BuildTime/PsExtensions.cs#L37-L45
        private static bool IsSimple(this Type type) =>
            type.IsPrimitive
            || type.IsEnum
            || type == typeof(string)
            || type == typeof(decimal);

        // https://github.com/Azure/autorest.powershell/blob/67d99227e4cb8b03ca3168731bcbe23ae14d35e5/powershell/resources/psruntime/BuildTime/PsExtensions.cs#L16-L35
        public static Type? GetElementType(Type type)
        {
            if (type.IsArray)
            {
                return type.GetElementType();
            }

            if (type.IsGenericType && typeof(IEnumerable<>).IsAssignableFrom(type))
            {
                return type.GetGenericArguments().First();
            }

            return null;
        }

        private static SerializationContainer ContainerizeObjectHierarchy(Type type, object value)
        {
            if (ISerializableType.IsAssignableFrom(type))
            {
                //using var stream = new MemoryStream();
                //new DataContractJsonSerializer(type).WriteObject(stream, value);
                //value = JsonDocument.Parse(stream).RootElement;

                return new SerializationContainer
                {
                    //AssemblyQualifiedName = type.AssemblyQualifiedName,
                    Type = type,
                    Value = value,
                    IsISerializable = true
                };
            }


            if (!type.IsSimple())
            {
                foreach (var property in GetProperties(type))
                {
                    var propertyValue = property.GetValue(value);
                    if(propertyValue is null)
                    {
                        continue;
                    }

                    var propertyType = property.PropertyType;
                    if ((propertyType == ObjectArrayType || (GetElementType(propertyType) is Type elementType && !elementType.IsSimple()))
                        && propertyValue is IList propertyCollection)
                    {
                        for (var i = 0; i < propertyCollection.Count; ++i)
                        {
                            if (propertyCollection[i] is not null)
                            {
                                var container = ContainerizeObjectHierarchy(propertyCollection[i].GetType(), propertyCollection[i]);
                                // Only assign the container itself if the property is defined as an object array.
                                propertyCollection[i] = propertyType == ObjectArrayType ? container : container.Value;
                            }
                        }
                        continue;
                    }

                    //if (propertyType == ObjectArrayType && propertyValue is object?[] propertyArray)
                    //{
                    //    for (var i = 0; i < propertyArray.Length; ++i)
                    //    {
                    //        if (propertyArray[i] is object propertyElement)
                    //        {
                    //            propertyArray[i] = ContainerizeObjectHierarchy(propertyElement.GetType(), propertyElement);
                    //        }
                    //    }
                    //    continue;
                    //}

                    //if (GetElementType(propertyType) is Type elementType && !elementType.IsSimple() && propertyValue is IList propertyList)
                    //{
                    //    for (var i = 0; i < propertyList.Count; ++i)
                    //    {
                    //        if (propertyList[i] is object propertyElement)
                    //        {
                    //            propertyList[i] = ContainerizeObjectHierarchy(propertyElement.GetType(), propertyElement).Value;
                    //        }
                    //    }
                    //    continue;
                    //}

                    if (propertyType == ObjectType || !propertyType.IsSimple())
                    {
                        var container = ContainerizeObjectHierarchy(propertyValue.GetType(), propertyValue);
                        // Only assign the container itself if the property is defined as an object.
                        property.SetValue(value, propertyType == ObjectType ? container : container.Value);
                    }

                    //if (!propertyType.IsSimple())
                    //{
                    //    property.SetValue(value, ContainerizeObjectHierarchy(propertyValue.GetType(), propertyValue).Value);
                    //}
                }
            }

            //type.GetInterfaces().Contains(ISerializableType)

            //if (ISerializableType.IsAssignableFrom(type))
            //{
            //    using var stream = new MemoryStream();
            //    new DataContractJsonSerializer(type).WriteObject(stream, value);
            //    value = JsonDocument.Parse(stream).RootElement;
            //}

            return new SerializationContainer
            {
                //AssemblyQualifiedName = type.AssemblyQualifiedName,
                Type = type,
                Value = value
            };
        }

        //private static Type[] AllowedTypes = { typeof(int), typeof(bool), typeof(string), };

        public static object? Deserialize(Stream stream)
        {
            var streamAsString = new StreamReader(stream).ReadToEnd();
            File.AppendAllLines(@"C:\Workspace\JsonTest-Deserialize.txt", new[] { streamAsString });
            // Type filtering will use FullName from the type
            (Type type, object? value, bool isISerializable) = ConvertContainerJsonString(streamAsString);
            return DecontainerizeObjectHierarchy(type, value, isISerializable);
            //SerializationContainer? container = JsonSerializer.Deserialize<SerializationContainer>(streamAsString);
            //if (container is null)
            //{
            //    return null;
            //}
            //Type? type = Type.GetType(container.AssemblyQualifiedName);
            //// Type filtering will use FullName from the type
            //if(type is null)
            //{
            //    throw new TypeLoadException();
            //}
            //if(container.Value is not JsonElement)
            //{
            //    throw new InvalidDataException();
            //}
            //return JsonSerializer.Deserialize(((JsonElement)container.Value).ToString(), type);
            //return DecontainerizeObjectHierarchy(container);
        }

        private static (Type Type, object? Value, bool IsISerializable) ConvertContainerJsonString(string jsonString)
        {
            var options = new JsonSerializerOptions { Converters = { new SerializationContainerConverter() } };
            if (JsonSerializer.Deserialize<SerializationContainer>(jsonString, options) is not SerializationContainer container
                || Type.GetType(container.AssemblyQualifiedName) is not Type type
                || container.Value is not JsonElement element)
            {
                throw new InvalidDataException();
            }

            if (container.IsISerializable)
            {
                using var stream = new MemoryStream(new UTF8Encoding().GetBytes(element.GetRawText()));
                //new DataContractJsonSerializer(container.Type).WriteObject(stream, container.Value);
                var value = new DataContractJsonSerializer(container.Type).ReadObject(stream);
                //value = JsonDocument.Parse(stream).RootElement;
                //JsonDocument.Parse(stream).WriteTo(writer);
                return (type, value, true);
            }

            return (type, JsonSerializer.Deserialize(element.GetRawText(), type, options), false);
        }

        private static object? DecontainerizeObjectHierarchy(Type type, object? value, bool isISerializable)
        {
            if (!isISerializable && value is not null && !type.IsSimple())
            {
                foreach (var property in GetProperties(type))
                {
                    var propertyValue = property.GetValue(value);
                    if (propertyValue is null)
                    {
                        continue;
                    }

                    var propertyType = property.PropertyType;
                    if ((propertyType == ObjectArrayType || (GetElementType(propertyType) is Type testElementType && !testElementType.IsSimple()))
                        && propertyValue is IList propertyCollection)
                    {
                        for (var i = 0; i < propertyCollection.Count; ++i)
                        {
                            if (propertyCollection[i] is not null)
                            {
                                Type elementType = propertyCollection[i].GetType();
                                object? elementValue = propertyCollection[i];
                                var elementIsISerializable = false;
                                if (propertyCollection[i] is JsonElement jsonElement)
                                {
                                    (elementType, elementValue, elementIsISerializable) = ConvertContainerJsonString(jsonElement.GetRawText());
                                }
                                propertyCollection[i] = DecontainerizeObjectHierarchy(elementType, elementValue, elementIsISerializable);
                            }
                        }
                        continue;
                    }

                    //if (propertyType == ObjectArrayType && property.GetValue(value) is object?[] propArray)
                    //{
                    //    for (var i = 0; i < propArray.Length; ++i)
                    //    {
                    //        if (propArray[i] is JsonElement propArrayElement)
                    //        {
                    //            (Type elementType, object? elementValue) = ConvertContainerJsonString(propArrayElement.GetRawText());
                    //            propArray[i] = DecontainerizeObjectHierarchy(elementType, elementValue);
                    //        }
                    //    }
                    //}

                    //if (GetElementType(propertyType) is Type collectionElementType && !collectionElementType.IsSimple() && property.GetValue(value) is IList propCollection)
                    //{
                    //    for (var i = 0; i < propCollection.Count; ++i)
                    //    {
                    //        if (propCollection[i] is object propCollectionElement)
                    //        {
                    //            propCollection[i] = DecontainerizeObjectHierarchy(propCollectionElement.GetType(), propCollectionElement);
                    //        }
                    //    }
                    //    continue;
                    //}

                    if (propertyType == ObjectType || !propertyType.IsSimple())
                    {
                        var propertyIsISerializable = false;
                        if (propertyValue is JsonElement jsonElement)
                        {
                            (propertyType, propertyValue, propertyIsISerializable) = ConvertContainerJsonString(jsonElement.GetRawText());
                        }
                        property.SetValue(value, DecontainerizeObjectHierarchy(propertyType, propertyValue, propertyIsISerializable));
                    }

                    //if (propertyType == ObjectType && property.GetValue(value) is JsonElement propElement)
                    //{
                    //    (Type elementType, object? elementValue) = ConvertContainerJsonString(propElement.GetRawText());
                    //    property.SetValue(value, DecontainerizeObjectHierarchy(elementType, elementValue));
                    //    continue;
                    //}

                    //if (!propertyType.IsSimple())
                    //{
                    //    property.SetValue(value, DecontainerizeObjectHierarchy(propertyType, propertyValue));
                    //}
                }
            }

            return value;
        }
    }

    //[JsonConverter(typeof(SerializationContainerConverter))]
    public class SerializationContainer
    {
        [JsonIgnore]
        public Type Type { get; set; }
        //public string AssemblyQualifiedName { get; set; }
        public string AssemblyQualifiedName {
            get => Type.AssemblyQualifiedName;
            set => Type = Type.GetType(value);
        }
        public object Value { get; set; }
        public bool IsISerializable { get; set; }
        //public object Value
        //{
        //    get
        //    {

        //    }
        //    set
        //    {
        //        if (ISerializableType.IsAssignableFrom(type))
        //        {
        //            using var stream = new MemoryStream();
        //            new DataContractJsonSerializer(type).WriteObject(stream, value);
        //            value = new UTF8Encoding().GetString(stream.ToArray());
        //        }
        //    }
        //}
    }

    ////https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-converters-how-to?pivots=dotnet-5-0#deserialize-inferred-types-to-object-properties
    //public class ObjectToInferredTypesConverter : JsonConverter<object>
    //{
    //    public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => reader.TokenType switch
    //    {
    //        JsonTokenType.True => true,
    //        JsonTokenType.False => false,
    //        JsonTokenType.Number when reader.TryGetInt64(out long l) => l,
    //        JsonTokenType.Number => reader.GetDouble(),
    //        JsonTokenType.String when reader.TryGetDateTime(out DateTime datetime) => datetime,
    //        JsonTokenType.String => reader.GetString(),
    //        _ => JsonDocument.ParseValue(ref reader).RootElement.Clone()
    //    };

    //    public override void Write(Utf8JsonWriter writer, object objectToWrite, JsonSerializerOptions options) =>
    //        JsonSerializer.Serialize(writer, objectToWrite, objectToWrite.GetType(), options);
    //}

    //public class BitmapConverter : JsonConverter<Bitmap>
    //{
    //    public override Bitmap Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    //    {
    //        if(reader.TokenType != JsonTokenType.StartArray)
    //        {
    //            throw new InvalidDataException();
    //        }

    //        reader.
    //    }
    //            //Temperature.Parse(reader.GetString());

    //    public override void Write(Utf8JsonWriter writer, Bitmap bitmap, JsonSerializerOptions options)
    //    {
    //        using var stream = new MemoryStream();
    //        bitmap.Save(stream, bitmap.RawFormat);
    //        writer.WriteStartArray();
    //        foreach(var byteElement in stream.ToArray())
    //        {
    //            writer.WriteNumberValue(byteElement);
    //        }
    //        writer.WriteEndArray();
    //    }
    //}

    //https://github.com/dotnet/runtime/issues/1784
    public class SerializationContainerConverter : JsonConverter<SerializationContainer>
    {
        public override SerializationContainer Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            //if (reader.TokenType != JsonTokenType.StartArray)
            //{
            //    throw new InvalidDataException();
            //}

            //reader.
            //var container = JsonSerializer.Deserialize<SerializationContainer>(JsonDocument.ParseValue(ref reader).RootElement.GetRawText());
            //if (container.IsISerializable)
            //{
            //    using var stream = new MemoryStream();
            //    //new DataContractJsonSerializer(container.Type).WriteObject(stream, container.Value);
            //    new DataContractJsonSerializer(container.Type).ReadObject()
            //    //value = JsonDocument.Parse(stream).RootElement;
            //    JsonDocument.Parse(stream).WriteTo(writer);
            //}

            //return container;
            var document = JsonDocument.ParseValue(ref reader);
            var container = JsonSerializer.Deserialize<SerializationContainer>(document.RootElement.GetRawText());
            return container;
        }
        //Temperature.Parse(reader.GetString());

        public override void Write(Utf8JsonWriter writer, SerializationContainer container, JsonSerializerOptions options)
        {
            //using var stream = new MemoryStream();
            //bitmap.Save(stream, bitmap.RawFormat);
            //writer.WriteStartArray();
            //foreach (var byteElement in stream.ToArray())
            //{
            //    writer.WriteNumberValue(byteElement);
            //}
            //writer.WriteEndArray();

            if (container.IsISerializable)
            {
                var settings = new DataContractJsonSerializerSettings { DateTimeFormat = new DateTimeFormat("o") };
                using var stream = new MemoryStream();
                new DataContractJsonSerializer(container.Type, settings).WriteObject(stream, container.Value);
                //value = JsonDocument.Parse(stream).RootElement;
                //var value = new UTF8Encoding().GetString(stream.ToArray());
                //JsonDocument.Parse(value).WriteTo(writer);
                stream.Position = 0;

                //JsonDocument.Parse(stream).WriteTo(writer);

                //https://github.com/dotnet/runtime/issues/30632#issuecomment-523104461
                //var dictionary = JsonSerializer.Deserialize<Dictionary<string, object>>(File.OpenRead(m_fullName));

                writer.WriteStartObject();
                writer.WriteString(nameof(SerializationContainer.AssemblyQualifiedName), container.AssemblyQualifiedName);
                writer.WritePropertyName(nameof(SerializationContainer.Value));
                JsonDocument.Parse(stream).WriteTo(writer);
                writer.WriteBoolean(nameof(SerializationContainer.IsISerializable), true);
                writer.WriteEndObject();

                return;
            }

            JsonDocument.Parse(JsonSerializer.Serialize(container)).WriteTo(writer);
        }
    }
}

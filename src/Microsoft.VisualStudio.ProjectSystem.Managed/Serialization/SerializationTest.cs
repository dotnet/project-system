// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;
using System.Reflection;
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
            //var type = value.GetType();
            //var container = new BasicSerializationContainer
            //{
            //    AssemblyQualifiedName = type.AssemblyQualifiedName,
            //    Value = value
            //};
            var container = ContainerizeObjectHierarchy(value);
            var jsonString = JsonSerializer.Serialize(container);
            File.AppendAllLines(@"C:\Workspace\JsonTest-Serialize.txt", new[] { jsonString });
            writer.Write(jsonString);
            writer.Flush();
            stream.Position = 0;
        }

        private static readonly Type ObjectType = typeof(object);
        private static readonly Type ObjectArrayType = typeof(object[]);
        private static readonly Type StringType = typeof(string);

        private static SerializationContainer ContainerizeObjectHierarchy(object value)
        {
            var type = value.GetType();
            if(!type.IsPrimitive && type != StringType)
            {
                var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
                foreach (var property in properties)
                {
                    if (property.PropertyType == ObjectType && property.GetValue(value) is object propValue)
                    {
                        property.SetValue(value, ContainerizeObjectHierarchy(propValue));
                        continue;
                    }

                    if (property.PropertyType == ObjectArrayType && property.GetValue(value) is object?[] propArray)
                    {
                        //var propValue = property.GetValue(value) as object?[];
                        for (var i = 0; i < propArray.Length; ++i)
                        {
                            if (propArray[i] is not null)
                            {
                                propArray[i] = ContainerizeObjectHierarchy(propArray[i]!);
                            }
                        }
                    }
                }
            }

            return new SerializationContainer
            {
                AssemblyQualifiedName = type.AssemblyQualifiedName,
                Value = value
            };
        }

        //private static Type[] AllowedTypes = { typeof(int), typeof(bool), typeof(string), };

        public static object? Deserialize(Stream stream)
        {
            var streamAsString = new StreamReader(stream).ReadToEnd();
            File.AppendAllLines(@"C:\Workspace\JsonTest-Deserialize.txt", new[] { streamAsString });
            return DecontainerizeObjectHierarchy(streamAsString);
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

        private static object? DecontainerizeObjectHierarchy(string jsonString)
        {
            //SerializationContainer? container = JsonSerializer.Deserialize<SerializationContainer>(jsonString);
            if (JsonSerializer.Deserialize<SerializationContainer>(jsonString) is not SerializationContainer container)
            {
                return null;
            }

            //Type? type = Type.GetType(container.AssemblyQualifiedName);
            // Type filtering will use FullName from the type
            if (Type.GetType(container.AssemblyQualifiedName) is not Type type || container.Value is not JsonElement element)
            {
                throw new InvalidDataException();
            }

            var value = JsonSerializer.Deserialize(element.GetRawText(), type);
            if (!type.IsPrimitive && type != StringType)
            {
                var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
                foreach (var property in properties)
                {
                    if (property.PropertyType == ObjectType && property.GetValue(value) is JsonElement propElement)
                    {
                        property.SetValue(value, DecontainerizeObjectHierarchy(propElement.GetRawText()));
                        continue;
                    }

                    if (property.PropertyType == ObjectArrayType && property.GetValue(value) is object?[] propArray)
                    {
                        //var propValue = property.GetValue(value) as object?[];
                        //if (propValue is null)
                        //{
                        //    throw new InvalidDataException();
                        //}
                        //var objectArray = JsonSerializer.Deserialize<object?[]>(propValue.ToString());
                        for (var i = 0; i < propArray.Length; ++i)
                        {
                            if (propArray[i] is JsonElement propArrayElement)
                            {
                                propArray[i] = DecontainerizeObjectHierarchy(propArrayElement.GetRawText());
                            }
                        }
                    }
                }
            }

            return value;
        }
    }

    public class SerializationContainer
    {
        public string AssemblyQualifiedName { get; set; }
        public object Value { get; set; }
    }
}

// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
            // Utf8JsonReader/Writer are more efficient
            var writer = new StreamWriter(stream);
            var container = ContainerizeObjectHierarchy(value.GetType(), value);
            var jsonString = JsonSerializer.Serialize(container);
            File.AppendAllLines(@"C:\Workspace\JsonTest-Serialize.txt", new[] { jsonString });
            writer.Write(jsonString);
            writer.Flush();
            stream.Position = 0;
        }

        private static readonly Type ObjectType = typeof(object);
        private static readonly Type ObjectArrayType = typeof(object[]);
        //private static readonly Type StringType = typeof(string);

        private static PropertyInfo[] GetProperties(Type type) => type
            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy)
            .Where(pi => pi.CanRead && pi.CanWrite && pi.GetCustomAttribute<JsonIgnoreAttribute>() is null)
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
            if(!type.IsSimple())
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
            // Type filtering will use FullName from the type
            (Type type, object? value) = ConvertContainerJsonString(streamAsString);
            return DecontainerizeObjectHierarchy(type, value);
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

        private static (Type Type, object? Value) ConvertContainerJsonString(string jsonString)
        {
            if (JsonSerializer.Deserialize<SerializationContainer>(jsonString) is not SerializationContainer container
                || Type.GetType(container.AssemblyQualifiedName) is not Type type
                || container.Value is not JsonElement element)
            {
                throw new InvalidDataException();
            }

            return (type, JsonSerializer.Deserialize(element.GetRawText(), type));
        }

        private static object? DecontainerizeObjectHierarchy(Type type, object? value)
        {
            if (value is not null && !type.IsSimple())
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
                                if (propertyCollection[i] is JsonElement jsonElement)
                                {
                                    (elementType, elementValue) = ConvertContainerJsonString(jsonElement.GetRawText());
                                }
                                propertyCollection[i] = DecontainerizeObjectHierarchy(elementType, elementValue);
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
                        if(propertyValue is JsonElement jsonElement)
                        {
                            (propertyType, propertyValue) = ConvertContainerJsonString(jsonElement.GetRawText());
                        }
                        property.SetValue(value, DecontainerizeObjectHierarchy(propertyType, propertyValue));
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

    public class SerializationContainer
    {
        public string AssemblyQualifiedName { get; set; }
        public object Value { get; set; }
    }
}

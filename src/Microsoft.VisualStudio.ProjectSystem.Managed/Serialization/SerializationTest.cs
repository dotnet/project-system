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
            //var type = value.GetType();
            //var container = new BasicSerializationContainer
            //{
            //    AssemblyQualifiedName = type.AssemblyQualifiedName,
            //    Value = value
            //};
            var container = ContainerizeObjectHierarchy(value.GetType(), value);
            var jsonString = JsonSerializer.Serialize(container);
            File.AppendAllLines(@"C:\Workspace\JsonTest-Serialize.txt", new[] { jsonString });
            writer.Write(jsonString);
            writer.Flush();
            stream.Position = 0;
        }

        private static readonly Type ObjectType = typeof(object);
        private static readonly Type ObjectArrayType = typeof(object[]);
        private static readonly Type StringType = typeof(string);

        private static PropertyInfo[] GetProperties(Type type) => type
            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy)
            .Where(pi => pi.CanRead && pi.CanWrite && pi.GetCustomAttribute<JsonIgnoreAttribute>() is null)
            .ToArray();

        // TODO: IsInterface doesn't work since we cannot assign SerializationContainer to an interface property
        private static bool IsContainerizable(Type type) =>
            type == ObjectType; //|| type.IsInterface;

        // https://github.com/Azure/autorest.powershell/blob/67d99227e4cb8b03ca3168731bcbe23ae14d35e5/powershell/resources/psruntime/BuildTime/PsExtensions.cs#L37-L45
        private static bool IsSimple(this Type type) =>
            type.IsPrimitive
            || type.IsEnum
            || type == typeof(string)
            || type == typeof(decimal);

        // https://github.com/Azure/autorest.powershell/blob/67d99227e4cb8b03ca3168731bcbe23ae14d35e5/powershell/resources/psruntime/BuildTime/PsExtensions.cs#L16-L35
        public static bool TryUnwrap(ref Type type)
        {
            if (type.IsArray)
            {
                type = type.GetElementType();
                return true;
            }

            if (type.IsGenericType
                && (type.GetGenericTypeDefinition() == typeof(Nullable<>) || typeof(IEnumerable<>).IsAssignableFrom(type)))
            {
                type = type.GetGenericArguments().First();
                return true;
            }

            return false;
        }

        private static SerializationContainer ContainerizeObjectHierarchy(Type type, object value)
        {
            //var type = value.GetType();
            if(!type.IsSimple())
            {
                foreach (var property in GetProperties(type))
                {
                    var propertyType = property.PropertyType;
                    if (IsContainerizable(propertyType) && property.GetValue(value) is object propValue)
                    {
                        property.SetValue(value, ContainerizeObjectHierarchy(propValue.GetType(), propValue));
                        continue;
                    }

                    if (propertyType == ObjectArrayType && property.GetValue(value) is object?[] propArray)
                    {
                        //var propValue = property.GetValue(value) as object?[];
                        for (var i = 0; i < propArray.Length; ++i)
                        {
                            if (propArray[i] is object propArrayElement)
                            {
                                propArray[i] = ContainerizeObjectHierarchy(propArrayElement.GetType(), propArrayElement);
                            }
                        }
                        continue;
                    }

                    if (TryUnwrap(ref propertyType) && !propertyType.IsSimple() && property.GetValue(value) is IList propCollection)
                    {
                        for (var i = 0; i < propCollection.Count; ++i)
                        {
                            if (propCollection[i] is object propCollectionElement)
                            {
                                propCollection[i] = ContainerizeObjectHierarchy(propCollectionElement.GetType(), propCollectionElement).Value;
                            }
                        }
                        //foreach(var propItem in propCollection)
                        //{
                        //    if(propItem is not null)
                        //    {
                        //        ContainerizeObjectHierarchy(propItem.GetType(), propItem);
                        //    }
                        //}
                        continue;
                    }

                    if (!propertyType.IsSimple() && property.GetValue(value) is object propComplex)
                    {
                        property.SetValue(value, ContainerizeObjectHierarchy(propComplex.GetType(), propComplex).Value);
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
            //if (JsonSerializer.Deserialize<SerializationContainer>(jsonString) is not SerializationContainer container)
            //{
            //    return (null, null);
            //}

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
            ////SerializationContainer? container = JsonSerializer.Deserialize<SerializationContainer>(jsonString);
            //if (JsonSerializer.Deserialize<SerializationContainer>(jsonString) is not SerializationContainer container)
            //{
            //    return null;
            //}

            ////Type? type = Type.GetType(container.AssemblyQualifiedName);
            //// Type filtering will use FullName from the type
            //if (Type.GetType(container.AssemblyQualifiedName) is not Type type || container.Value is not JsonElement element)
            //{
            //    throw new InvalidDataException();
            //}

            //var value = JsonSerializer.Deserialize(element.GetRawText(), type);
            if (value is not null && !type.IsSimple())
            {
                foreach (var property in GetProperties(type))
                {
                    var propertyType = property.PropertyType;
                    if (IsContainerizable(propertyType) && property.GetValue(value) is JsonElement propElement)
                    {
                        (Type elementType, object? elementValue) = ConvertContainerJsonString(propElement.GetRawText());
                        property.SetValue(value, DecontainerizeObjectHierarchy(elementType, elementValue));
                        continue;
                    }

                    if (propertyType == ObjectArrayType && property.GetValue(value) is object?[] propArray)
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
                                (Type elementType, object? elementValue) = ConvertContainerJsonString(propArrayElement.GetRawText());
                                propArray[i] = DecontainerizeObjectHierarchy(elementType, elementValue);
                            }
                        }
                    }

                    if (TryUnwrap(ref propertyType) && !propertyType.IsSimple() && property.GetValue(value) is IList propCollection)
                    {
                        for (var i = 0; i < propCollection.Count; ++i)
                        {
                            if (propCollection[i] is JsonElement propCollectionElement)
                            {
                                (Type elementType, object? elementValue) = ConvertContainerJsonString(propCollectionElement.GetRawText());
                                propCollection[i] = DecontainerizeObjectHierarchy(elementType, elementValue);
                            }
                        }
                        //foreach (var propItem in propCollection)
                        //{
                        //    if (propItem is not null)
                        //    {
                        //        DecontainerizeObjectHierarchy(propertyType, propItem);
                        //    }
                        //}
                        continue;
                    }

                    if (!propertyType.IsSimple() && property.GetValue(value) is object propComplex)
                    {
                        property.SetValue(value, DecontainerizeObjectHierarchy(propertyType, propComplex));
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

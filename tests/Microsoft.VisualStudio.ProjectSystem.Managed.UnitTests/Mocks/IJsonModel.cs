// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Newtonsoft.Json.Linq;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal interface IJsonModel<out T>
    {
        T ToActualModel();
    }

    internal abstract class JsonModel<T> : IJsonModel<T>
    {
        public T FromJson(string jsonString)
        {
            var json = JObject.Parse(jsonString);
            var data = (IJsonModel<T>?)json.ToObject(GetType());
            Assumes.NotNull(data);
            return data.ToActualModel();
        }

        public abstract T ToActualModel();
    }
}

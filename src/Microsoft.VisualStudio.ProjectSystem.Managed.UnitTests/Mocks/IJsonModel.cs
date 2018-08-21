// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Newtonsoft.Json.Linq;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal interface IJsonModel<T>
    {
        T ToActualModel();
    }

    internal class JsonModel<T> : IJsonModel<T>
    {
        public T FromJson(string jsonString)
        {
            var json = JObject.Parse(jsonString);
            var data = (IJsonModel<T>)json.ToObject(GetType());
            return data.ToActualModel();
        }

        public virtual T ToActualModel()
        {
            return default;
        }
    }
}

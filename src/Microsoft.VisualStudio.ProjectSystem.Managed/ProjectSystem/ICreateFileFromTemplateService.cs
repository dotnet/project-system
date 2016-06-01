// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem
{
    public interface ICreateFileFromTemplateService
    {
        Task<bool> CreateFileAsync(string templateFile, IProjectTree parentNode, string specialFileName);
    }
}

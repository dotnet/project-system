// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.Build.Construction;

namespace Microsoft.VisualStudio.Build
{
    public sealed class MsBuildProjectFile : IDisposable
    {
        public string Filename { get; }

        public ProjectRootElement Project { get; }

        public MsBuildProjectFile(string xml = "<Project/>")
        {
            Filename = Path.GetTempFileName();
            using (var file = File.CreateText(Filename))
            {
                file.Write(xml);
            }

            Project = ProjectRootElement.Open(Filename);
        }

        public void Dispose()
        {
            if (File.Exists(Filename))
            {
                File.Delete(Filename);
            }
        }
    }
}

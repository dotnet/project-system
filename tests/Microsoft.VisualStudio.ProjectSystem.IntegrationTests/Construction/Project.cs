// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections;
using System.Xml.Linq;
using Microsoft.Test.Apex.VisualStudio.Solution;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    /// <summary>
    /// Defines a <c>.csproj</c> file to be created when using <see cref="ProjectLayoutTestBase"/>.
    /// </summary>
    public sealed class Project : IEnumerable
    {
        private static readonly Guid s_sdkProjectTypeGuid = Guid.Parse("9A19103F-16F7-4668-BE54-9A1E7A4F7556");

        private List<Project>? _referencedProjects;
        private List<PackageReference>? _packageReferences;
        private List<AssemblyReference>? _assemblyReferences;
        private List<IFile>? _files;

        public XElement XElement { get; } = new XElement("Project");

        public string Sdk { get; }
        public string TargetFrameworks { get; }

        public string ProjectName { get; } = "Project_" + Guid.NewGuid().ToString("N").Substring(0, 12);
        public string ProjectFileName => $"{ProjectName}.csproj";
        public string RelativeProjectFilePath => $"{ProjectName}\\{ProjectName}.csproj";

        public Guid ProjectGuid { get; } = Guid.NewGuid();
        public object ProjectTypeGuid => s_sdkProjectTypeGuid;

        public ProjectTestExtension? Extension { get; set; }

        public Project(string targetFrameworks, string sdk = "Microsoft.NET.Sdk")
        {
            TargetFrameworks = targetFrameworks;
            Sdk = sdk;
        }

        public void Save(string solutionRoot)
        {
            XElement.Add(new XAttribute("Sdk", Sdk));

            XElement.Add(new XElement(
                "PropertyGroup",
                new XElement("TargetFrameworks", TargetFrameworks)));

            if (_referencedProjects is not null)
            {
                XElement.Add(new XElement(
                    "ItemGroup",
                    _referencedProjects.Select(p => new XElement(
                        "ProjectReference",
                        new XAttribute("Include", $"..\\{p.RelativeProjectFilePath}")))));
            }

            if (_assemblyReferences is not null)
            {
                XElement.Add(new XElement(
                    "ItemGroup",
                    _assemblyReferences.Select(p => new XElement(
                        "Reference",
                        new XAttribute("Include", p.Name)))));
            }

            if (_packageReferences is not null)
            {
                XElement.Add(new XElement(
                    "ItemGroup",
                    _packageReferences.Select(p => new XElement(
                        "PackageReference",
                        new XAttribute("Include", p.PackageId),
                        new XAttribute("Version", p.Version)))));
            }

            var projectRoot = Path.Combine(solutionRoot, ProjectName);

            Directory.CreateDirectory(projectRoot);

            XElement.Save(Path.Combine(solutionRoot, RelativeProjectFilePath));

            if (_files is not null)
            {
                foreach (var file in _files)
                {
                    file.Save(projectRoot);
                }
            }
        }

        /// <summary>
        /// Adds a P2P (project-to-project) reference from this project to <paramref name="referree"/>.
        /// </summary>
        /// <param name="referree">The project to reference.</param>
        public void Add(Project referree)
        {
            _referencedProjects ??= new List<Project>();
            _referencedProjects.Add(referree);
        }

        public void Add(PackageReference packageReference)
        {
            _packageReferences ??= new List<PackageReference>();
            _packageReferences.Add(packageReference);
        }

        public void Add(AssemblyReference assemblyReference)
        {
            _assemblyReferences ??= new List<AssemblyReference>();
            _assemblyReferences.Add(assemblyReference);
        }

        public void Add(IFile file)
        {
            _files ??= new List<IFile>();
            _files.Add(file);
        }

        /// <summary>
        /// We only implement <see cref="IEnumerable"/> to support collection initialiser syntax.
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator() => throw new NotSupportedException();
    }

    public readonly struct PackageReference
    {
        public string PackageId { get; }
        public string Version { get; }

        public PackageReference(string packageId, string version)
        {
            PackageId = packageId;
            Version = version;
        }
    }

    public readonly struct AssemblyReference
    {
        public string Name { get; }

        public AssemblyReference(string name)
        {
            Name = name;
        }
    }

    public interface IFile
    {
        void Save(string projectRoot);
    }

    public sealed class CSharpClass : IFile
    {
        public string Name { get; }

        public CSharpClass(string name)
        {
            Name = name;
        }

        public void Save(string projectRoot)
        {
            var content = $@"class {Name} {{ }}";

            File.WriteAllText(Path.Combine(projectRoot, $"{Name}.cs"), content);
        }
    }
}

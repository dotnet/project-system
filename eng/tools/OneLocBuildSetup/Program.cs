// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.Json;
using CommandLine;

namespace OneLocBuildSetup
{
    [SuppressMessage("Style", "IDE0008:Use explicit type")]
    [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression")]
    [SuppressMessage("ReSharper", "SuggestVarOrType_BuiltInTypes")]
    [SuppressMessage("ReSharper", "SuggestVarOrType_Elsewhere")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal class Program
    {
        private const string XlfExtension = ".xlf";
        // Spanish language files have no special meaning. Any language can be used as a template file for OneLocBuild.
        // For details: https://ceapex.visualstudio.com/CEINTL/_wiki/wikis/CEINTL.wiki/1450/OneLocBuild-Non-Enu-source-file-support-(workaround)
        private const string SpanishXlfExtension = ".es" + XlfExtension;
        private const string SourceFolderName = "src";
        private const string LocalizationFolderName = "loc";
        private const string ProjectFileName = "LocProject.json";

        public static void Main(string[] args) => Parser.Default.ParseArguments<Arguments>(args).WithParsed(RunSetup);

        private static void RunSetup(Arguments args)
        {
            var srcPath = Path.Combine(args.RepositoryPath, SourceFolderName);
            var xlfPaths = CreateTemplateFiles(srcPath).ToArray();
            CreateLocProject(xlfPaths, srcPath, args.RepositoryPath, args.OutputPath);
        }

        // Copies existing language-specific XLF files to have a language-neutral filename. These files are used as a template for OneLocBuild.
        // For details: https://ceapex.visualstudio.com/CEINTL/_wiki/wikis/CEINTL.wiki/1450/OneLocBuild-Non-Enu-source-file-support-(workaround)
        private static IEnumerable<string> CreateTemplateFiles(string srcPath)
        {
            var esXlfPaths = Directory.GetFiles(srcPath, $"*{SpanishXlfExtension}", SearchOption.AllDirectories);
            var filePaths = esXlfPaths.Select(es => (esXlfPath: es, xlfPath: es.Replace(SpanishXlfExtension, XlfExtension))).ToArray();
            foreach ((string esXlfPath, string xlfPath) in filePaths)
            {
                Console.WriteLine($"Creating {xlfPath}");
                File.Copy(esXlfPath, xlfPath, true);
                yield return xlfPath;
            }
        }

        // Creates a LocProject JSON file based on the provided XLF files, and writes it to disk based on the provided output path.
        // For details: https://ceapex.visualstudio.com/CEINTL/_wiki/wikis/CEINTL.wiki/107/Localization-with-OneLocBuild-Task?anchor=author-localization-project-file
        private static void CreateLocProject(string[] xlfPaths, string srcPath, string repositoryPath, string outputPath)
        {
            var locProject = new LocProject(xlfPaths
                .Select(xp => (
                    xlfPath: xp.Remove(0, repositoryPath.Length).TrimStart(Path.DirectorySeparatorChar),
                    projectName: GetProjectName(xp, srcPath)))
                .GroupBy(p => p.projectName)
                .OrderBy(pg => pg.Key)
                .Select(pg => new Project(pg
                    .Select(p => CreateLocItem(p.xlfPath))
                    .OrderBy(li => li.SourceFile)
                    .ToArray()))
                .ToArray());

            var locPath = Path.Combine(outputPath, LocalizationFolderName);
            Directory.CreateDirectory(locPath);
            var locProjectPath = Path.Combine(locPath, ProjectFileName);
            var locProjectJson = JsonSerializer.Serialize(locProject, new JsonSerializerOptions { WriteIndented = true });
            Console.WriteLine($"Creating {locProjectPath}");
            File.WriteAllText(locProjectPath, locProjectJson);
        }

        private static string GetProjectName(string xlfPath, string srcPath) => xlfPath
            .Remove(0, srcPath.Length)
            .Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault() ?? string.Empty;

        private static LocItem CreateLocItem(string xlfPath)
        {
            var outputPath = Path.GetDirectoryName(xlfPath) ?? string.Empty;
            var lclPath = Path.Combine(outputPath, "{Lang}", Path.GetFileName(xlfPath).Replace(XlfExtension, $"{XlfExtension}.lcl"));
            return new LocItem(xlfPath, outputPath, lclPath);
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.RoslynLogging
{
    internal static class RoslynWorkspaceStructureLogger
    {
        private static int s_nextCompilationId;
        private static readonly ConditionalWeakTable<Compilation, StrongBox<int>> s_compilationIds = new ConditionalWeakTable<Compilation, StrongBox<int>>();

        public static void ShowSaveDialogAndLog(IServiceProvider serviceProvider)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var saveDialog = new SaveFileDialog()
            {
                Filter = "Zip Files (*.zip)|*.zip"
            };

            if (saveDialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            Log(serviceProvider, saveDialog.FileName);
        }

        public static void Log(IServiceProvider serviceProvider, string path)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var componentModel = (IComponentModel)serviceProvider.GetService(typeof(SComponentModel));
            Assumes.Present(componentModel);
            var dte = (EnvDTE.DTE)serviceProvider.GetService(typeof(SDTE));

            VisualStudioWorkspace workspace = componentModel.GetService<VisualStudioWorkspace>();
            Solution solution = workspace.CurrentSolution;

            var threadedWaitDialog = (IVsThreadedWaitDialog3)serviceProvider.GetService(typeof(SVsThreadedWaitDialog));
            Assumes.Present(threadedWaitDialog);
            var threadedWaitCallback = new ThreadedWaitCallback();

            int projectsProcessed = 0;
            threadedWaitDialog.StartWaitDialogWithCallback(RoslynLoggingResources.ProjectSystemTools, RoslynLoggingResources.LoggingRoslynWorkspaceStructure, null, null, null, true, 0, true, solution.ProjectIds.Count, 0, threadedWaitCallback);

            try
            {
                var document = new XDocument();
                var workspaceElement = new XElement("workspace");
                document.Add(workspaceElement);

                foreach (Project project in solution.GetProjectDependencyGraph().GetTopologicallySortedProjects(threadedWaitCallback.CancellationToken).Select(solution.GetProject))
                {
                    // Dump basic project attributes
                    var projectElement = new XElement("project");
                    workspaceElement.Add(projectElement);

                    projectElement.SetAttributeValue("id", SanitizePath(project.Id.ToString()));
                    projectElement.SetAttributeValue("name", project.Name);
                    projectElement.SetAttributeValue("assemblyName", project.AssemblyName);
                    projectElement.SetAttributeValue("language", project.Language);
                    projectElement.SetAttributeValue("path", SanitizePath(project.FilePath ?? "(none)"));
                    projectElement.SetAttributeValue("outputPath", SanitizePath(project.OutputFilePath ?? "(none)"));

                    bool? hasSuccesfullyLoaded = TryGetHasSuccessfullyLoaded(project, threadedWaitCallback.CancellationToken);

                    if (hasSuccesfullyLoaded.HasValue)
                    {
                        projectElement.SetAttributeValue("hasSuccessfullyLoaded", hasSuccesfullyLoaded.Value);
                    }

                    // Dump MSBuild <Reference> nodes
                    if (project.FilePath != null)
                    {
                        var msbuildProject = XDocument.Load(project.FilePath);
                        var msbuildNamespace = XNamespace.Get("http://schemas.microsoft.com/developer/msbuild/2003");

                        var msbuildReferencesElement = new XElement("msbuildReferences");
                        projectElement.Add(msbuildReferencesElement);

                        msbuildReferencesElement.Add(msbuildProject.Descendants(msbuildNamespace + "ProjectReference"));
                        msbuildReferencesElement.Add(msbuildProject.Descendants(msbuildNamespace + "Reference"));
                        msbuildReferencesElement.Add(msbuildProject.Descendants(msbuildNamespace + "ReferencePath"));
                    }

                    // Dump DTE references
                    VSLangProj.VSProject langProjProject = TryFindLangProjProject(dte, project);

                    if (langProjProject != null)
                    {
                        var dteReferences = new XElement("dteReferences");
                        projectElement.Add(dteReferences);

                        foreach (VSLangProj.Reference reference in langProjProject.References.Cast<VSLangProj.Reference>())
                        {
                            if (reference.SourceProject != null)
                            {
                                dteReferences.Add(new XElement("projectReference", new XAttribute("projectName", reference.SourceProject.Name)));
                            }
                            else
                            {
                                dteReferences.Add(new XElement("metadataReference",
                                    reference.Path != null ? new XAttribute("path", SanitizePath(reference.Path)) : null,
                                    new XAttribute("name", reference.Name)));
                            }
                        }
                    }

                    // Dump the actual metadata references in the workspace
                    var workspaceReferencesElement = new XElement("workspaceReferences");
                    projectElement.Add(workspaceReferencesElement);

                    foreach (MetadataReference metadataReference in project.MetadataReferences)
                    {
                        workspaceReferencesElement.Add(CreateElementForPortableExecutableReference(metadataReference));
                    }

                    // Dump project references in the workspace
                    foreach (ProjectReference projectReference in project.AllProjectReferences)
                    {
                        var referenceElement = new XElement("projectReference", new XAttribute("id", SanitizePath(projectReference.ProjectId.ToString())));

                        if (!project.ProjectReferences.Contains(projectReference))
                        {
                            referenceElement.SetAttributeValue("missingInSolution", "true");
                        }

                        workspaceReferencesElement.Add(referenceElement);
                    }

                    projectElement.Add(new XElement("workspaceDocuments", CreateElementsForDocumentCollection(project.Documents, "document")));
                    projectElement.Add(new XElement("workspaceAdditionalDocuments", CreateElementsForDocumentCollection(project.AdditionalDocuments, "additionalDocuments")));

                    // Read AnalyzerConfigDocuments via reflection, as our target version may not be on a Roslyn
                    // new enough to support it.
                    System.Reflection.PropertyInfo analyzerConfigDocumentsProperty = project.GetType().GetProperty("AnalyzerConfigDocuments");

                    if (analyzerConfigDocumentsProperty != null)
                    {
                        var analyzerConfigDocuments = (IEnumerable<TextDocument>)analyzerConfigDocumentsProperty.GetValue(project);
                        projectElement.Add(new XElement("workspaceAnalyzerConfigDocuments", CreateElementsForDocumentCollection(analyzerConfigDocuments, "analyzerConfigDocument")));
                    }

                    // Dump references from the compilation; this should match the workspace but can help rule out
                    // cross-language reference bugs or other issues like that
#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits -- this is fine since it's a Roslyn API
                    Compilation compilation = project.GetCompilationAsync(threadedWaitCallback.CancellationToken).Result;
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits

                    if (compilation != null)
                    {
                        var compilationReferencesElement = new XElement("compilationReferences");
                        projectElement.Add(compilationReferencesElement);

                        foreach (MetadataReference reference in compilation.References)
                        {
                            compilationReferencesElement.Add(CreateElementForPortableExecutableReference(reference));
                        }

                        projectElement.Add(CreateElementForCompilation(compilation));

                        // Dump all diagnostics
                        var diagnosticsElement = new XElement("diagnostics");
                        projectElement.Add(diagnosticsElement);

                        foreach (Diagnostic diagnostic in compilation.GetDiagnostics(threadedWaitCallback.CancellationToken))
                        {
                            diagnosticsElement.Add(
                                new XElement("diagnostic",
                                    new XAttribute("severity", diagnostic.Severity.ToString()),
                                    diagnostic.GetMessage()));
                        }
                    }

                    projectsProcessed++;

                    threadedWaitDialog.UpdateProgress(null, null, null, projectsProcessed, solution.ProjectIds.Count, false, out bool cancelled);
                }

                File.Delete(path);

                using ZipArchive zipFile = ZipFile.Open(path, ZipArchiveMode.Create);
                ZipArchiveEntry zipFileEntry = zipFile.CreateEntry("Workspace.xml", CompressionLevel.Fastest);
                using Stream stream = zipFileEntry.Open();
                document.Save(stream);
            }
            catch (OperationCanceledException)
            {
                // They cancelled
            }
            finally
            {
                threadedWaitDialog.EndWaitDialog(out int cancelled);
            }
        }

        private static bool? TryGetHasSuccessfullyLoaded(Project project, CancellationToken cancellationToken)
        {
            // This method has not been made a public API, but is useful for analyzing some issues. Since it's non-public we'll
            // be careful to deal with it missing if/when it's updated.
            System.Reflection.MethodInfo method = project.GetType().GetMethod("HasSuccessfullyLoadedAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (method == null)
            {
                return null;
            }

            var task = method.Invoke(project, new object[] { cancellationToken }) as Task<bool>;

#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits -- this is fine since it's a Roslyn API

            task.Wait(cancellationToken);

            return task.Result;

#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits
        }

        private static VSLangProj.VSProject TryFindLangProjProject(EnvDTE.DTE dte, Project project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            EnvDTE.Project dteProject = dte.Solution.Projects.Cast<EnvDTE.Project>().FirstOrDefault(
                p =>
                {
                    ThreadHelper.ThrowIfNotOnUIThread();
                    try
                    {
                        return string.Equals(p.FullName, project.FilePath, StringComparison.OrdinalIgnoreCase);
                    }
                    catch (NotImplementedException)
                    {
                        // Some EnvDTE.Projects will throw on p.FullName, so just bail in that case.
                        return false;
                    }
                });

            return dteProject?.Object as VSLangProj.VSProject;
        }

        private static string SanitizePath(string s)
        {
            return ReplacePathComponent(s, Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "%USERPROFILE%");
        }

        /// <summary>
        /// Equivalent to string.Replace, but uses OrdinalIgnoreCase for matching.
        /// </summary>
        private static string ReplacePathComponent(string s, string oldValue, string newValue)
        {
            while (true)
            {
                int index = s.IndexOf(oldValue, StringComparison.OrdinalIgnoreCase);
                if (index == -1)
                {
                    return s;
                }

                s = s.Substring(0, index) + newValue + s.Substring(index + oldValue.Length);
            }
        }

        private static XElement CreateElementForPortableExecutableReference(MetadataReference reference)
        {
            var aliasesAttribute = new XAttribute("aliases", string.Join(",", reference.Properties.Aliases));

            if (reference is CompilationReference compilationReference)
            {
                return new XElement("compilationReference",
                    aliasesAttribute,
                    CreateElementForCompilation(compilationReference.Compilation));
            }
            else if (reference is PortableExecutableReference portableExecutableReference)
            {
                return new XElement("peReference",
                    new XAttribute("file", SanitizePath(portableExecutableReference.FilePath ?? "(none)")),
                    new XAttribute("display", SanitizePath(portableExecutableReference.Display)),
                    aliasesAttribute);
            }
            else
            {
                return new XElement("metadataReference", new XAttribute("display", SanitizePath(reference.Display)));
            }
        }

        private static XElement CreateElementForCompilation(Compilation compilation)
        {
            if (!s_compilationIds.TryGetValue(compilation, out StrongBox<int> compilationId))
            {
                compilationId = new StrongBox<int>(s_nextCompilationId++);
                s_compilationIds.Add(compilation, compilationId);
            }

            var namespaces = new Queue<INamespaceSymbol>();
            var typesElement = new XElement("types");

            namespaces.Enqueue(compilation.Assembly.GlobalNamespace);

            while (namespaces.Count > 0)
            {
                INamespaceSymbol @ns = namespaces.Dequeue();

                foreach (INamedTypeSymbol type in @ns.GetTypeMembers())
                {
                    typesElement.Add(new XElement("type", new XAttribute("name", type.ToDisplayString())));
                }

                foreach (INamespaceSymbol childNamespace in @ns.GetNamespaceMembers())
                {
                    namespaces.Enqueue(childNamespace);
                }
            }

            return new XElement("compilation",
                new XAttribute("objectId", compilationId.Value),
                new XAttribute("assemblyIdentity", compilation.Assembly.Identity.ToString()),
                typesElement);
        }

        public static IEnumerable<XElement> CreateElementsForDocumentCollection(IEnumerable<TextDocument> documents, string elementName)
        {
            foreach (TextDocument document in documents)
            {
                yield return new XElement(elementName, new XAttribute("file", SanitizePath(document.FilePath ?? "(none)")));
            }
        }

        private sealed class ThreadedWaitCallback : IVsThreadedWaitDialogCallback
        {
            private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

            public CancellationToken CancellationToken
            {
                get { return _cancellationTokenSource.Token; }
            }

            public void OnCanceled()
            {
                _cancellationTokenSource.Cancel();
            }
        }
    }
}

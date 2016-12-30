
// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

// workaround for https://github.com/dotnet/roslyn-analyzers/issues/955
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(
                "Reliability",
                "RS0006:Do not mix attributes from different versions of MEF",
                Justification = "<Pending>")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0030:Use null propagation", Justification = "https://github.com/dotnet/roslyn/issues/161", Scope = "member", Target = "~M:Microsoft.VisualStudio.ProjectSystem.VS.Editor.TempFileTextBufferManager.#ctor(Microsoft.VisualStudio.ProjectSystem.UnconfiguredProject,Microsoft.VisualStudio.ProjectSystem.VS.Editor.IProjectXmlAccessor,Microsoft.VisualStudio.Editor.IVsEditorAdaptersFactoryService,Microsoft.VisualStudio.Text.ITextDocumentFactoryService,Microsoft.VisualStudio.ProjectSystem.VS.UI.IVsShellUtilitiesHelper,Microsoft.VisualStudio.IO.IFileSystem,Microsoft.VisualStudio.ProjectSystem.IProjectThreadingService,System.IServiceProvider)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0030:Use null propagation", Justification = "https://github.com/dotnet/roslyn/issues/1611", Scope = "member", Target = "~M:Microsoft.VisualStudio.ProjectSystem.VS.Editor.Listeners.TempFileBufferStateListener.#ctor(Microsoft.VisualStudio.ProjectSystem.VS.Editor.IEditorStateModel,Microsoft.VisualStudio.Editor.IVsEditorAdaptersFactoryService,Microsoft.VisualStudio.Text.ITextDocumentFactoryService,Microsoft.VisualStudio.ProjectSystem.IProjectThreadingService,Microsoft.VisualStudio.ProjectSystem.VS.UI.IVsShellUtilitiesHelper,System.IServiceProvider)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0030:Use null propagation", Justification = "https://github.com/dotnet/roslyn/issues/16114", Scope = "member", Target = "~M:Microsoft.VisualStudio.ProjectSystem.VS.Editor.FrameOpenCloseListener.#ctor(System.IServiceProvider,Microsoft.VisualStudio.ProjectSystem.VS.Editor.IEditorStateModel,Microsoft.VisualStudio.ProjectSystem.IProjectThreadingService,Microsoft.VisualStudio.ProjectSystem.UnconfiguredProject)")]


// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.LanguageServices.ProjectSystem;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices;

/// <summary>
/// Models exports of <see cref="IWorkspaceUpdateHandler"/> for a given project slice.
/// </summary>
/// <remarks>
/// Several handlers are stateful with respect to the <see cref="IWorkspaceProjectContext"/> they are populating.
/// For this reason, we use an <see cref="ExportFactory{T}"/> to create instances of each handler implementation
/// for the current slice. This allows them to travel together, and be disposed together.
/// </remarks>
internal sealed class UpdateHandlers : IDisposable
{
    public ImmutableArray<ICommandLineHandler> CommandLineHandlers { get; }
    public ImmutableArray<IProjectEvaluationHandler> EvaluationHandlers { get; }
    public ImmutableArray<ISourceItemsHandler> SourceItemHandlers { get; }

    public ImmutableArray<string> EvaluationRules { get; }

    private readonly ExportLifetimeContext<IWorkspaceUpdateHandler>[] _lifetimes;

    private int _disposed;

    public UpdateHandlers(ExportFactory<IWorkspaceUpdateHandler>[] factories)
    {
        _lifetimes = factories.SelectArray(factory => factory.CreateExport());

        CommandLineHandlers = Create<ICommandLineHandler>();
        EvaluationHandlers = Create<IProjectEvaluationHandler>();
        SourceItemHandlers = Create<ISourceItemsHandler>();

        EvaluationRules = EvaluationHandlers
            .Select(handler => handler.ProjectEvaluationRule) // Each handler specifies the evaluation rule it wants
            .Append(ConfigurationGeneral.SchemaName) // Needed when creating IWorkspaceProjectContext
            .Distinct(StringComparers.RuleNames)
            .ToImmutableArray();

        ImmutableArray<T> Create<T>()
        {
            ImmutableArray<T>.Builder builder = ImmutableArray.CreateBuilder<T>();

            foreach (ExportLifetimeContext<IWorkspaceUpdateHandler> lifetime in _lifetimes)
            {
                if (lifetime.Value is T handler)
                {
                    builder.Add(handler);
                }
            }

            return builder.ToImmutable();
        }
    }

    public void Dispose()
    {
        if (Interlocked.CompareExchange(ref _disposed, 1, 0) == 0)
        {
            foreach (IDisposable lifetime in _lifetimes)
            {
                lifetime.Dispose();
            }
        }
    }
}

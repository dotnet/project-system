// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Snapshot;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Subscriptions.MSBuildDependencies;

/// <summary>
/// Base class for our implementations of <see cref="IMSBuildDependencyFactory"/>. An implementation
/// is exported for each kind of dependency (assembly, package, project, etc.).
/// </summary>
/// <remarks>
/// <para>
/// There is no standard set of metadata on dependencies declared via MSBuild, so instances of this class
/// override methods as needed to obtain the set of properties we need in order to treat dependencies
/// uniformly. The following virtual methods exist for this purpose, and each provides a sensible default:
/// <list type="bullet">
///   <item><see cref="GetUnresolvedCaption"/></item>
///   <item><see cref="GetResolvedCaption"/></item>
///   <item><see cref="GetOriginalItemSpec"/></item>
///   <item><see cref="UpdateTreeFlags"/></item>
///   <item><see cref="GetDiagnosticLevel"/></item>
/// </list>
/// </para>
/// <para>
/// As project data is received via project evaluation and design-time build data, these updates are
/// provided to the following set of methods, all of which are called by <see cref="MSBuildDependencyCollection"/>
/// as needed:
/// <list type="bullet">
///   <item><see cref="CreateDependency"/></item>
///   <item><see cref="TryUpdate"/></item>
///   <item><see cref="TryMakeUnresolved"/></item>
/// </list>
/// </para>
/// <para>
/// Instances of this factory class create instances of <see cref="MSBuildDependencyCollection"/> via its
/// <see cref="CreateCollection"/> method, which is a stateful object that maintains a set of dependencies
/// in response to project updates over time.
/// </para>
/// </remarks>
internal abstract class MSBuildDependencyFactoryBase : IMSBuildDependencyFactory
{
    /// <summary>
    /// Gets the name of the rule (schema) that obtains item data from evaluations.
    /// </summary>
    /// <remarks>
    /// This value is used for data subscription, and also for browse object population.
    /// </remarks>
    public abstract string UnresolvedRuleName { get; }

    /// <summary>
    /// Gets the name of the rule (schema) that obtains item data from design-time builds.
    /// </summary>
    /// <remarks>
    /// This value is used for data subscription, and also for browse object population.
    /// </remarks>
    public abstract string ResolvedRuleName { get; }

    /// <summary>
    /// Gets metadata about the type of dependency produced by this factory.
    /// </summary>
    public abstract DependencyGroupType DependencyGroupType { get; }

    /// <summary>
    /// Gets the MSBuild item type specified in the rule's DataSource.
    /// </summary>
    /// <remarks>
    /// This value is the same for both unresolved and resolved rules.
    /// </remarks>
    public abstract string SchemaItemType { get; }

    /// <summary>
    /// Gets the icon associated with the dependency when <see cref="IDependency.DiagnosticLevel"/> is <see cref="DiagnosticLevel.None"/>.
    /// and <see cref="MSBuildDependency.IsImplicit"/> is <see langword="false"/>.
    /// </summary>
    public abstract ProjectImageMoniker Icon { get; }

    /// <summary>
    /// Gets the icon associated with the dependency when <see cref="IDependency.DiagnosticLevel"/> is <see cref="DiagnosticLevel.Warning"/>.
    /// </summary>
    public abstract ProjectImageMoniker IconWarning { get; }

    /// <summary>
    /// Gets the icon associated with the dependency when <see cref="IDependency.DiagnosticLevel"/> is <see cref="DiagnosticLevel.Error"/>.
    /// </summary>
    public abstract ProjectImageMoniker IconError { get; }

    /// <summary>
    /// Gets the icon associated with the dependency when <see cref="IDependency.DiagnosticLevel"/> is <see cref="DiagnosticLevel.None"/>
    /// and <see cref="MSBuildDependency.IsImplicit"/> is <see langword="true"/>.
    /// </summary>
    public abstract ProjectImageMoniker IconImplicit { get; }

    /// <summary>
    /// Gets an object that efficiently produces the correct set of <see cref="ProjectTreeFlags"/> for dependencies produced by this factory.
    /// </summary>
    public abstract DependencyFlagCache FlagCache { get; }

    /// <summary>
    /// Controls whether a resolved item must have a corresponding evaluated item
    /// in order to be considered.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For most rules we require the item to be present in evaluation data as well
    /// as design-time data to be considered resolved. In general, all items should
    /// be provided to the tree by evaluation. However currently Analyzers are only
    /// available when resolved during design-time builds.
    /// </para>
    /// <para>
    /// https://github.com/dotnet/project-system/issues/4782 tracks making these
    /// items available during evaluation.
    /// </para>
    /// </remarks>
    public virtual bool ResolvedItemRequiresEvaluatedItem => true;

    public MSBuildDependencyCollection CreateCollection()
    {
        return new MSBuildDependencyCollection(this);
    }

    // The following methods provide hooks for factories to override how item metadata is transformed into data for use in the tree.
    // Ideally all items would have consistent metadata, and this would not be needed. Perhaps one day we will unify these such that
    // this logic exists in MSBuild. But for now, it's here.

    /// <summary>
    /// Gets the display string for the as-yet unresolved form of this dependency.
    /// </summary>
    /// <param name="itemSpec">The item spec of the unresolved dependency item.</param>
    /// <param name="unresolvedProperties">The full set of properties on the unresolved item.</param>
    /// <returns></returns>
    protected internal virtual string GetUnresolvedCaption(string itemSpec, IImmutableDictionary<string, string> unresolvedProperties)
    {
        return itemSpec;
    }

    /// <summary>
    /// Gets the display string for the resolved form of this dependency.
    /// </summary>
    /// <param name="itemSpec">The item spec of the resolved dependency item.</param>
    /// <param name="originalItemSpec">The original item spec, where available, sourced via <c>OriginalItemSpec</c> metadata on the resolved item.</param>
    /// <param name="resolvedProperties">The full set of properties on the resolved item.</param>
    /// <returns></returns>
    protected internal virtual string GetResolvedCaption(string itemSpec, string? originalItemSpec, IImmutableDictionary<string, string> resolvedProperties)
    {
        return originalItemSpec ?? itemSpec;
    }

    /// <summary>
    /// Gets the item spec of the unresolved item from the resolved item.
    /// </summary>
    /// <remarks>
    /// The default convention is that the resolved item stores the unresolved item's spec in <c>OriginalItemSpec</c> metadata.
    /// Subclasses may override this behavior to control that.
    /// </remarks>
    /// <param name="resolvedItemSpec">The item spec of the resolved item.</param>
    /// <param name="resolvedProperties">The properties of the resolved item.</param>
    /// <returns></returns>
    protected internal virtual string? GetOriginalItemSpec(string resolvedItemSpec, IImmutableDictionary<string, string> resolvedProperties)
    {
        return resolvedProperties.GetStringProperty(ProjectItemMetadata.OriginalItemSpec) ?? resolvedItemSpec;
    }

    /// <summary>
    /// Modifies the <see cref="ProjectTreeFlags"/> for the dependency.
    /// </summary>
    /// <remarks>
    /// When not overridden, this method just returns <paramref name="flags"/> unchanged.
    /// </remarks>
    /// <param name="id">The identity of the dependency.</param>
    /// <param name="flags">The default flags for the item, which this method may modify as needed.</param>
    /// <returns>The flags to use for the dependency.</returns>
    protected internal virtual ProjectTreeFlags UpdateTreeFlags(string id, ProjectTreeFlags flags)
    {
        return flags;
    }

    /// <summary>
    /// Gets the <see cref="DiagnosticLevel"/> to set for a dependency, based on its resolved state and item properties.
    /// </summary>
    /// <param name="isResolved"><see langword="true"/> if the dependency is resolved, <see langword="false"/> if it is unresolved, or <see langword="null"/> if the status is not yet determined.</param>
    /// <param name="hasBuildError">Whether the MSBuild invocation that produced this data also encountered an error or not.</param>
    /// <param name="properties">The properties of the item.</param>
    /// <param name="defaultLevel">The diagnostic level to use when the property is either missing or empty. Intended to receive a dependency's current diagnostic level when an evaluation-only update is being processed.</param>
    /// <returns></returns>
    protected internal virtual DiagnosticLevel GetDiagnosticLevel(bool? isResolved, bool hasBuildError, IImmutableDictionary<string, string> properties, DiagnosticLevel defaultLevel = DiagnosticLevel.None)
    {
        return (isResolved, hasBuildError, properties.GetDiagnosticLevel(defaultLevel)) switch
        {
            (false, false, DiagnosticLevel.None) => DiagnosticLevel.Warning,
            (_, _, DiagnosticLevel level) => level
        };
    }

    /// <summary>
    /// Constructs a new <see cref="MSBuildDependency"/> based on the provided project data.
    /// This method understands the significance of different presentations of that data between
    /// evaluation and build, and populates the constructed dependency object accordingly.
    /// </summary>
    /// <param name="id">
    /// Identifies the dependency. Equal to the item spec of the evaluation item.
    /// For build items, determined by <see cref="GetOriginalItemSpec"/>.
    /// </param>
    /// <param name="evaluation">Evaluation item data, if present.</param>
    /// <param name="build">Evaluation item data, if present.</param>
    /// <param name="projectFullPath">Full path to the project file.</param>
    /// <param name="hasBuildError">Whether the evaluation or build that produced this update ended with an error.</param>
    /// <param name="isEvaluationOnlySnapshot">
    /// Whether this update contained only evaluation data. If <see langword="false"/>, this update
    /// came from the JointRule source.
    /// </param>
    /// <returns>
    /// The new dependency, or <see langword="null"/> if no dependency should exist for the given data.
    /// </returns>
    internal MSBuildDependency? CreateDependency(
        string id,
        (string ItemSpec, IImmutableDictionary<string, string> Properties)? evaluation,
        (string ItemSpec, IImmutableDictionary<string, string> Properties)? build,
        string projectFullPath,
        bool hasBuildError,
        bool isEvaluationOnlySnapshot)
    {
        if (build is not null)
        {
            if (evaluation is null && ResolvedItemRequiresEvaluatedItem)
            {
                // We must have an evaluation item to create this dependency. Disallow the creation of this dependency.
                // Ideally we would not be passed such items, as they represent redundant items retained in snapshot
                // data.
                // TODO FrameworkReference items resolve to create many assembly references that are rejected at this point -- they should be rejected earlier, in the DTB targets
                return null;
            }

            IImmutableDictionary<string, string> properties = build.Value.Properties;
            string itemSpec = build.Value.ItemSpec;
            bool? isVisible = properties.GetBoolProperty(ProjectItemMetadata.Visible);

            if (isVisible is false)
            {
                // This dependency is not visible and should be removed.
                return null;
            }

#if DEBUG
            string? originalItemSpec = GetOriginalItemSpec(itemSpec, properties);
            Assumes.True(StringComparers.DependencyIds.Equals(id, originalItemSpec));
            Assumes.NotNull(originalItemSpec);
#endif

            // This is a resolved dependency.
            bool isImplicit = IsImplicit(projectFullPath, evaluation?.Properties, properties);
            DiagnosticLevel diagnosticLevel = GetDiagnosticLevel(isResolved: true, hasBuildError, properties);
            string caption = GetResolvedCaption(itemSpec, id, properties);
            ProjectImageMoniker icon = GetIcon(isImplicit, diagnosticLevel);
            ProjectTreeFlags flags = UpdateTreeFlags(itemSpec, FlagCache.Get(isResolved: true, isImplicit));

            return new MSBuildDependency(
                factory: this,
                id: id,
                caption: caption,
                icon: icon,
                flags: flags,
                diagnosticLevel: diagnosticLevel,
                isResolved: true,
                isImplicit: isImplicit,
                filePath: itemSpec,
                browseObjectProperties: properties);
        }
        else if (evaluation is not null)
        {
            IImmutableDictionary<string, string> properties = evaluation.Value.Properties;
            string itemSpec = evaluation.Value.ItemSpec;
            bool? isVisible = properties.GetBoolProperty(ProjectItemMetadata.Visible);

            if (isVisible is false)
            {
                // This dependency is not visible and should be removed.
                return null;
            }

#if DEBUG
            Assumes.True(StringComparers.DependencyIds.Equals(id, itemSpec));
#endif

            bool isImplicit = IsImplicit(projectFullPath, properties, buildProperties: null);
            DiagnosticLevel diagnosticLevel = GetDiagnosticLevel(isResolved: isEvaluationOnlySnapshot, hasBuildError, properties);
            string caption = GetUnresolvedCaption(itemSpec, properties);
            ProjectImageMoniker icon = GetIcon(isImplicit, diagnosticLevel);
            ProjectTreeFlags flags = UpdateTreeFlags(itemSpec, FlagCache.Get(isResolved: false, isImplicit));

            return new MSBuildDependency(
                factory: this,
                id: itemSpec,
                caption: caption,
                icon: icon,
                flags: flags,
                diagnosticLevel: diagnosticLevel,
                isResolved: isEvaluationOnlySnapshot ? null : false, // Pass null when resolved state is not yet truly determined.
                isImplicit: isImplicit,
                filePath: itemSpec,
                browseObjectProperties: properties);
        }
        else
        {
            throw Assumes.NotReachable();
        }
    }

    /// <summary>
    /// Update an existing <see cref="MSBuildDependency"/> instances with updated MSBuild item
    /// data from the project.
    /// </summary>
    /// <param name="dependency">The dependency to update.</param>
    /// <param name="evaluation">Evaluation item data, if present.</param>
    /// <param name="build">Evaluation item data, if present.</param>
    /// <param name="projectFullPath">Full path to the project file.</param>
    /// <param name="isEvaluationOnlySnapshot">
    /// Whether this update contained only evaluation data. If <see langword="false"/>, this update
    /// came from the JointRule source.
    /// </param>
    /// <param name="hasBuildError">Whether the evaluation or build that produced this update ended with an error.</param>
    /// <param name="updated">
    /// The updated dependency. May be <see langword="null"/> if <paramref name="dependency"/>
    /// should be removed for whatever reason. May be the same object as <paramref name="dependency"/>
    /// if no change was applied (in which case the return value will be <see langword="false"/>).
    /// </param>
    /// <returns><see langword="true"/> if anything changed, otherwise <see langword="false"/>.</returns>
    internal bool TryUpdate(
        MSBuildDependency dependency,
        (string ItemSpec, IImmutableDictionary<string, string> Properties)? evaluation,
        (string ItemSpec, IImmutableDictionary<string, string> Properties)? build,
        string projectFullPath,
        bool isEvaluationOnlySnapshot,
        bool hasBuildError,
        out MSBuildDependency? updated)
    {
        Assumes.True(evaluation is not null || build is not null);

        if (build is not null)
        {
            Assumes.False(isEvaluationOnlySnapshot);

            IImmutableDictionary<string, string> properties = build.Value.Properties;
            string itemSpec = build.Value.ItemSpec;
            bool? isVisible = properties.GetBoolProperty(ProjectItemMetadata.Visible);

            if (isVisible is false)
            {
                // This dependency is not visible and should be removed.
                updated = null;
                return true;
            }

            bool isResolved = true;

            bool isImplicit = IsImplicit(projectFullPath, properties, evaluation?.Properties);
            DiagnosticLevel diagnosticLevel = GetDiagnosticLevel(isResolved, hasBuildError, properties);
            string caption = GetResolvedCaption(itemSpec, dependency.Id, properties);
            ProjectImageMoniker icon = GetIcon(isImplicit, diagnosticLevel);
            ProjectTreeFlags flags = UpdateTreeFlags(dependency.Id, FlagCache.Get(isResolved, isImplicit));

            updated = dependency.With(
                isResolved: isResolved,
                isImplicit: isImplicit,
                diagnosticLevel: diagnosticLevel,
                caption: caption,
                icon: icon,
                flags: flags,
                filePath: itemSpec,
                browseObjectProperties: properties);
            return !ReferenceEquals(dependency, updated);
        }
        else if (evaluation is not null)
        {
            IImmutableDictionary<string, string> properties = evaluation.Value.Properties;
            string itemSpec = evaluation.Value.ItemSpec;
            bool? isVisible = properties.GetBoolProperty(ProjectItemMetadata.Visible);

            if (isVisible is false)
            {
                // This dependency is not visible and should be removed.
                updated = null;
                return true;
            }

            bool? isResolved = isEvaluationOnlySnapshot ? dependency.IsResolved : false;

            bool isImplicit = IsImplicit(projectFullPath, evaluationProperties: null, properties);
            DiagnosticLevel diagnosticLevel = GetDiagnosticLevel(isResolved, hasBuildError, properties, defaultLevel: dependency.DiagnosticLevel);
            string caption = GetUnresolvedCaption(itemSpec, properties);
            ProjectImageMoniker icon = GetIcon(isImplicit, diagnosticLevel);
            ProjectTreeFlags flags = UpdateTreeFlags(itemSpec, FlagCache.Get(isResolved ?? true, isImplicit));

            updated = dependency.With(
                isResolved: isResolved,
                isImplicit: isImplicit,
                diagnosticLevel: diagnosticLevel,
                caption: caption,
                icon: icon,
                flags: flags,
                filePath: itemSpec,
                browseObjectProperties: properties);
            return !ReferenceEquals(dependency, updated);
        }
        else
        {
            throw Assumes.NotReachable();
        }
    }

    /// <summary>
    /// Constructs the unresolved form of <paramref name="dependency"/>.
    /// </summary>
    /// <param name="dependency">The dependency to make unresolved.</param>
    /// <param name="updated">The unresolved dependency.</param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="updated"/> is different to <paramref name="dependency"/>,
    /// or <see langword="false"/> to indicate that the dependency was already unresolved and no change
    /// was made.
    /// </returns>
    internal bool TryMakeUnresolved(MSBuildDependency dependency, out MSBuildDependency updated)
    {
        DiagnosticLevel diagnosticLevel = dependency.DiagnosticLevel <= DiagnosticLevel.Warning ? DiagnosticLevel.Warning : dependency.DiagnosticLevel;
        ProjectTreeFlags flags = UpdateTreeFlags(dependency.Id, FlagCache.Get(isResolved: false, dependency.IsImplicit));
        ProjectImageMoniker icon = GetIcon(dependency.IsImplicit, diagnosticLevel);
        updated = dependency.With(isResolved: false, icon: icon, flags: flags, diagnosticLevel: diagnosticLevel);
        return !ReferenceEquals(dependency, updated);
    }

    private ProjectImageMoniker GetIcon(bool isImplicit, DiagnosticLevel diagnosticLevel)
    {
        return (diagnosticLevel, isImplicit) switch
        {
            (DiagnosticLevel.None, true) => IconImplicit,
            (DiagnosticLevel.None, false) => Icon,
            (DiagnosticLevel.Warning, _) => IconWarning,
            (DiagnosticLevel.Error, _) => IconError,
            _ => throw new()
        };
    }

    private static bool IsImplicit(
        string projectFullPath,
        IImmutableDictionary<string, string>? evaluationProperties,
        IImmutableDictionary<string, string>? buildProperties)
    {
        Requires.NotNull(projectFullPath);
        Assumes.True(evaluationProperties is not null || buildProperties is not null);

        // We have two ways of determining whether a given dependency is implicit.
        //
        // 1. Checking its "IsImplicitlyDefined" metadata, and
        // 2. Checking whether its "DefiningProjectFullPath" metadata matches the current project path.
        //
        // Additionally, we check both evaluation and build data where possible, because certain
        // dependency types require this currently. For example, resolved package references (from
        // build data) report "IsImplicitlyDefined" as "false" despite being defined outside the
        // user's project. Therefore, we check "DefiningProjectFullPath" first, however that value is
        // only defined (for packages) in evaluation data, hence the need to check both eval and build
        // properties.
        //
        // Note that ideally the evaluation/build would produce consistent values for all
        // dependencies, rather than us having to manipulate them here. We could fix that in
        // MSBuild and SDK targets one day.

        // Check for "DefiningProjectFullPath" metadata and compare with the project file path.
        // This is used by COM dependencies (and possibly others). It may only be present in
        // evaluation properties, so we check both build and eval data.
        string? definingProjectFullPath = buildProperties?.GetStringProperty(ProjectItemMetadata.DefiningProjectFullPath);
        definingProjectFullPath ??= evaluationProperties?.GetStringProperty(ProjectItemMetadata.DefiningProjectFullPath);

        if (!string.IsNullOrEmpty(definingProjectFullPath))
        {
            return !StringComparers.Paths.Equals(definingProjectFullPath, projectFullPath);
        }

        // Check for "IsImplicitlyDefined" metadata, which is available on certain items.
        // Some items, such as package references, define this on evaluation data but not build data.
        bool? isImplicitMetadata = buildProperties?.GetBoolProperty(ProjectItemMetadata.IsImplicitlyDefined);
        isImplicitMetadata ??= evaluationProperties?.GetBoolProperty(ProjectItemMetadata.IsImplicitlyDefined);

        if (isImplicitMetadata != null)
        {
            return isImplicitMetadata.Value;
        }

        return false;
    }

    /// <summary>
    /// Assists in the creation and caching of flags used in MSBuild dependencies.
    /// </summary>
    /// <remarks>
    /// <see cref="ProjectTreeFlags"/> internally performs operations on immutable sets during operations such as
    /// <see cref="ProjectTreeFlags.Union(ProjectTreeFlags)"/> and <see cref="ProjectTreeFlags.Union(ProjectTreeFlags)"/>
    /// which commonly results in allocating identical values on the heap. By caching them, dependency model types can
    /// avoid such allocations during their construction, keeping them lighter.
    /// </remarks>
    internal readonly struct DependencyFlagCache
    {
        private readonly ProjectTreeFlags[] _lookup;

        public DependencyFlagCache(ProjectTreeFlags resolved, ProjectTreeFlags unresolved, ProjectTreeFlags remove = default)
        {
            // The 'isResolved' dimension determines whether we start with generic resolved or unresolved dependency flags.
            // We then add (union) and remove (except) any other flags as instructed.

            ProjectTreeFlags combinedResolved = DependencyTreeFlags.ResolvedDependencyFlags.Union(resolved).Except(remove);
            ProjectTreeFlags combinedUnresolved = DependencyTreeFlags.UnresolvedDependencyFlags.Union(unresolved).Except(remove);

            // The 'isImplicit' dimension only enforces, when true, that the dependency cannot be removed.

            _lookup = new ProjectTreeFlags[4];
            _lookup[Index(isResolved: true, isImplicit: false)] = combinedResolved;
            _lookup[Index(isResolved: true, isImplicit: true)] = combinedResolved.Except(DependencyTreeFlags.SupportsRemove);
            _lookup[Index(isResolved: false, isImplicit: false)] = combinedUnresolved;
            _lookup[Index(isResolved: false, isImplicit: true)] = combinedUnresolved.Except(DependencyTreeFlags.SupportsRemove);
        }

        /// <summary>Retrieves the cached <see cref="ProjectTreeFlags"/> given the arguments.</summary>
        public ProjectTreeFlags Get(bool isResolved, bool isImplicit) => _lookup[Index(isResolved, isImplicit)];

        /// <summary>Provides a unique mapping between (bool,bool) and [0,3].</summary>
        private static int Index(bool isResolved, bool isImplicit) => (isResolved ? 2 : 0) | (isImplicit ? 1 : 0);
    }
}

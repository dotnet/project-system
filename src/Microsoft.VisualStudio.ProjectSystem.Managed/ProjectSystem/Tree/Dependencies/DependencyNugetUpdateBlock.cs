// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Text.RegularExpressions;
using System.Threading.Tasks.Dataflow;
using System.Timers;
using Microsoft.VisualStudio.Collections;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Models;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies;

[Export(typeof(IDependencyNugetUpdateBlock))]
internal class DependencyNugetUpdateBlock: ProjectValueDataSourceBase<Dictionary<string, DiagnosticLevel>>, IDependencyNugetUpdateBlock
{
    private int _sourceVersion;

    private IBroadcastBlock<IProjectVersionedValue<Dictionary<string, DiagnosticLevel>>> _broadcastBlock = null!;

    private IReceivableSourceBlock<IProjectVersionedValue<Dictionary<string, DiagnosticLevel>>> _publicBlock = null!;

    private Dictionary<string, DiagnosticLevel>? _lastPublishedValue;

    public override NamedIdentity DataSourceKey { get; } = new(nameof(DependencyNugetUpdateBlock));

    public override IComparable DataSourceVersion => _sourceVersion;

    [ImportingConstructor]
    public DependencyNugetUpdateBlock(UnconfiguredProject unconfiguredProject) 
        : base(unconfiguredProject.Services, synchronousDisposal: false, registerDataSource: false)
    {
    }
    
    public override IReceivableSourceBlock<IProjectVersionedValue<Dictionary<string, DiagnosticLevel>>> SourceBlock
    {
        get
        {
            EnsureInitialized();
            return _publicBlock;
        }
    }
    
    protected override void Initialize()
    {
#pragma warning disable RS0030
        base.Initialize();
#pragma warning restore RS0030
        
        _broadcastBlock = DataflowBlockSlim.CreateBroadcastBlock<IProjectVersionedValue<Dictionary<string, DiagnosticLevel>>>(nameFormat: $"{nameof(DependencyNugetUpdateBlock)} {1}");

        _publicBlock = _broadcastBlock.SafePublicize();
        
        PostNewValue(GetNewValue()); // TODO currently blocks receiving dependency model to make initial request

        var timer = new System.Timers.Timer();
        timer.Elapsed += OnRefreshDependencyStatus;
        timer.Interval = TimeSpan.FromMinutes(15).TotalMilliseconds;
        timer.Start();
    }

    private void OnRefreshDependencyStatus(object sender, ElapsedEventArgs elapsedEventArgs)
    {
        PostNewValue(GetNewValue());
    }

    private string RunCommandSynchronouslyAndReceiveOutput(string command)
    {
        var process = new System.Diagnostics.Process();
        var startInfo = new System.Diagnostics.ProcessStartInfo
        {
            WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
            FileName = "cmd.exe",
            Arguments = $"/C {command}",
            RedirectStandardOutput = true,
            UseShellExecute = false
        };
        
        process.StartInfo = startInfo;
        process.Start();
        string output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        return output;
    }
    
    private Dictionary<string, DiagnosticLevel> GetNewValue()
    {
        Dictionary<string, DiagnosticLevel> packageDiagnosticLevels = new();
        
        string dotnetListVulnerableCommandOutput = RunCommandSynchronouslyAndReceiveOutput("dotnet list package --vulnerable");
        string dotnetListOutdatedCommandOutput = RunCommandSynchronouslyAndReceiveOutput("dotnet list package --outdated");
        string dotnetListDeprecatedCommandOutput = RunCommandSynchronouslyAndReceiveOutput("dotnet list package --deprecated");

        foreach (Match match in Regex.Matches(dotnetListVulnerableCommandOutput, "> ([^\\s]+)\\s+"))
        {
            AddPackageIfLevelHasPriority(match.Groups[1].Value, DiagnosticLevel.Vulnerability);
        }
        
        foreach (Match match in Regex.Matches(dotnetListOutdatedCommandOutput, "> ([^\\s]+)\\s+"))
        {
            AddPackageIfLevelHasPriority(match.Groups[1].Value, DiagnosticLevel.UpgradeAvailable);
        }
        
        foreach (Match match in Regex.Matches(dotnetListDeprecatedCommandOutput, "> ([^\\s]+)\\s+"))
        {
            AddPackageIfLevelHasPriority(match.Groups[1].Value, DiagnosticLevel.Deprecation);
        }

        void AddPackageIfLevelHasPriority(string package, DiagnosticLevel level)
        {
            if (!packageDiagnosticLevels.TryGetValue(package, out DiagnosticLevel existingValue) || existingValue < level)
            {
                packageDiagnosticLevels[package] = level;
            }
        }

        return packageDiagnosticLevels;
    }

    private void PostNewValue(Dictionary<string, DiagnosticLevel> newValue)
    {
        // Add thread safety as needed. Make sure to never regress the data source version published
        if (!DictionaryEqualityComparer<string, DiagnosticLevel>.Instance.Equals(newValue, _lastPublishedValue)) // only publish if you have to
        {
            _lastPublishedValue = newValue;
            _broadcastBlock.Post(
                new ProjectVersionedValue<Dictionary<string, DiagnosticLevel>>(
                    newValue,
                    ImmutableDictionary.Create<NamedIdentity, IComparable>().Add(
                        DataSourceKey,
                        _sourceVersion++)));
        }
    }
}

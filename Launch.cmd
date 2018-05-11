REM Assumes that your current working directory is the git repository root
set VisualStudioXamlRulesDir=%cd%\artifacts\Debug\VSSetup\Rules\
set VisualBasicDesignTimeTargetsPath=%VisualStudioXamlRulesDir%Microsoft.VisualBasic.DesignTime.targets
set FSharpDesignTimeTargetsPath=%VisualStudioXamlRulesDir%Microsoft.FSharp.DesignTime.targets
set CSharpDesignTimeTargetsPath=%VisualStudioXamlRulesDir%Microsoft.CSharp.DesignTime.targets

devenv /rootsuffix ProjectSystem
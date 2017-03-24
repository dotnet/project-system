using EnvDTE;
using VSLangProj;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties
{
    /// <summary>
    /// This implementation of VSProject just redirects the VSProject call to the contained
    /// VSProject object imported from CPS
    /// </summary>
    internal partial class VsLangProjectProperties : VSProject
    {
        public VSLangProj.References References => _vsProject.References;

        public BuildManager BuildManager => _vsProject.BuildManager;

        public DTE DTE => _vsProject.DTE;

        public Project Project => _vsProject.Project;

        public ProjectItem WebReferencesFolder => _vsProject.WebReferencesFolder;

        public string TemplatePath => _vsProject.TemplatePath;

        public bool WorkOffline { get => _vsProject.WorkOffline; set => _vsProject.WorkOffline = value; }

        public Imports Imports => _vsProject.Imports;

        public VSProjectEvents Events => _vsProject.Events;

        // Implementation of VSProject to redirect the call to the actual VSProject object
        public ProjectItem CreateWebReferencesFolder()
        {
            return _vsProject.CreateWebReferencesFolder();
        }

        public ProjectItem AddWebReference(string bstrUrl)
        {
            return _vsProject.AddWebReference(bstrUrl);
        }

        public void Refresh()
        {
            _vsProject.Refresh();
        }

        public void CopyProject(string bstrDestFolder, string bstrDestUNCPath, prjCopyProjectOption copyProjectOption, string bstrUsername, string bstrPassword)
        {
            _vsProject.CopyProject(bstrDestFolder, bstrDestUNCPath, copyProjectOption, bstrUsername, bstrPassword);
        }

        public void Exec(prjExecCommand command, int bSuppressUI, object varIn, out object pVarOut)
        {
            _vsProject.Exec(command, bSuppressUI, varIn, out pVarOut);
        }

        public void GenerateKeyPairFiles(string strPublicPrivateFile, string strPublicOnlyFile = "0")
        {
            _vsProject.GenerateKeyPairFiles(strPublicPrivateFile, strPublicOnlyFile);
        }

        public string GetUniqueFilename(object pDispatch, string bstrRoot, string bstrDesiredExt)
        {
            return _vsProject.GetUniqueFilename(pDispatch, bstrRoot, bstrDesiredExt);
        }
    }
}

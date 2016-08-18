using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using System.Globalization;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Rename
{
    internal abstract class AbstractRenameStrategy : IRenameStrategy
    {
        protected readonly IProjectThreadingService _threadingService;
        protected readonly IUserNotificationServices _userNotificationServices;
        protected readonly IOptionsSettings _optionsSettings;
        private bool _userPromptedOnce = false;
        private bool _userConfirmedRename = true;

        public AbstractRenameStrategy(IProjectThreadingService threadingService, IUserNotificationServices userNotificationService, IOptionsSettings optionsSettings)
        {
            _threadingService = threadingService;
            _userNotificationServices = userNotificationService;
            _optionsSettings = optionsSettings;
        }

        public abstract bool CanHandleRename(string oldFilePath, string newFilePath);

        public abstract Task RenameAsync(Project newProject, string oldFilePath, string newFilePath);

        protected async Task<bool> CheckUserConfirmation(string oldFileName)
        {
            if (_userPromptedOnce)
            {
                return _userConfirmedRename;
            }

            await _threadingService.SwitchToUIThread();
            var userNeedPrompt = _optionsSettings.GetPropertiesValue("Environment", "ProjectsAndSolution", "PromptForRenameSymbol", false);
            if (userNeedPrompt)
            {
                string renamePromptMessage = string.Format(CultureInfo.CurrentCulture, VSResources.RenameSymbolPrompt, oldFileName);

                await _threadingService.SwitchToUIThread();
                _userConfirmedRename = _userNotificationServices.Confirm(renamePromptMessage);
            }

            _userPromptedOnce = true;
            return _userConfirmedRename;
        }

        protected Document GetDocument(Project project, string filePath) =>
            (from d in project.Documents where StringComparers.Paths.Equals(d.FilePath, filePath) select d).FirstOrDefault();

        protected async Task<SyntaxNode> GetRootNode(Document newDocument) =>
            await newDocument.GetSyntaxRootAsync().ConfigureAwait(false);

        protected bool HasMatchingSyntaxNode(Document document, SyntaxNode syntaxNode, string name)
        {
            var generator = SyntaxGenerator.GetGenerator(document);
            var kind = generator.GetDeclarationKind(syntaxNode);

            if (kind == DeclarationKind.Class ||
                kind == DeclarationKind.Interface ||
                kind == DeclarationKind.Delegate ||
                kind == DeclarationKind.Enum ||
                kind == DeclarationKind.Struct)
            {
                return generator.GetName(syntaxNode) == name;
            }
            return false;
        }
    }
}

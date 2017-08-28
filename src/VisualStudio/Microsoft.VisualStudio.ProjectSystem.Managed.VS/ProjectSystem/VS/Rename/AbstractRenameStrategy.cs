using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Rename
{
    internal abstract class AbstractRenameStrategy : IRenameStrategy
    {
        protected readonly IProjectThreadingService _threadingService;
        protected readonly IUserNotificationServices _userNotificationServices;
        protected readonly IEnvironmentOptions _environmentOptions;
        protected readonly IRoslynServices _roslynServices;
        private bool _userPromptedOnce = false;
        private bool _userConfirmedRename = true;

        public AbstractRenameStrategy(
            IProjectThreadingService threadingService,
            IUserNotificationServices userNotificationService,
            IEnvironmentOptions environmentOptions,
            IRoslynServices roslynServices)
        {
            _threadingService = threadingService;
            _userNotificationServices = userNotificationService;
            _environmentOptions = environmentOptions;
            _roslynServices = roslynServices;
        }

        public abstract bool CanHandleRename(string oldFilePath, string newFilePath, bool isCaseSensitive);

        public abstract Task RenameAsync(Project newProject, string oldFilePath, string newFilePath, bool isCaseSensitive);

        protected async Task<bool> CheckUserConfirmation(string oldFileName)
        {
            if (_userPromptedOnce)
            {
                return _userConfirmedRename;
            }

            await _threadingService.SwitchToUIThread();
            var userNeedPrompt = _environmentOptions.GetOption("Environment", "ProjectsAndSolution", "PromptForRenameSymbol", false);
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

        protected bool HasMatchingSyntaxNode(SemanticModel model, SyntaxNode syntaxNode, string name, bool isCaseSensitive)
        {
            if (model.GetDeclaredSymbol(syntaxNode) is INamedTypeSymbol symbol &&
                (symbol.TypeKind == TypeKind.Class ||
                 symbol.TypeKind == TypeKind.Interface ||
                 symbol.TypeKind == TypeKind.Delegate ||
                 symbol.TypeKind == TypeKind.Enum ||
                 symbol.TypeKind == TypeKind.Struct ||
                 symbol.TypeKind == TypeKind.Module))
            {
                return string.Compare(symbol.Name, name, !isCaseSensitive) == 0;
            }

            return false;
        }
    }
}

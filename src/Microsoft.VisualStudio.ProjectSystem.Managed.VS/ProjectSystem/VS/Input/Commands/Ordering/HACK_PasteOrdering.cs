using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands.Ordering
{
    /// <summary>
    /// This is a hack due to CPS not exposing easier ways to handle pasting items.
    /// This does proper file ordering for pasting or dropping items into a folder or project.
    /// </summary>
    [Export(typeof(IPasteDataObjectProcessor))]
    [Export(typeof(IPasteHandler))]
    [AppliesTo(ProjectCapabilities.SortByDisplayOrder)]
    [Order(OrderPrecedence)]
    internal class HACK_PasteOrdering : IPasteHandler, IPasteDataObjectProcessor
    {
        public const int OrderPrecedence = 10000;

        private readonly IPhysicalProjectTree _projectTree;
        private readonly IUnconfiguredProjectVsServices _projectVsServices;

        private IProjectTree _dropTarget;

        private IPasteHandler _pasteHandler;
        private IPasteDataObjectProcessor _pasteProcessor;

        [ImportingConstructor]
        public HACK_PasteOrdering(IPhysicalProjectTree projectTree, IUnconfiguredProjectVsServices projectVsServices, UnconfiguredProject unconfiguredProject)
        {
            Requires.NotNull(projectTree, nameof(IPhysicalProjectTree));
            Requires.NotNull(unconfiguredProject, nameof(UnconfiguredProject));

            _projectTree = projectTree;
            _projectVsServices = projectVsServices;

            PasteHandlers = new OrderPrecedenceImportCollection<IPasteHandler>(projectCapabilityCheckProvider: unconfiguredProject);
            PasteProcessors = new OrderPrecedenceImportCollection<IPasteDataObjectProcessor>(projectCapabilityCheckProvider: unconfiguredProject);
        }

        [ImportMany]
        private OrderPrecedenceImportCollection<IPasteHandler> PasteHandlers { get; set; }

        [ImportMany]
        private OrderPrecedenceImportCollection<IPasteDataObjectProcessor> PasteProcessors { get; set; }

        private IPasteHandler PasteHandler
        {
            get
            {
                if (_pasteHandler == null)
                {
                    // Grab the paste handler that has the highest order precedence that is below HACK_PasteOrdering's order precedence. 
                    _pasteHandler = 
                        PasteHandlers.Where(x => x.Metadata.OrderPrecedence < OrderPrecedence)
                        .OrderByDescending(x => x.Metadata.OrderPrecedence).First().Value;
                }

                Assumes.NotNull(_pasteHandler);

                return _pasteHandler;
            }
        }

        private IPasteDataObjectProcessor PasteProcessor
        {
            get
            {
                if (_pasteProcessor == null)
                {
                    // Grab the paste processor that has the highest order precedence that is below HACK_PasteOrdering's order precedence. 
                    _pasteProcessor = 
                        PasteProcessors.Where(x => x.Metadata.OrderPrecedence < OrderPrecedence)
                        .OrderByDescending(x => x.Metadata.OrderPrecedence).First().Value;
                }

                Assumes.NotNull(_pasteProcessor);

                return _pasteProcessor;
            }
        }

        #region IPasteDataObjectProcessor
        public bool CanHandleDataObject(object dataObject, IProjectTree dropTarget, IProjectTreeProvider currentProvider)
        {
            return PasteProcessor.CanHandleDataObject(dataObject, dropTarget, currentProvider);
        }

        public Task<IEnumerable<ICopyPasteItem>> ProcessDataObjectAsync(object dataObject, IProjectTree dropTarget, IProjectTreeProvider currentProvider, DropEffects effect)
        {
            _dropTarget = dropTarget;
            return PasteProcessor.ProcessDataObjectAsync(dataObject, dropTarget, currentProvider, effect);
        }

        public DropEffects? QueryDropEffect(object dataObject, int grfKeyState, bool draggedFromThisProject)
        {
            return PasteProcessor.QueryDropEffect(dataObject, grfKeyState, draggedFromThisProject);
        }

        public Task ProcessPostFilterAsync(IEnumerable<ICopyPasteItem> items)
        {
            return PasteProcessor.ProcessPostFilterAsync(items);
        }
        #endregion

        #region IPasteHandler
        public bool CanHandleItem(Type itemType)
        {
            return PasteHandler.CanHandleItem(itemType);
        }

        public void FilterItemList(IEnumerable<ICopyPasteItem> items, DropEffects effect)
        {
            PasteHandler.FilterItemList(items, effect);
        }

        public async Task<PasteItemsResult> PasteItemsAsync(IEnumerable<ICopyPasteItem> items, DropEffects effect)
        {
            Assumes.NotNull(_dropTarget);

            var result = new PasteItemsResult();

            var addedElements = await OrderingHelper.AddItems(_projectVsServices.ActiveConfiguredProject, _projectVsServices.ProjectLockService, async () =>
            {
                result = await PasteHandler.PasteItemsAsync(items, effect).ConfigureAwait(false);
            }).ConfigureAwait(false);

            await new ProjectAccessor(_projectVsServices.ProjectLockService).OpenProjectForWriteAsync(_projectVsServices.ActiveConfiguredProject, project =>
            {
                OrderingHelper.TryMoveElementsToTop(project, addedElements, _dropTarget);
            }).ConfigureAwait(false);

            return result;
        }

        public bool PromptForAnyOverwrites(IEnumerable<ICopyPasteItem> items, ref DropEffects effect)
        {
            return PasteHandler.PromptForAnyOverwrites(items, ref effect);
        }

        public Task<IEnumerable<string>> ValidateItemListAsync(IEnumerable<ICopyPasteItem> items, DropEffects effect)
        {
            return PasteHandler.ValidateItemListAsync(items, effect);
        }
        #endregion
    }
}

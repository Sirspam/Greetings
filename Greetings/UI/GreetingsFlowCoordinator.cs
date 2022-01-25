using BeatSaberMarkupLanguage;
using Greetings.UI.ViewControllers;
using HMUI;
using Zenject;

namespace Greetings.UI
{
    internal class GreetingsFlowCoordinator : FlowCoordinator
    {
        private ScreenControlsViewController _screenControlsViewController = null!;
        private VideoSelectionViewController _videoSelectionViewController = null!;
        private GreetingsSettingsViewController _greetingsSettingsViewController = null!;
        private MainFlowCoordinator _mainFlowCoordinator = null!;

        [Inject]
        public void Construct(ScreenControlsViewController screenControlsViewController, VideoSelectionViewController videoSelectionViewController, GreetingsSettingsViewController greetingsSettingsViewController, MainFlowCoordinator mainFlowCoordinator)
        {
            _screenControlsViewController = screenControlsViewController;
            _videoSelectionViewController = videoSelectionViewController;
            _greetingsSettingsViewController = greetingsSettingsViewController;
            _mainFlowCoordinator = mainFlowCoordinator;
        }

        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            SetTitle(nameof(Greetings));
            showBackButton = true;

            ProvideInitialViewControllers(_screenControlsViewController, _videoSelectionViewController, _greetingsSettingsViewController);
        }

        protected override void BackButtonWasPressed(ViewController topViewController)
        {
            _mainFlowCoordinator.DismissFlowCoordinator(this);
        }
    }
}
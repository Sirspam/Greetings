using BeatSaberMarkupLanguage;
using Greetings.UI.ViewControllers;
using HMUI;
using Zenject;

namespace Greetings.UI.FlowCoordinator
{
	internal class GreetingsFlowCoordinator : HMUI.FlowCoordinator
	{
		private MainFlowCoordinator _mainFlowCoordinator = null!;
		private ScreenControlsViewController _screenControlsViewController = null!;
		private VideoSelectionViewController _videoSelectionViewController = null!;
		private GreetingsSettingsViewController _greetingsSettingsViewController = null!;

		[Inject]
		public void Construct(MainFlowCoordinator mainFlowCoordinator, ScreenControlsViewController screenControlsViewController, VideoSelectionViewController videoSelectionViewController, GreetingsSettingsViewController greetingsSettingsViewController)
		{
			_mainFlowCoordinator = mainFlowCoordinator;
			_screenControlsViewController = screenControlsViewController;
			_videoSelectionViewController = videoSelectionViewController;
			_greetingsSettingsViewController = greetingsSettingsViewController;
		}

		protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
		{
			SetTitle(nameof(Greetings));
			showBackButton = true;
			
			ProvideInitialViewControllers(_screenControlsViewController, _videoSelectionViewController, _greetingsSettingsViewController);
		}

		protected override void BackButtonWasPressed(ViewController topViewController) => _mainFlowCoordinator.DismissFlowCoordinator(this);
	}
}
﻿using BeatSaberMarkupLanguage;
using Greetings.UI.ViewControllers;
using HMUI;
using Zenject;

namespace Greetings.UI.FlowCoordinator
{
	internal sealed class NoVideosFlowCoordinator : HMUI.FlowCoordinator
	{
		private MainFlowCoordinator _mainFlowCoordinator = null!;
		private NoVideosViewController _noVideosViewController = null!;
		private GreetingsFlowCoordinator _greetingsFlowCoordinator = null!;

		[Inject]
		public void Construct(MainFlowCoordinator mainFlowCoordinator, NoVideosViewController noVideosViewController, GreetingsFlowCoordinator greetingsFlowCoordinator)
		{
			_mainFlowCoordinator = mainFlowCoordinator;
			_noVideosViewController = noVideosViewController;
			_greetingsFlowCoordinator = greetingsFlowCoordinator;
		}

		public override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
		{
			SetTitle(nameof(Greetings));
			showBackButton = true;
			
			ProvideInitialViewControllers(_noVideosViewController);
			_noVideosViewController.VideosAddedEvent += NoVideosViewControllerOnVideosAddedEvent;
		}

		public override void BackButtonWasPressed(ViewController topViewController) => _mainFlowCoordinator.DismissFlowCoordinator(this);

		private void NoVideosViewControllerOnVideosAddedEvent()
		{
			_noVideosViewController.VideosAddedEvent -= NoVideosViewControllerOnVideosAddedEvent;
			
			_mainFlowCoordinator.DismissFlowCoordinator(this, () => _mainFlowCoordinator.PresentFlowCoordinator(_greetingsFlowCoordinator), immediately: true);
		}
	}
}
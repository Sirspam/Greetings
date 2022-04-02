using System;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.MenuButtons;
using Zenject;

namespace Greetings.UI
{
	internal class MenuButtonManager : IInitializable, IDisposable
	{
		private readonly MenuButton _menuButton;
		private readonly MainFlowCoordinator _mainFlowCoordinator;
		private readonly GreetingsFlowCoordinator _greetingsFlowCoordinator;

		public MenuButtonManager(MainFlowCoordinator mainFlowCoordinator, GreetingsFlowCoordinator greetingsFlowCoordinator)
		{
			_menuButton = new MenuButton(nameof(Greetings), "Wort Wort Wort!", MenuButtonClicked);
			_mainFlowCoordinator = mainFlowCoordinator;
			_greetingsFlowCoordinator = greetingsFlowCoordinator;
		}

		public void Initialize()
		{
			MenuButtons.instance.RegisterButton(_menuButton);
		}

		public void Dispose()
		{
			if (MenuButtons.IsSingletonAvailable)
				MenuButtons.instance.UnregisterButton(_menuButton);
		}

		private void MenuButtonClicked()
		{
			_mainFlowCoordinator.PresentFlowCoordinator(_greetingsFlowCoordinator);
		}
	}
}
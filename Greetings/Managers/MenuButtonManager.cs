﻿using System;
using BeatSaberMarkupLanguage.MenuButtons;
using Greetings.Configuration;
using Greetings.UI.FlowCoordinator;
using Zenject;

namespace Greetings.Managers
{
	internal sealed class MenuButtonManager : IInitializable, IDisposable
	{
		private readonly MenuButton _menuButton;
		private readonly PluginConfig _pluginConfig;
		private readonly MainFlowCoordinator _mainFlowCoordinator;
		private readonly NoVideosFlowCoordinator _noVideosFlowCoordinator;
		private readonly GreetingsFlowCoordinator _greetingsFlowCoordinator;

		public MenuButtonManager(PluginConfig pluginConfig, MainFlowCoordinator mainFlowCoordinator, NoVideosFlowCoordinator noVideosFlowCoordinator, GreetingsFlowCoordinator greetingsFlowCoordinator)
		{
			_menuButton = new MenuButton(nameof(Greetings), "Wort Wort Wort!", MenuButtonClicked);
			_pluginConfig = pluginConfig;
			_mainFlowCoordinator = mainFlowCoordinator;
			_noVideosFlowCoordinator = noVideosFlowCoordinator;
			_greetingsFlowCoordinator = greetingsFlowCoordinator;
		}

		public void Initialize() => MenuButtons.Instance.RegisterButton(_menuButton);

		public void Dispose() => MenuButtons.Instance.UnregisterButton(_menuButton);

		private void MenuButtonClicked()
		{
			if (_pluginConfig.CheckIfVideoPathEmpty())
			{
				_mainFlowCoordinator.PresentFlowCoordinator(_noVideosFlowCoordinator);
			}
			else
			{
				_mainFlowCoordinator.PresentFlowCoordinator(_greetingsFlowCoordinator);	
			}
		}
	}
}
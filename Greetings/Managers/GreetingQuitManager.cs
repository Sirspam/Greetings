using System;
using Greetings.Configuration;
using Greetings.Utils;
using IPA.Utilities;
using Zenject;

namespace Greetings.Managers
{
	internal sealed class GreetingQuitManager : IInitializable, IDisposable
	{
		private readonly PluginConfig _pluginConfig;
		private readonly FadeInOutController _fadeInOutController;
		private readonly GreetingsScreenManager _greetingsScreenManager;
		private readonly MainMenuViewController _mainMenuViewController;

		public GreetingQuitManager(PluginConfig pluginConfig, FadeInOutController fadeInOutController, GreetingsScreenManager greetingsScreenManager, MainMenuViewController mainMenuViewController)
		{
			_pluginConfig = pluginConfig;
			_fadeInOutController = fadeInOutController;
			_greetingsScreenManager = greetingsScreenManager;
			_mainMenuViewController = mainMenuViewController;
		}

		public void Initialize()
		{
			_mainMenuViewController.didActivateEvent += MainMenuViewControllerOndidActivateEvent;
		}

		public void Dispose()
		{
			_mainMenuViewController.didActivateEvent -= MainMenuViewControllerOndidActivateEvent;
		}

		private void MainMenuViewControllerOndidActivateEvent(bool firstactivation, bool addedtohierarchy, bool screensystemenabling)
		{
			_mainMenuViewController.didActivateEvent -= MainMenuViewControllerOndidActivateEvent;
			
			var quitButton = _mainMenuViewController._quitButton;
			quitButton.onClick.RemoveAllListeners();
			quitButton.onClick.AddListener(OnClickListener);
		}

		private void OnClickListener()
		{
			if (_pluginConfig.PlayOnQuit && _pluginConfig.SelectedQuitVideo != null)
			{
				_greetingsScreenManager.StartGreetings(_pluginConfig.RandomQuitVideo ? GreetingsUtils.VideoType.RandomVideo : GreetingsUtils.VideoType.QuitVideo, () =>
				{
					if (_greetingsScreenManager.GreetingsUtils.SkipRequested)
					{
						QuitButtonAction();
					}
					else
					{
						_fadeInOutController.FadeOut(QuitButtonAction);
					}
				}, true);
			}
			else
			{
				QuitButtonAction();
			}
		}

		private void QuitButtonAction() => _mainMenuViewController.InvokeMethod<object, MainMenuViewController>("HandleMenuButton", MainMenuViewController.MenuButton.Quit);
	}
}
using System;
using Greetings.Configuration;
using Greetings.Utils;
using Zenject;

namespace Greetings.Managers
{
	internal class GreetingsStartManager : IInitializable, IDisposable
	{
		private static bool _greetingsPlayed;
		
		private readonly PluginConfig _pluginConfig;
		private readonly GameScenesManager _gameScenesManager;
		private readonly GreetingsScreenManager _greetingsScreenManager;
		
		public GreetingsStartManager(PluginConfig pluginConfig, GameScenesManager gameScenesManager, GreetingsScreenManager greetingsScreenManager)
		{
			_pluginConfig = pluginConfig;
			_gameScenesManager = gameScenesManager;
			_greetingsScreenManager = greetingsScreenManager;
		}

		public void Initialize()
		{
			if ((_greetingsPlayed && _pluginConfig.PlayOnce) || !_pluginConfig.PlayOnStart)
			{
				return;
			}

			_gameScenesManager.transitionDidFinishEvent += GameScenesManagerOnTransitionDidFinishEvent;
		}

		public void Dispose()
		{
			_gameScenesManager.transitionDidFinishEvent -= GameScenesManagerOnTransitionDidFinishEvent;
		}

		private void GameScenesManagerOnTransitionDidFinishEvent(ScenesTransitionSetupDataSO arg1, DiContainer arg2)
		{
			_gameScenesManager.transitionDidFinishEvent -= GameScenesManagerOnTransitionDidFinishEvent;

			_greetingsPlayed = true;
			if (_pluginConfig.SelectedStartVideo != null)
			{
				_greetingsScreenManager.StartGreetings(_pluginConfig.RandomStartVideo ? GreetingsUtils.VideoType.RandomVideo : GreetingsUtils.VideoType.StartVideo, useAwaiter: true);	
			}
		}
	}
}
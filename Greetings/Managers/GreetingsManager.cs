using System;
using Greetings.Components;
using Greetings.Configuration;
using Greetings.UI.ViewControllers;
using Greetings.Utils;
using HMUI;
using IPA.Utilities;
using SiraUtil.Extras;
using SiraUtil.Logging;
using Tweening;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.XR;
using Zenject;

namespace Greetings.Managers
{
	internal class GreetingsManager : IInitializable, IDisposable
	{
		private static bool _greetingsPlayed;
		private ScreenSystem? _screenSystem;
		private SkipController? _skipController;
		private GreetingsAwaiter? _greetingsAwaiter;
		private Vector3 _originalScreenSystemPosition;

		private readonly SiraLog _siraLog;
		private readonly DiContainer _diContainer;
		private readonly ScreenUtils _screenUtils;
		private readonly PluginConfig _pluginConfig;
		private readonly TickableManager _tickableManager;
		private readonly HierarchyManager _hierarchyManager;
		private readonly GameScenesManager _gameScenesManager;
		private readonly TimeTweeningManager _timeTweeningManager;
		private readonly FloorTextViewController _floorTextViewController;
		private readonly VRControllersInputManager _vrControllersInputManager;
		private readonly GameplaySetupViewController _gameplaySetupViewController;

		public GreetingsManager(SiraLog siraLog, DiContainer diContainer, ScreenUtils screenUtils, PluginConfig pluginConfig, TickableManager tickableManager, HierarchyManager hierarchyManager, GameScenesManager gameScenesManager, TimeTweeningManager tweeningManager, FloorTextViewController floorTextViewController, VRControllersInputManager vrControllersInputManager, GameplaySetupViewController gameplaySetupViewController)
		{
			_siraLog = siraLog;
			_diContainer = diContainer;
			_screenUtils = screenUtils;
			_pluginConfig = pluginConfig;
			_tickableManager = tickableManager;
			_hierarchyManager = hierarchyManager;
			_gameScenesManager = gameScenesManager;
			_timeTweeningManager = tweeningManager;
			_floorTextViewController = floorTextViewController;
			_vrControllersInputManager = vrControllersInputManager;
			_gameplaySetupViewController = gameplaySetupViewController;
		}

		public void Initialize()
		{
			if (_greetingsPlayed && _pluginConfig.PlayOnce)
			{
				return;
			}

			_screenSystem = _hierarchyManager.GetField<ScreenSystem, HierarchyManager>("_screenSystem");
			var gameObject = _screenSystem.gameObject;
			_originalScreenSystemPosition = gameObject.transform.position;
			gameObject.transform.position = Vector3.negativeInfinity; // Sod off wanker
			_screenUtils.CreateScreen(_pluginConfig.RandomVideo);

			_gameScenesManager.transitionDidFinishEvent += GameScenesManagerOnTransitionDidFinishEvent;
			_gameplaySetupViewController.didActivateEvent += GameplaySetupViewControllerOndidActivateEvent;
		}

		public void Dispose()
		{
			_gameScenesManager.transitionDidFinishEvent -= GameScenesManagerOnTransitionDidFinishEvent;
			_gameplaySetupViewController.didActivateEvent -= GameplaySetupViewControllerOndidActivateEvent;

			if (_screenUtils.VideoPlayer != null)
			{
				_screenUtils.VideoPlayer!.loopPointReached -= VideoPlayer_loopPointReached;
			}
		}

		private void GameScenesManagerOnTransitionDidFinishEvent(ScenesTransitionSetupDataSO arg1, DiContainer arg2)
		{
			_gameScenesManager.transitionDidFinishEvent -= GameScenesManagerOnTransitionDidFinishEvent;

			_skipController = new SkipController(this);
			_tickableManager.Add(_skipController);
			_floorTextViewController.ChangeTextTo(FloorTextViewController.TextChange.SkipText);
			_screenUtils.VideoPlayer!.loopPointReached += VideoPlayer_loopPointReached;
			_greetingsAwaiter = _diContainer.InstantiateComponent<GreetingsAwaiter>(_screenUtils.GreetingsScreen);
		}

		// Sometimes the Greetings screen won't close correctly. It's an incredibly rare case and I can't reproduce it myself
		// This is here just in case that situation does happen.
		private void GameplaySetupViewControllerOndidActivateEvent(bool firstactivation, bool addedtohierarchy, bool screensystemenabling)
		{
			_screenUtils.HideScreen(false);
			_gameplaySetupViewController.didActivateEvent -= GameplaySetupViewControllerOndidActivateEvent;
		}

		private async void DismissGreetings()
		{
			if (_skipController != null)
			{
				_tickableManager.Remove(_skipController);
				_skipController = null;
			}

			if (_greetingsAwaiter != null)
			{
				_greetingsAwaiter.StopAllCoroutines();
			}

			_greetingsPlayed = true;
			_screenUtils.HideScreen();
			_floorTextViewController.HideScreen();
			await Utilities.PauseChamp;
			_screenSystem!.gameObject.transform.position = _originalScreenSystemPosition;
			foreach (var cg in _screenSystem.gameObject.GetComponentsInChildren<CanvasGroup>())
			{
				_timeTweeningManager.AddTween(new FloatTween(0f, 1f, val => cg.alpha = val, 0.5f, EaseType.InOutQuad), cg.gameObject);
			}
		}

		private void VideoPlayer_loopPointReached(VideoPlayer source)
		{
			DismissGreetings();
			_screenUtils.VideoPlayer!.loopPointReached -= VideoPlayer_loopPointReached;
		}

		private class SkipController : ITickable
		{
			private readonly GreetingsManager _greetingsManager;

			public SkipController(GreetingsManager greetingsManager)
			{
				_greetingsManager = greetingsManager;
			}

			public void Tick()
			{
				if (_greetingsManager._vrControllersInputManager.TriggerValue(XRNode.LeftHand) >= 0.8f || _greetingsManager._vrControllersInputManager.TriggerValue(XRNode.RightHand) >= 0.8f || Input.GetKey(KeyCode.Mouse0))
				{
					_greetingsManager._siraLog.Info("Skipping Greetings");

					_greetingsManager.DismissGreetings();
					_greetingsManager._screenUtils.VideoPlayer!.loopPointReached -= _greetingsManager.VideoPlayer_loopPointReached;
				}
			}
		}
	}
}
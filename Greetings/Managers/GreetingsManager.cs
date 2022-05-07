using System;
using Greetings.Configuration;
using Greetings.UI.ViewControllers;
using Greetings.Utils;
using HMUI;
using IPA.Loader;
using IPA.Utilities;
using SiraUtil.Extras;
using SiraUtil.Logging;
using SiraUtil.Tools.FPFC;
using SongCore;
using Tweening;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.XR;
using Zenject;

namespace Greetings.Managers
{
	internal class GreetingsManager : IInitializable, IDisposable
	{
		private readonly SiraLog _siraLog;
		private readonly ScreenUtils _screenUtils;
		private readonly PluginConfig _pluginConfig;
		private readonly IFPFCSettings _fpfcSettings;
		private readonly TickableManager _tickableManager;
		private readonly HierarchyManager _hierarchyManager;
		private readonly GameScenesManager _gameScenesManager;
		private readonly IVRPlatformHelper _vrPlatformHelper;
		private readonly TimeTweeningManager _timeTweeningManager;
		private readonly FloorTextViewController _floorTextViewController;
		private readonly VRControllersInputManager _vrControllersInputManager;
		private readonly GameplaySetupViewController _gameplaySetupViewController;

		private static bool _greetingsPlayed;
		private ScreenSystem? _screenSystem;
		private SkipController? _skipController;
		private GreetingsAwaiter? _greetingsAwaiter;
		private Vector3 _originalScreenSystemPosition;

		public GreetingsManager(SiraLog siraLog, ScreenUtils screenUtils, PluginConfig pluginConfig, IFPFCSettings fpfcSettings, TickableManager tickableManager, HierarchyManager hierarchyManager, IVRPlatformHelper vrPlatformHelper, GameScenesManager gameScenesManager, TimeTweeningManager tweeningManager, FloorTextViewController floorTextViewController, VRControllersInputManager vrControllersInputManager, GameplaySetupViewController gameplaySetupViewController)
		{
			_siraLog = siraLog;
			_screenUtils = screenUtils;
			_pluginConfig = pluginConfig;
			_fpfcSettings = fpfcSettings;
			_tickableManager = tickableManager;
			_hierarchyManager = hierarchyManager;
			_vrPlatformHelper = vrPlatformHelper;
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
			_floorTextViewController.ChangeText(FloorTextViewController.TextChange.ShowSkipText);
			
			if (_pluginConfig.AwaitFps || _pluginConfig.AwaitHmd)
			{
				_greetingsAwaiter = new GreetingsAwaiter(this);
				_tickableManager.Add(_greetingsAwaiter);
				_floorTextViewController.ChangeText(FloorTextViewController.TextChange.ShowFpsText);
				return;
			}

			_screenUtils.ShowScreen(randomVideo: _pluginConfig.RandomVideo);
			_screenUtils.VideoPlayer!.loopPointReached += VideoPlayer_loopPointReached;
		}

		// Sometimes the Greetings screen won't close correctly. It's an incredibly rare case and I can't reproduce it myself
		// This is here just in case that situation does happen.
		private void GameplaySetupViewControllerOndidActivateEvent(bool firstactivation, bool addedtohierarchy, bool screensystemenabling)
		{
			_screenUtils.HideScreen();
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
				_greetingsAwaiter.YouShouldKillYourselfNow = true;
				_greetingsAwaiter = null;
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

		private class GreetingsAwaiter : ITickable
		{
			private readonly GreetingsManager _greetingsManager;

			private int _targetFps;
			private int _fpsStreak;
			private bool _awaitingHmd;
			private float _maxWaitTime;
			private int _stabilityCounter;
			private bool _awaitingSongCore;
			private float _waitTimeCounter;
			private float _updateRateCounter;
			private bool _awaitingPreparation;
			private bool _firsTimeAwaitingHmd;
			private bool _firsTimeAwaitingSongCore;
			internal bool YouShouldKillYourselfNow;
			private bool _firstTimeAwaitingPreparation;

			public GreetingsAwaiter(GreetingsManager greetingsManager)
			{
				_greetingsManager = greetingsManager;
				Initialize();
			}

			private void Initialize()
			{
				_waitTimeCounter = 0f;
				_stabilityCounter = 0;
				_updateRateCounter = 0f;
				_awaitingPreparation = true;
				YouShouldKillYourselfNow = false;

				_targetFps = _greetingsManager._pluginConfig.TargetFps;
				_fpsStreak = _greetingsManager._pluginConfig.FpsStreak;
				_maxWaitTime = _greetingsManager._pluginConfig.MaxWaitTime;
				_awaitingHmd = _greetingsManager._pluginConfig.AwaitHmd && !_greetingsManager._fpfcSettings.Enabled;
				_awaitingSongCore = _greetingsManager._pluginConfig.AwaitSongCore && PluginManager.GetPluginFromId("SongCore") != null;

				_firsTimeAwaitingHmd = true;
				_firsTimeAwaitingSongCore = true;
				_firstTimeAwaitingPreparation = true;

				_greetingsManager._siraLog.Debug("target fps " + _targetFps);
				if (_awaitingHmd)
				{
					_greetingsManager._siraLog.Info("Awaiting HMD Focus");
					return;
				}

				_greetingsManager._siraLog.Info("Awaiting FPS stabilisation");
			}

			public void Tick()
			{
				if (YouShouldKillYourselfNow)
				{
					_greetingsManager._tickableManager.Remove(this);
					return;
				}

				if (_awaitingHmd)
				{
					if (_firsTimeAwaitingHmd)
					{
						_greetingsManager._siraLog.Info("Awaiting HMD focus");
						_firsTimeAwaitingHmd = false;
					}
					
					if (!_greetingsManager._vrPlatformHelper.hasVrFocus && !_greetingsManager._fpfcSettings.Enabled)
					{
						return;
					}

					_awaitingHmd = false;
					_greetingsManager._siraLog.Info("HMD focused");
				}

				if (_awaitingSongCore)
				{
					if (_firsTimeAwaitingSongCore)
					{
						_greetingsManager._siraLog.Info("Awaiting SongCore");
						_firsTimeAwaitingSongCore = false;
					}
					
					if (!Loader.AreSongsLoaded)
					{
						return;
					}

					_awaitingSongCore = false;
					_greetingsManager._siraLog.Info("SongCore loaded");
				}

				if (_awaitingPreparation)
				{
					if (_firstTimeAwaitingPreparation)
					{
						_greetingsManager._siraLog.Info("Awaiting video preparation");
						_firstTimeAwaitingPreparation = false;
					}
					
					if (!_greetingsManager._screenUtils.VideoPlayer!.isPrepared)
					{
						return;
					}

					_awaitingPreparation = false;
					_greetingsManager._siraLog.Info("Video prepared");
				}

				_updateRateCounter += Time.unscaledDeltaTime;
				if (_updateRateCounter < 0.10f)
				{
					return;
				}

				_updateRateCounter = 0f;

				// We do a lil' bit of logging
				var fps = Time.timeScale / Time.deltaTime;
				_greetingsManager._siraLog.Debug("fps " + fps);

				_waitTimeCounter += Time.unscaledDeltaTime;
				if (_targetFps <= fps)
				{
					_stabilityCounter += 1;
					if (_stabilityCounter >= _fpsStreak)
					{
						_greetingsManager._siraLog.Info("Target FPS reached, starting Greetings");
						PlayTheThingThenKys();
					}
				}
				else if (_waitTimeCounter >= _maxWaitTime)
				{
					_greetingsManager._siraLog.Info("Max wait time reached, starting Greetings");
					PlayTheThingThenKys();
				}
				else
				{
					_stabilityCounter = 0;
				}
			}

			private void PlayTheThingThenKys()
			{
				_greetingsManager._screenUtils.ShowScreen(randomVideo: _greetingsManager._pluginConfig.RandomVideo);
				_greetingsManager._screenUtils.VideoPlayer!.loopPointReached += _greetingsManager.VideoPlayer_loopPointReached;
				_greetingsManager._floorTextViewController.ChangeText(FloorTextViewController.TextChange.HideFpsText);
				_greetingsManager._tickableManager.Remove(this);
			}
		}
	}
}
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

namespace Greetings
{
    internal class GreetingsController : IInitializable
    {
        private readonly SiraLog _siraLog;
        private readonly CheeseUtils _cheeseUtils;
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

        private static bool _greetingsPlayed;
        private ScreenSystem? _screenSystem;
        private SkipController? _skipController;
        private GreetingsAwaiter? _greetingsAwaiter;
        private Vector3 _originalScreenSystemPosition;

        public GreetingsController(SiraLog siraLog, CheeseUtils cheeseUtils, ScreenUtils screenUtils, PluginConfig pluginConfig, IFPFCSettings fpfcSettings, TickableManager tickableManager, HierarchyManager hierarchyManager, IVRPlatformHelper vrPlatformHelper, GameScenesManager gameScenesManager, TimeTweeningManager tweeningManager, FloorTextViewController floorTextViewController, VRControllersInputManager vrControllersInputManager)
        {
            _siraLog = siraLog;
            _cheeseUtils = cheeseUtils;
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
        }

        public virtual void Initialize()
        {
            if (_greetingsPlayed && _pluginConfig.PlayOnce)
            {
                return;
            }
            
            _screenSystem = _hierarchyManager.GetField<ScreenSystem, HierarchyManager>("_screenSystem");
            var gameObject = _screenSystem.gameObject;
            _originalScreenSystemPosition = gameObject.transform.position;
            gameObject.transform.position = Vector3.negativeInfinity; // Sod off wanker
            _screenUtils.CreateScreen();
            _screenUtils.VideoPlayer!.Prepare();
            
            _gameScenesManager.transitionDidFinishEvent += GameScenesManagerOnTransitionDidFinishEvent;
        }

        private void GameScenesManagerOnTransitionDidFinishEvent(ScenesTransitionSetupDataSO arg1, DiContainer arg2)
        {
            _gameScenesManager.transitionDidFinishEvent -= GameScenesManagerOnTransitionDidFinishEvent;

            if (!_cheeseUtils.TheTimeHathCome)
            {
                _skipController = new SkipController(this);
                _tickableManager.Add(_skipController);
                _floorTextViewController.ChangeText(FloorTextViewController.TextChange.ShowSkipText);
            }
            
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
            private readonly GreetingsController _greetingsController;

            public SkipController(GreetingsController greetingsController)
            {
                _greetingsController = greetingsController;
            }

            public void Tick()
            {
                if (_greetingsController._vrControllersInputManager.TriggerValue(XRNode.LeftHand) >= 0.8f || _greetingsController._vrControllersInputManager.TriggerValue(XRNode.RightHand) >= 0.8f || Input.GetKey(KeyCode.Mouse0))
                {
                    _greetingsController._siraLog.Info("Skipping Greetings");

                    _greetingsController.DismissGreetings();
                    _greetingsController._screenUtils.VideoPlayer!.loopPointReached -= _greetingsController.VideoPlayer_loopPointReached;
                }
            }
        }

        private class GreetingsAwaiter : ITickable
        {
            private readonly GreetingsController _greetingsController;

            private int _targetFps;
            private int _fpsStreak;
            private bool _awaitingHmd;
            private float _maxWaitTime;
            private int _stabilityCounter;
            private bool _awaitingSongCore;
            private float _waitTimeCounter;
            private bool _awaitingPreperation;
            internal bool YouShouldKillYourselfNow;
            
            public GreetingsAwaiter(GreetingsController greetingsController)
            {
                _greetingsController = greetingsController;
                Initialize();
            }

            private void Initialize()
            {
                _stabilityCounter = 0;
                _waitTimeCounter = 0f;
                _awaitingPreperation = true;
                YouShouldKillYourselfNow = false;

                _targetFps = _greetingsController._pluginConfig.TargetFps;
                _fpsStreak = _greetingsController._pluginConfig.FpsStreak;
                _maxWaitTime = _greetingsController._pluginConfig.MaxWaitTime;
                _awaitingHmd = _greetingsController._pluginConfig.AwaitHmd && !_greetingsController._fpfcSettings.Enabled;
                _awaitingSongCore = _greetingsController._pluginConfig.AwaitSongCore && PluginManager.GetPluginFromId("SongCore") != null;


                _greetingsController._siraLog.Debug("target fps " + _targetFps);
                if (_awaitingHmd)
                {
                    _greetingsController._siraLog.Info("Awaiting HMD Focus");
                    return;
                }
                _greetingsController._siraLog.Info("Awaiting FPS stabilisation");
            }

            public void Tick()
            {
                if (YouShouldKillYourselfNow)
                {
                    _greetingsController._tickableManager.Remove(this);
                    return;
                }

                if (_awaitingHmd)
                {
                    if (!_greetingsController._vrPlatformHelper.hasVrFocus && !_greetingsController._fpfcSettings.Enabled)
                    {
                        return;
                    }
                    _awaitingHmd = false;
                    _greetingsController._siraLog.Info("HMD focused");
                    
                    return;
                }

                if (_awaitingSongCore)
                {
                    if (!Loader.AreSongsLoaded)
                    {
                        return;
                    }
                    _awaitingSongCore = false;
                    _greetingsController._siraLog.Info("SongCore Loaded");

                    return;
                }

                if (_awaitingPreperation)
                {
                    if (!_greetingsController._screenUtils.VideoPlayer!.isPrepared)
                    {
                        return;
                    }

                    _awaitingPreperation = false;
                    _greetingsController._siraLog.Info("Video Prepared");
                }
                    
                
                // We do a lil' bit of logging
                var fps = Time.timeScale / Time.deltaTime;
                _greetingsController._siraLog.Debug("fps " + fps);

                _waitTimeCounter += Time.unscaledDeltaTime;
                if (_targetFps <= fps)
                {
                    _stabilityCounter += 1;
                    if (_stabilityCounter >= _fpsStreak)
                    {
                        _greetingsController._siraLog.Info("Target FPS reached, starting Greetings");
                        PlayTheThingThenKys();
                    }
                }
                else if (_waitTimeCounter >= _maxWaitTime)
                {
                    _greetingsController._siraLog.Info("Max wait time reached, starting Greetings");
                    PlayTheThingThenKys();
                }
                else
                {
                    _stabilityCounter = 0;
                }
            }

            private void PlayTheThingThenKys()
            {
                _greetingsController._screenUtils.ShowScreen(randomVideo: _greetingsController._pluginConfig.RandomVideo);
                _greetingsController._screenUtils.VideoPlayer!.loopPointReached += _greetingsController.VideoPlayer_loopPointReached;
                _greetingsController._floorTextViewController.ChangeText(FloorTextViewController.TextChange.HideFpsText);
                _greetingsController._tickableManager.Remove(this);
            }
        }
    }
}
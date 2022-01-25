using Greetings.Configuration;
using Greetings.UI.ViewControllers;
using Greetings.Utils;
using HMUI;
using IPA.Utilities;
using SiraUtil.Extras;
using SiraUtil.Logging;
using SiraUtil.Tools.FPFC;
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
        private readonly ScreenUtils _screenUtils;
        private readonly PluginConfig _pluginConfig;
        private readonly IFPFCSettings _fpfcSettings;
        private readonly TickableManager _tickableManager;
        private readonly HierarchyManager _hierarchyManager;
        private readonly IVRPlatformHelper _vrPlatformHelper;
        private readonly TimeTweeningManager _timeTweeningManager;
        private readonly FloorTextViewController _floorTextViewController;
        private readonly VRControllersInputManager _vrControllersInputManager;

        private ScreenSystem? _screenSystem;
        private Vector3 _originalScreenSystemPosition;
        private SkipController? _skipController;
        private GreetingsAwaiter? _greetingsAwaiter;

        public GreetingsController(SiraLog siraLog, ScreenUtils screenUtils, PluginConfig pluginConfig, IFPFCSettings fpfcSettings, TickableManager tickableManager, HierarchyManager hierarchyManager, IVRPlatformHelper vrPlatformHelper, TimeTweeningManager tweeningManager, FloorTextViewController floorTextViewController, VRControllersInputManager vrControllersInputManager)
        {
            _siraLog = siraLog;
            _screenUtils = screenUtils;
            _pluginConfig = pluginConfig;
            _fpfcSettings = fpfcSettings;
            _tickableManager = tickableManager;
            _hierarchyManager = hierarchyManager;
            _vrPlatformHelper = vrPlatformHelper;
            _timeTweeningManager = tweeningManager;
            _floorTextViewController = floorTextViewController;
            _vrControllersInputManager = vrControllersInputManager;
        }

        public void Initialize()
        {
            _screenSystem = _hierarchyManager.GetField<ScreenSystem, HierarchyManager>("_screenSystem");
            var gameObject = _screenSystem.gameObject;
            _originalScreenSystemPosition = gameObject.transform.position;
            gameObject.transform.position = Vector3.negativeInfinity; // Sod off wanker
            _screenUtils.CreateScreen();
            
            _skipController = new SkipController(this);
            _tickableManager.Add(_skipController);
            _floorTextViewController.ChangeText(FloorTextViewController.TextChange.ShowSkipText);

            if (_pluginConfig.AwaitFps || _pluginConfig.AwaitHmd)
            {
                _greetingsAwaiter = new GreetingsAwaiter(this);
                _screenUtils.VideoPlayer!.Prepare();
                _tickableManager.Add(_greetingsAwaiter);
                _floorTextViewController.ChangeText(FloorTextViewController.TextChange.ShowFpsText);
                return;
            }
            
            _screenUtils.ShowScreen();
            _screenUtils.VideoPlayer!.loopPointReached += VideoPlayer_loopPointReached;
        }

        private async void DismissGreetings()
        {
            _tickableManager.Remove(_skipController);
            _skipController = null;
            if (_greetingsAwaiter != null)
            {
                _greetingsAwaiter.YouShouldKillYourselfNow = true;
                _greetingsAwaiter = null;
            }

            
            _screenUtils.HideScreen();
            _floorTextViewController.HideScreen();
            await Utilities.PauseChamp;
            _screenSystem!.gameObject.transform.position = _originalScreenSystemPosition;
            foreach (var cg in _screenSystem.gameObject.GetComponentsInChildren<CanvasGroup>()) _timeTweeningManager.AddTween(new FloatTween(0f, 1f, val => cg.alpha = val, 0.5f, EaseType.InOutQuad), cg.gameObject);
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

            internal bool YouShouldKillYourselfNow;
            private float _stabilityCounter;
            private float _waitTimeCounter;
            private float _maxWaitTime;
            private bool _awaitingHmd;

            public GreetingsAwaiter(GreetingsController greetingsController)
            {
                _greetingsController = greetingsController;
                Initialize();
            }

            private void Initialize()
            {
                _stabilityCounter = 0f;
                _awaitingHmd = _greetingsController._pluginConfig.AwaitHmd;
                YouShouldKillYourselfNow = false;

                _waitTimeCounter = 0f;
                _maxWaitTime = 5;

                if (_awaitingHmd)
                {
                    _greetingsController._siraLog.Info("Awaiting HMD and FPS Stabilisation");
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
                    if (_greetingsController._vrPlatformHelper.hasVrFocus || _greetingsController._fpfcSettings.Enabled)
                    {
                        _awaitingHmd = false;
                        _greetingsController._siraLog.Info("HMD focused. Awaiting FPS stabilisation");
                    }
                    return;
                }

                // We do a lil' bit of logging
                _greetingsController._siraLog.Debug("target fps " + XRDevice.refreshRate);
                _greetingsController._siraLog.Debug("fps " + 1f / Time.deltaTime);

                _waitTimeCounter += Time.deltaTime;
                if (XRDevice.refreshRate <= 1f / Time.deltaTime)
                {
                    _stabilityCounter += Time.deltaTime;
                    if (_stabilityCounter >= 0.4f && _greetingsController._screenUtils.VideoPlayer!.isPrepared)
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
            }

            private void PlayTheThingThenKys()
            {
                _greetingsController._screenUtils.ShowScreen();
                _greetingsController._screenUtils.VideoPlayer!.loopPointReached += _greetingsController.VideoPlayer_loopPointReached;
                _greetingsController._floorTextViewController.ChangeText(FloorTextViewController.TextChange.HideFpsText);
                _greetingsController._tickableManager.Remove(this);
            }
        }
    }
}
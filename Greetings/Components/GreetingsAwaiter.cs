using System;
using System.Collections;
using Greetings.Configuration;
using Greetings.UI.ViewControllers;
using Greetings.Utils;
using IPA.Loader;
using SiraUtil.Logging;
using SiraUtil.Tools.FPFC;
using SongCore;
using UnityEngine;
using Zenject;

namespace Greetings.Components
{
	internal class GreetingsAwaiter : MonoBehaviour
	{
		private int _targetFps;
		private int _fpsStreak;
		private float _maxWaitTime;
		private int _stabilityCounter;
		private float _waitTimeCounter;
		private Coroutine? _awaiterCoroutine;

		private SiraLog _siraLog = null!;
		private MainCamera _mainCamera = null!;
		private GreetingsUtils _greetingsUtils = null!;
		private PluginConfig _pluginConfig = null!;
		private IFPFCSettings _fpfcSettings = null!;
		private IVRPlatformHelper _vrPlatformHelper = null!;
		private FloorTextFloatingScreenController _floorTextFloatingScreenController = null!;

		[Inject]
		public void Construct(SiraLog siraLog, MainCamera mainCamera, GreetingsUtils greetingsUtils, PluginConfig pluginConfig, IFPFCSettings fpfcSettings, IVRPlatformHelper vrPlatformHelper, FloorTextFloatingScreenController floorTextFloatingScreenController)
		{
			_siraLog = siraLog;
			_mainCamera = mainCamera;
			_greetingsUtils = greetingsUtils;
			_pluginConfig = pluginConfig;
			_fpfcSettings = fpfcSettings;
			_vrPlatformHelper = vrPlatformHelper;
			_floorTextFloatingScreenController = floorTextFloatingScreenController;
		}

		private void Start()
		{
			_waitTimeCounter = 0f;
			_stabilityCounter = 0;

			_targetFps = _pluginConfig.TargetFps;
			_fpsStreak = _pluginConfig.FpsStreak;
			_maxWaitTime = _pluginConfig.MaxWaitTime;
		}

		private void OnDisable() => _floorTextFloatingScreenController.ChangeVisibility(FloorTextFloatingScreenController.VisibilityChange.HideBottomText);

		public void StartCoroutine()
		{
			enabled = true;
			_awaiterCoroutine = StartCoroutine(AwaiterCoroutine());
		}

		private IEnumerator AwaiterCoroutine()
		{
			_siraLog.Info("Awaiting Video Preparation");
			_floorTextFloatingScreenController.ChangeTextTo(FloorTextFloatingScreenController.TextChange.AwaitingVideoPreparationText);
			yield return new WaitUntil(() => _greetingsUtils.VideoPlayer!.isPrepared);
			_siraLog.Info("Video Prepared");

			if (_pluginConfig.AwaitSongCore && PluginManager.GetPluginFromId("SongCore") != null)
			{
				_siraLog.Info("Awaiting SongCore");
				_floorTextFloatingScreenController.ChangeTextTo(FloorTextFloatingScreenController.TextChange.AwaitingSongCore);
				yield return new WaitUntil(() => Loader.AreSongsLoaded);
				_siraLog.Info("SongCore Loaded");	
			}

			if (_pluginConfig.AwaitHmd)
			{
				_siraLog.Info("Awaiting HMD focus");
				_floorTextFloatingScreenController.ChangeTextTo(FloorTextFloatingScreenController.TextChange.AwaitingHmdFocus);
				yield return new WaitUntil(() => (_vrPlatformHelper.hasVrFocus && Math.Abs(Quaternion.Dot(_mainCamera.rotation, _greetingsUtils.GreetingsScreen!.transform.rotation)) >= 0.95f) || _fpfcSettings.Enabled);
				_siraLog.Info("HMD focused");
			}
			
			if (_pluginConfig.AwaitFps)
			{
				_siraLog.Info("Awaiting FPS Stabilisation");
				_floorTextFloatingScreenController.ChangeTextTo(FloorTextFloatingScreenController.TextChange.AwaitingFpsStabilisation);
				_siraLog.Debug("target fps " + _targetFps);

				while (true)
				{
					// We do a lil' bit of logging
					var fps = Time.timeScale / Time.deltaTime;
					_siraLog.Debug("fps " + fps);

					_waitTimeCounter += Time.unscaledDeltaTime;
					if (_targetFps <= fps)
					{
						_stabilityCounter += 1;
						if (_stabilityCounter >= _fpsStreak)
						{
							_siraLog.Info("Target FPS reached, starting Greetings");
							PlayTheThing();
							break;
						}
					}
					else if (_waitTimeCounter >= _maxWaitTime)
					{
						_siraLog.Info("Max wait time reached, starting Greetings");
						PlayTheThing();
						break;
					}
					else
					{
						_stabilityCounter = 0;
					}
				
					yield return new WaitForSeconds(0.25f);	
				}
			}
			
			PlayTheThing();
		}

		private void PlayTheThing()
		{
			StopCoroutine(_awaiterCoroutine);
			
			_greetingsUtils.ShowScreen();
			enabled = false;
		}
	}
}
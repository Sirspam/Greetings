using System;
using System.Collections;
using Greetings.Configuration;
using Greetings.Managers;
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

		private SiraLog _siraLog = null!;
		private ScreenUtils _screenUtils = null!;
		private PluginConfig _pluginConfig = null!;
		private IFPFCSettings _fpfcSettings = null!;
		private IVRPlatformHelper _vrPlatformHelper = null!;
		private FloorTextViewController _floorTextViewController = null!;

		[Inject]
		public void Construct(SiraLog siraLog, ScreenUtils screenUtils, PluginConfig pluginConfig, IFPFCSettings fpfcSettings, IVRPlatformHelper vrPlatformHelper, FloorTextViewController floorTextViewController)
		{
			_siraLog = siraLog;
			_screenUtils = screenUtils;
			_pluginConfig = pluginConfig;
			_fpfcSettings = fpfcSettings;
			_vrPlatformHelper = vrPlatformHelper;
			_floorTextViewController = floorTextViewController;
		}

		private void Start()
		{
			_waitTimeCounter = 0f;
			_stabilityCounter = 0;

			_targetFps = _pluginConfig.TargetFps;
			_fpsStreak = _pluginConfig.FpsStreak;
			_maxWaitTime = _pluginConfig.MaxWaitTime;

			StartCoroutine(AwaiterCoroutine());
		}
		
		private IEnumerator AwaiterCoroutine()
		{
			_siraLog.Info("Awaiting Video Preparation");
			_floorTextViewController.ChangeTextTo(FloorTextViewController.TextChange.AwaitingVideoPreparationText);
			yield return new WaitUntil(() => _screenUtils.VideoPlayer!.isPrepared);
			_siraLog.Info("Video Prepared");

			if (PluginManager.GetPluginFromId("SongCore") != null && _pluginConfig.AwaitSongCore)
			{
				_siraLog.Info("Awaiting SongCore");
				_floorTextViewController.ChangeTextTo(FloorTextViewController.TextChange.AwaitingSongCore);
				yield return new WaitUntil(() => Loader.AreSongsLoaded);
				_siraLog.Info("SongCore Loaded");	
			}

			if (_pluginConfig.AwaitHmd)
			{
				_siraLog.Info("Awaiting HMD focus");
				_floorTextViewController.ChangeTextTo(FloorTextViewController.TextChange.AwaitingHmd);
				yield return new WaitUntil(() => _vrPlatformHelper.hasVrFocus || _fpfcSettings.Enabled);
				_siraLog.Info("HMD focused");
			}


			if (_pluginConfig.AwaitFps)
			{
				_floorTextViewController.ChangeTextTo(FloorTextViewController.TextChange.AwaitingFpsStabilisation);
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
							PlayTheThingThenGoAwayKThx();
							break;
						}
					}
					else if (_waitTimeCounter >= _maxWaitTime)
					{
						_siraLog.Info("Max wait time reached, starting Greetings");
						PlayTheThingThenGoAwayKThx();
						break;
					}
					else
					{
						_stabilityCounter = 0;
					}
				
					yield return new WaitForSeconds(.25f);	
				}
			}
			
			PlayTheThingThenGoAwayKThx();
		}

		private void PlayTheThingThenGoAwayKThx()
		{
			StopCoroutine(AwaiterCoroutine());
			
			_screenUtils.ShowScreen(randomVideo: _pluginConfig.RandomVideo);
			_floorTextViewController.ChangeVisibility(FloorTextViewController.VisibilityChange.HideBottomText);
		}
	}
}
using System;
using Greetings.Configuration;
using Greetings.UI.ViewControllers;
using Greetings.Utils;
using HMUI;
using IPA.Utilities;
using SiraUtil.Extras;
using Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using VRUIControls;
using Zenject;

namespace Greetings.Managers
{
	internal sealed class GreetingsScreenManager : IInitializable, IDisposable
	{
		private enum TweenType
		{
			In,
			Out
		}

		public event Action? GreetingsShown;
		public event Action? GreetingsHidden;
		public event Action? StartGreetingsFinished;

		private bool _noDismiss;
		public bool HasStartGreetingsPlayed;
		private Action? _videoFinishedCallback;
		private CanvasGroup? _screenSystemCanvasGroup;
		private Vector3 _originalScreenSystemPosition;

		private readonly PluginConfig _pluginConfig;
		private readonly ScreenSystem _screenSystem;
		private readonly VRInputModule _vrInputModule;
		public readonly GreetingsUtils GreetingsUtils;
		private readonly GameScenesManager _gameScenesManager;
		private readonly TimeTweeningManager _timeTweeningManager;
		private readonly FloorTextFloatingScreenController _floorTextFloatingScreenController;

		public GreetingsScreenManager(PluginConfig pluginConfig, VRInputModule vrInputModule, GreetingsUtils greetingsUtils, HierarchyManager hierarchyManager, GameScenesManager gameScenesManager, TimeTweeningManager tweeningManager, FloorTextFloatingScreenController floorTextFloatingScreenController)
		{
			_pluginConfig = pluginConfig;
			_vrInputModule = vrInputModule;
			GreetingsUtils = greetingsUtils;
			_gameScenesManager = gameScenesManager;
			_timeTweeningManager = tweeningManager;
			_floorTextFloatingScreenController = floorTextFloatingScreenController;
			_screenSystem = hierarchyManager.GetField<ScreenSystem, HierarchyManager>("_screenSystem");
		}
		
		public bool IsVideoPlaying => GreetingsUtils.VideoPlayer != null && GreetingsUtils.VideoPlayer.isPlaying;

		public void Initialize()
		{
			_screenSystemCanvasGroup = _screenSystem.gameObject.AddComponent<CanvasGroup>();
			GreetingsUtils.CreateScreen();
			_floorTextFloatingScreenController.CreateScreen();

			if (!_pluginConfig.PlayOnStart)
			{
				HasStartGreetingsPlayed = true;
			}
		}

		public void Dispose()
		{
			if (GreetingsUtils.VideoPlayer != null)
			{
				GreetingsUtils.VideoPlayer!.loopPointReached -= VideoEnded;
			}
			
			_gameScenesManager.transitionDidStartEvent -= GameScenesManagerOntransitionDidStartEvent;
		}
		
		// For Multiplayer and Tournament Assistant
		private void GameScenesManagerOntransitionDidStartEvent(float obj)
		{
			if (SceneManager.GetActiveScene().name == "MainMenu")
			{
				DismissGreetings(true);
			}
		}
		
		public void StartGreetings(GreetingsUtils.VideoType videoType, Action? callback = null, bool noDismiss = false, bool useAwaiter = false)
		{
			if (_pluginConfig.IsVideoPathEmpty || !GreetingsUtils.VideoPlayer!.enabled || GreetingsUtils.VideoPlayer.isPlaying)
			{
				callback?.Invoke();
				return;
			}

			if (!HasStartGreetingsPlayed && _pluginConfig.PlayOnStart && useAwaiter)
			{
				_videoFinishedCallback = () =>
				{
					callback?.Invoke();
					StartGreetingsFinished?.Invoke();
					HasStartGreetingsPlayed = true;
				};
			}
			else
			{
				_videoFinishedCallback = callback;	
			}
			
			GreetingsUtils.SkipRequested = false;
			GreetingsUtils.CreateScreen(videoType);

			_vrInputModule.enabled = false;
			GreetingsShown?.Invoke();
			_originalScreenSystemPosition = _screenSystem.gameObject.transform.position;
			TweenScreenSystemAlpha(TweenType.Out, () =>
			{
				_screenSystem.gameObject.transform.position = Vector3.negativeInfinity;
				GreetingsUtils.VideoPlayer!.loopPointReached += VideoEnded;
				
				_noDismiss = noDismiss;
				GreetingsUtils.SkipController!.StartCoroutine();
				
				if (useAwaiter)
				{
					GreetingsUtils.GreetingsAwaiter!.StartCoroutine();
					return;
				}
				
				GreetingsUtils.ShowScreen();
			});
			
			_gameScenesManager.transitionDidStartEvent += GameScenesManagerOntransitionDidStartEvent;
		}

		// I love events with parameters I don't need
		private void VideoEnded(VideoPlayer source)
		{
			GreetingsUtils.VideoPlayer!.loopPointReached -= VideoEnded;
			VideoEnded();
		}

		public void VideoEnded()
		{
			GreetingsUtils.VideoPlayer!.loopPointReached -= VideoEnded;

			if (_noDismiss)
			{
				_videoFinishedCallback?.Invoke();
				_videoFinishedCallback = null;
				_gameScenesManager.transitionDidStartEvent -= GameScenesManagerOntransitionDidStartEvent;
			}
			else
			{
				DismissGreetings();
			}
		}

		private async void DismissGreetings(bool instant = false)
		{
			_gameScenesManager.transitionDidStartEvent -= GameScenesManagerOntransitionDidStartEvent;
			
			if (GreetingsUtils.SkipController != null)
			{
				GreetingsUtils.SkipController.enabled = false;
			}

			if (GreetingsUtils.GreetingsAwaiter != null)
			{
				GreetingsUtils.GreetingsAwaiter.enabled = false;
			}
			
			GreetingsUtils.HideScreen(!instant);
			_floorTextFloatingScreenController.HideScreen();
			GreetingsHidden?.Invoke();
			
			await Utilities.PauseChamp;
			_screenSystem.gameObject.transform.position = _originalScreenSystemPosition;
			TweenScreenSystemAlpha(TweenType.In, () =>
			{
				_vrInputModule.enabled = true;

				if (_videoFinishedCallback != null)
				{
					_videoFinishedCallback.Invoke();
					_videoFinishedCallback = null;
				}
			}, instant);
		}

		private void TweenScreenSystemAlpha(TweenType tweenType, Action? callback = null, bool instant = false)
		{
			if (_screenSystemCanvasGroup == null)
			{
				return;
			}

			float fromValue;
			float toValue;

			switch (tweenType)
			{
				default:
				case TweenType.In:
					fromValue = 0f;
					toValue = 1f;
					break;
				case TweenType.Out:
					fromValue = 1f;
					toValue = 0f;
					break;
			}

			if (instant)
			{
				_screenSystemCanvasGroup.alpha = toValue;
				callback?.Invoke();
			}
			else
			{
				var tween = new FloatTween(fromValue, toValue, val => _screenSystemCanvasGroup.alpha = val, 0.28f, EaseType.InQuad);
				if (callback != null)
				{
					tween.onCompleted = callback.Invoke;
				}

				_timeTweeningManager.AddTween(tween, _screenSystemCanvasGroup);	
			}
		}
	}
}
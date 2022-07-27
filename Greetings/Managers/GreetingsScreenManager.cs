using System;
using Greetings.UI.ViewControllers;
using Greetings.Utils;
using HMUI;
using IPA.Utilities;
using SiraUtil.Extras;
using Tweening;
using UnityEngine;
using UnityEngine.Video;
using VRUIControls;
using Zenject;

namespace Greetings.Managers
{
	internal class GreetingsScreenManager : IInitializable, IDisposable
	{
		private enum TweenType
		{
			In,
			Out
		}

		public event Action? GreetingsShown;
		public event Action? GreetingsHidden;

		private bool _noDismiss;
		public bool SkipRequested;
		private Action? _videoFinishedCallback;
		private CanvasGroup? _screenSystemCanvasGroup;
		private Vector3 _originalScreenSystemPosition;
		
		private readonly ScreenSystem _screenSystem;
		private readonly VRInputModule _vrInputModule;
		private readonly GreetingsUtils _greetingsUtils;
		private readonly TimeTweeningManager _timeTweeningManager;
		private readonly FloorTextFloatingScreenController _floorTextFloatingScreenController;

		public GreetingsScreenManager(GreetingsUtils greetingsUtils, VRInputModule vrInputModule, HierarchyManager hierarchyManager, TimeTweeningManager tweeningManager, FloorTextFloatingScreenController floorTextFloatingScreenController)
		{
			_vrInputModule = vrInputModule;
			_greetingsUtils = greetingsUtils;
			_screenSystem = hierarchyManager.GetField<ScreenSystem, HierarchyManager>("_screenSystem");
			_timeTweeningManager = tweeningManager;
			_floorTextFloatingScreenController = floorTextFloatingScreenController;
		}

		public void Initialize()
		{
			var gameObject = _screenSystem.gameObject;
			_screenSystemCanvasGroup = gameObject.AddComponent<CanvasGroup>();
			_originalScreenSystemPosition = gameObject.transform.position;
			_greetingsUtils.CreateScreen();
		}

		public void Dispose()
		{
			if (_greetingsUtils.VideoPlayer != null)
			{
				_greetingsUtils.VideoPlayer!.loopPointReached -= VideoEnded;
			}
		}
		
		public void StartGreetings(GreetingsUtils.VideoType videoType, Action? callback = null, bool noDismiss = false, bool useAwaiter = false)
		{
			_videoFinishedCallback = callback;
			SkipRequested = false;
			_greetingsUtils.CreateScreen(videoType);
			
			_vrInputModule.enabled = false;
			GreetingsShown?.Invoke();
			TweenScreenSystemAlpha(TweenType.Out, () =>
			{
				_greetingsUtils.VideoPlayer!.loopPointReached += VideoEnded;
				
				_noDismiss = noDismiss;
				_greetingsUtils.SkipController!.StartCoroutine();
				
				if (useAwaiter)
				{
					_greetingsUtils.GreetingsAwaiter!.StartCoroutine();
					return;
				}

				_greetingsUtils.ShowScreen();
			});
		}

		// I love events with parameters I don't need
		private void VideoEnded(VideoPlayer source)
		{
			_greetingsUtils.VideoPlayer!.loopPointReached -= VideoEnded;
			VideoEnded();
		}

		public void VideoEnded()
		{
			_greetingsUtils.VideoPlayer!.loopPointReached -= VideoEnded;
			
			if (_noDismiss)
			{
				_videoFinishedCallback?.Invoke();
				_videoFinishedCallback = null;
			}
			else
			{
				DismissGreetings();
			}
		}

		public async void DismissGreetings(bool instant = false)
		{
			if (_greetingsUtils.SkipController != null)
			{
				_greetingsUtils.SkipController.enabled = false;
			}

			if (_greetingsUtils.GreetingsAwaiter != null)
			{
				_greetingsUtils.GreetingsAwaiter.enabled = false;
			}
			
			_greetingsUtils.HideScreen(!instant);
			_floorTextFloatingScreenController.HideScreen();
			GreetingsHidden?.Invoke();
			
			await Utilities.PauseChamp;
			_screenSystem!.gameObject.transform.position = _originalScreenSystemPosition;
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
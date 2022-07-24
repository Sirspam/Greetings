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

		private bool _noDismiss;
		public bool SkipRequested;
		private ScreenSystem? _screenSystem;
		private Action? _videoFinishedCallback;
		private CanvasGroup? _screenSystemCanvasGroup;
		private Vector3 _originalScreenSystemPosition;
		
		private readonly GreetingsUtils _greetingsUtils;
		private readonly VRInputModule _vrInputModule;
		private readonly HierarchyManager _hierarchyManager;
		private readonly TimeTweeningManager _timeTweeningManager;
		private readonly FloorTextViewController _floorTextViewController;

		public GreetingsScreenManager(GreetingsUtils greetingsUtils, VRInputModule vrInputModule, HierarchyManager hierarchyManager, TimeTweeningManager tweeningManager, FloorTextViewController floorTextViewController)
		{
			_greetingsUtils = greetingsUtils;
			_vrInputModule = vrInputModule;
			_hierarchyManager = hierarchyManager;
			_timeTweeningManager = tweeningManager;
			_floorTextViewController = floorTextViewController;
		}

		public void Initialize()
		{
			_screenSystem = _hierarchyManager.GetField<ScreenSystem, HierarchyManager>("_screenSystem");
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

		private async void DismissGreetings()
		{
			if (_greetingsUtils.SkipController != null)
			{
				_greetingsUtils.SkipController.enabled = false;
			}

			if (_greetingsUtils.GreetingsAwaiter != null)
			{
				_greetingsUtils.GreetingsAwaiter.enabled = false;
			}
			
			_greetingsUtils.HideScreen();
			_floorTextViewController.HideScreen();
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
			});
		}

		private void TweenScreenSystemAlpha(TweenType tweenType, Action? callback = null)
		{
			if (_screenSystemCanvasGroup == null)
			{
				return;
			}

			float fromValue;
			float toValue;

			switch (tweenType)
			{
				default: case TweenType.In:
					fromValue = 0f;
					toValue = 1f;
					break;
				case TweenType.Out:
					fromValue = 1f;
					toValue = 0f;
					break;
			}

			var tween = new FloatTween(fromValue, toValue, val => _screenSystemCanvasGroup.alpha = val, 0.35f, EaseType.InQuad);
			if (callback != null)
			{
				tween.onCompleted = callback.Invoke;
			}

			_timeTweeningManager.AddTween(tween, _screenSystemCanvasGroup);
		}
	}
}
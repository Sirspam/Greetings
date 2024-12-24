using System;
using System.IO;
using System.Threading.Tasks;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.FloatingScreen;
using BeatSaberMarkupLanguage.ViewControllers;
using Greetings.Configuration;
using Greetings.Managers;
using Greetings.Utils;
using Tweening;
using UnityEngine;
using Zenject;

namespace Greetings.UI.ViewControllers
{
	[ViewDefinition("Greetings.UI.Views.RandomVideoView.bsml")]
	[HotReload(RelativePathToLayout = @"..\Views\RandomVideoView.bsml")]
	internal sealed class RandomVideoFloatingScreenController : BSMLAutomaticViewController, IInitializable, IDisposable
	{
		public static readonly Vector3 DefaultPosition = new(4.15f, 1f, 0.5f);
		public static readonly Quaternion DefaultRotation = Quaternion.Euler(0f, 80f, 0f);

		private bool _interactable = true;
		private Vector3 _handleScale;
		private Color? _highlightColor;
		private Vector3 _floatingScreenScale;
		private FloatingScreen? _floatingScreen;
		private Vector3 _originalFloatingScreenScale;
		private BeatSaberUI.ScaleOptions _scaleOptions;

		[UIComponent("button")] private readonly ClickableImage _imageButton = null!;

		public bool Interactable
		{
			get => _interactable;
			set
			{
				_interactable = value;
				SetIntractability(value);
			}
		}

		private MainCamera _mainCamera = null!;
		private PluginConfig _pluginConfig = null!;
		private MaterialGrabber _materialGrabber = null!;
		private TimeTweeningManager _timeTweeningManager = null!;
		private GreetingsScreenManager _greetingsScreenManager = null!;

		[Inject]
		public void Construct(MainCamera mainCamera, PluginConfig pluginConfig, MaterialGrabber materialGrabber, TimeTweeningManager timeTweeningManager, GreetingsScreenManager greetingsScreenManager)
		{
			_mainCamera = mainCamera;
			_pluginConfig = pluginConfig;
			_materialGrabber = materialGrabber;
			_timeTweeningManager = timeTweeningManager;
			_greetingsScreenManager = greetingsScreenManager;
			_scaleOptions.ShouldScale = true;
			_scaleOptions.MaintainRatio = true;
			_scaleOptions.Height = 128;
			_scaleOptions.Width = 128;
		}

		private void CreateFloatingScreen()
		{
			_floatingScreen = FloatingScreen.CreateFloatingScreen(new Vector2(42, 42), true, _pluginConfig.FloatingScreenPosition, _pluginConfig.FloatingScreenRotation, hasBackground: true);
			_floatingScreen.name = "GreetingsRandomVideoFloatingScreen";
			_floatingScreen.HighlightHandle = true;
			_originalFloatingScreenScale = _floatingScreen.transform.localScale;
			_floatingScreenScale = _originalFloatingScreenScale;
			SetScale(_pluginConfig.FloatingScreenScale);
			_floatingScreen.Handle.transform.localPosition = new Vector3(-_floatingScreen.ScreenSize.x / 1.9f, 0, 0f);
			_handleScale = _floatingScreen.Handle.transform.localScale;
			_floatingScreen.ShowHandle = _pluginConfig.HandleEnabled;

			_floatingScreen.SetRootViewController(this, AnimationType.None);
			
			_highlightColor = _imageButton.HighlightColor;
			SetIntractability(_interactable);

			_floatingScreen.HandleReleased += FloatingScreenOnHandleReleased;
			_greetingsScreenManager.GreetingsShown += GreetingsShown;
			_greetingsScreenManager.GreetingsHidden += GreetingsHidden;
		}

		private void ShowFloatingScreen()
		{
			if (_floatingScreen == null)
			{
				CreateFloatingScreen();
			}

			var tween = new Vector3Tween(new Vector3(_floatingScreen!.transform.localScale.x, 0f, _floatingScreen.transform.localScale.z), _floatingScreenScale, val => _floatingScreen.transform.localScale = val, 0.3f, EaseType.OutExpo)
			{
				onStart = () => _floatingScreen.gameObject.SetActive(true)
			};
			_timeTweeningManager.KillAllTweens(_floatingScreen);
			_timeTweeningManager.AddTween(tween, _floatingScreen);
		}

		private void HideFloatingScreen()
		{
			if (_floatingScreen == null)
			{
				return;
			}

			var tween = new Vector3Tween(_floatingScreen.transform.localScale, new Vector3(_floatingScreenScale.x, 0f), val => _floatingScreen.transform.localScale = val, 0.3f, EaseType.OutExpo)
			{
				onCompleted = () => _floatingScreen.gameObject.SetActive(false)
			};
			_timeTweeningManager.KillAllTweens(_floatingScreen);
			_timeTweeningManager.AddTween(tween, _floatingScreen);
		}

		private void ShowHandle()
		{
			if (_floatingScreen == null)
			{
				CreateFloatingScreen();
			}

			var tween = new Vector3Tween(new Vector3(_handleScale.x, 0f), _handleScale, val => _floatingScreen!.Handle.transform.localScale = val, 0.35f, EaseType.OutQuart)
			{
				onStart = () => _floatingScreen!.ShowHandle = true
			};
			_timeTweeningManager.KillAllTweens(_floatingScreen!.Handle);
			_timeTweeningManager.AddTween(tween, _floatingScreen.Handle);
		}

		private void HideHandle()
		{
			if (_floatingScreen == null)
			{
				return;
			}

			var tween = new Vector3Tween(_handleScale, new Vector3(_handleScale.x, 0f), val => _floatingScreen.Handle.transform.localScale = val, 0.35f, EaseType.OutQuart)
			{
				onCompleted = () => _floatingScreen.ShowHandle = false
			};
			_timeTweeningManager.KillAllTweens(_floatingScreen.Handle);
			_timeTweeningManager.AddTween(tween, _floatingScreen.Handle);
		}

		public void SetFloatingScreenActive(bool value)
		{
			switch (value)
			{
				case true:
				{
					ShowFloatingScreen();
					break;
				}
				case false:
				{
					HideFloatingScreen();
					break;
				}
			}
		}

		public void SetHandleActive(bool value)
		{
			if (!_pluginConfig.FloatingScreenEnabled)
			{
				return;
			}
			
			switch (value)
			{
				case true:
				{
					ShowHandle();
					break;
				}
				case false:
				{
					HideHandle();
					break;
				}
			}
		}

		private void SetIntractability(bool value)
		{
			if (_floatingScreen == null)
			{
				return;
			}
			
			switch (value)
			{
				case true:
				{
					_imageButton.DefaultColor = Color.white;
					_imageButton.HighlightColor = (Color) _highlightColor!;
					break;
				}
				case false:
				{
					_imageButton.DefaultColor = Color.grey;
					_imageButton.HighlightColor = _imageButton.DefaultColor;
					break;
				}
			}
		}

		public void SetScale(float value)
		{
			if (_floatingScreen == null)
			{
				CreateFloatingScreen();
			}

			_floatingScreenScale = _originalFloatingScreenScale * value;
			_floatingScreen!.transform.localScale = _floatingScreenScale;
		}

		public void ResetPosition() => TweenToPosition(DefaultPosition, DefaultRotation, true);

		public void SetUpright()
		{
			if (_floatingScreen != null)
			{
				TweenToPosition(null, Quaternion.Euler(0f, _floatingScreen.gameObject.transform.rotation.eulerAngles.y, 0f), true);
			}
		}

		public void FaceHeadset()
		{
			if (_floatingScreen != null)
			{
				TweenToPosition(null, Quaternion.LookRotation(_floatingScreen.gameObject.transform.position - _mainCamera.transform.position), true);
			}
		}

		public void TweenToPosition(Vector3? newPosition, Quaternion? newRotation, bool saveToConfig)
		{
			if (_floatingScreen == null || (newPosition is null && newRotation is null))
			{
				return;
			}

			var transform = _floatingScreen.transform;
			var position = transform.position;
			var rotation = transform.rotation;
			
			if (newPosition is not null)
			{
				position = (Vector3) newPosition;
			}

			if (newRotation is not null)
			{
				rotation = (Quaternion) newRotation;
			}
			
			TweenToPosition(position, rotation, saveToConfig);
		}
		
		public void TweenToPosition(Vector3 newPosition, Quaternion newRotation, bool saveToConfig)
		{
			if (_floatingScreen == null)
			{
				return;
			}
			
			var transform = _floatingScreen.gameObject.transform;
			var startPosition = transform.position;
			var startRotation = transform.rotation;

			_timeTweeningManager.KillAllTweens(_floatingScreen);
			if (startPosition == newPosition && startRotation == newRotation)
			{
				return;
			}

			var maxDuration = 1.2f;
			var positionDuration = maxDuration / Vector3.Distance(startPosition, newPosition);
			var rotationDuration = maxDuration / Quaternion.Angle(startRotation, newRotation);
			var duration = Mathf.Max(positionDuration, rotationDuration);
			// min time
			duration = Mathf.Max(duration, 0.85f);
			// max time
			duration = Mathf.Min(duration, maxDuration);

			var positionTween = new FloatTween(0f, 1f, val => transform.position = Vector3.Lerp(startPosition, newPosition, val), duration, EaseType.OutQuad);
			var rotationTween = new FloatTween(0f, 1f, val => transform.rotation = Quaternion.Lerp(startRotation, newRotation, val), duration, EaseType.OutCubic);
			_timeTweeningManager.AddTween(positionTween, _floatingScreen);
			_timeTweeningManager.AddTween(rotationTween, _floatingScreen);

			if (!saveToConfig)
			{
				return;
			}

			_pluginConfig.FloatingScreenPosition = newPosition;
			_pluginConfig.FloatingScreenRotation = newRotation;
		}

		public async Task SetImage(string? fileName)
		{
			await _imageButton.SetImageAsync(fileName is null
				? "Greetings.Resources.Greetings.png"
				: Path.Combine(Plugin.FloatingScreenImagesPath, fileName), false, _scaleOptions);
		}

		private void GreetingsShown() => SetFloatingScreenActive(false);

		private void GreetingsHidden() => SetFloatingScreenActive(true);

		private void FloatingScreenOnHandleReleased(object sender, FloatingScreenHandleEventArgs e)
		{
			_pluginConfig.FloatingScreenPosition = e.Position;
			_pluginConfig.FloatingScreenRotation = e.Rotation;
		}

		[UIAction("#post-parse")]
		private async Task PostParse()
		{
			_imageButton.material = _materialGrabber.NoGlowRoundEdge;
			await SetImage(_pluginConfig.FloatingScreenImage);
		}
		
		[UIAction("clicked")]
		private void Clicked()
		{
			if (_interactable && !_pluginConfig.IsVideoPathEmpty)
			{
				SetFloatingScreenActive(false);
				_greetingsScreenManager.StartGreetings(GreetingsUtils.VideoType.RandomVideo);
			}
		}

		public void Initialize()
		{
			if (_pluginConfig.FloatingScreenEnabled && _floatingScreen == null)
			{
				CreateFloatingScreen();
			}
		}

		public void Dispose()
		{
			if (_floatingScreen != null)
			{
				_floatingScreen.HandleReleased -= FloatingScreenOnHandleReleased;
				
				Destroy(_floatingScreen);
				_floatingScreen = null;
			}
			
			_greetingsScreenManager.GreetingsShown -= GreetingsShown;
			_greetingsScreenManager.GreetingsHidden -= GreetingsHidden;
		}
	}
}
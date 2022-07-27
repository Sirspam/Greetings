using System;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.FloatingScreen;
using BeatSaberMarkupLanguage.ViewControllers;
using Greetings.Configuration;
using Greetings.Managers;
using Greetings.Utils;
using Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

namespace Greetings.UI.ViewControllers
{
	[ViewDefinition("Greetings.UI.Views.RandomVideoView.bsml")]
	[HotReload(RelativePathToLayout = @"..\Views\RandomVideoView.bsml")]
	internal class RandomVideoFloatingScreenController : BSMLAutomaticViewController, IInitializable, IDisposable
	{
		public static Vector3 DefaultPosition = new Vector3(4.20f, 1f, 0.5f);
		public static Quaternion DefaultRotation = Quaternion.Euler(0f, 80f, 0f);

		private bool _interactable;
		private Vector3 _handleScale;
		private Color? _highlightColor;
		private Vector3 _floatingScreenScale;
		private FloatingScreen? _floatingScreen;
		private Vector3 _originalFloatingScreenScale;

		[UIComponent("button")] private ClickableImage _imageButton = null!;

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
		private GameScenesManager _gameScenesManager = null!;
		private TimeTweeningManager _timeTweeningManager = null!;
		private GreetingsScreenManager _greetingsScreenManager = null!;

		[Inject]
		public void Construct(MainCamera mainCamera, PluginConfig pluginConfig, GameScenesManager gameScenesManager, TimeTweeningManager timeTweeningManager, GreetingsScreenManager greetingsScreenManager)
		{
			_mainCamera = mainCamera;
			_pluginConfig = pluginConfig;
			_gameScenesManager = gameScenesManager;
			_timeTweeningManager = timeTweeningManager;
			_greetingsScreenManager = greetingsScreenManager;
		}

		private void CreateFloatingScreen()
		{
			_floatingScreen = FloatingScreen.CreateFloatingScreen(new Vector2(42, 42), true, _pluginConfig.FloatingScreenPosition, _pluginConfig.FloatingScreenRotation, hasBackground: true);
			_floatingScreen.name = "GreetingsRandomVideoFloatingScreen";
			_floatingScreen.HighlightHandle = true;
			_originalFloatingScreenScale = _floatingScreen.transform.localScale;
			_floatingScreenScale = _originalFloatingScreenScale;
			SetScale(_pluginConfig.FloatingScreenScale);
			_floatingScreen.handle.transform.localPosition = new Vector3(-_floatingScreen.ScreenSize.x / 1.9f, 0, 0f);
			_handleScale = _floatingScreen.handle.transform.localScale;
			_floatingScreen.ShowHandle = _pluginConfig.HandleEnabled;

			_floatingScreen.SetRootViewController(this, AnimationType.None);
			
			_highlightColor = _imageButton.HighlightColor;
			Interactable = true;

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

			var tween = new Vector3Tween(_floatingScreen!.handle.transform.localScale, _handleScale, val => _floatingScreen.handle.transform.localScale = val, 0.35f, EaseType.OutQuart)
			{
				onStart = () => _floatingScreen.ShowHandle = true
			};
			_timeTweeningManager.KillAllTweens(_floatingScreen.handle);
			_timeTweeningManager.AddTween(tween, _floatingScreen.handle);
		}

		private void HideHandle()
		{
			if (_floatingScreen == null)
			{
				return;
			}

			var tween = new Vector3Tween(_floatingScreen.handle.transform.localScale, new Vector3(_handleScale.x, 0f), val => _floatingScreen.handle.transform.localScale = val, 0.35f, EaseType.OutQuart)
			{
				onCompleted = () => _floatingScreen.ShowHandle = false
			};
			_timeTweeningManager.KillAllTweens(_floatingScreen.handle);
			_timeTweeningManager.AddTween(tween, _floatingScreen.handle);
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
		
		public void ResetPosition()
		{
			if (_floatingScreen == null)
			{
				return;
			}
			
			_timeTweeningManager.KillAllTweens(_floatingScreen);
			var screenTransform = _floatingScreen.gameObject.transform;
			var oldPosition = screenTransform.position;
			var oldRotation = screenTransform.rotation;
			var time = (float) Math.Sqrt(Vector3.Distance(oldPosition, DefaultPosition) / 2);
			if (time < 0.5f)
			{
				time = (float) Math.Sqrt(Quaternion.Angle(oldRotation, DefaultRotation) / 100); // Still should probably change this
			}
			var positionTween = new FloatTween(0f, 1f, val => screenTransform.position = Vector3.Lerp(oldPosition, DefaultPosition, val), time, EaseType.OutQuart);
			var rotationTween = new FloatTween(0f, 1f, val => screenTransform.rotation = Quaternion.Lerp(oldRotation, DefaultRotation, val), time, EaseType.OutQuart);
			_timeTweeningManager.AddTween(positionTween, _floatingScreen);
			_timeTweeningManager.AddTween(rotationTween, _floatingScreen);
		}

		public void SetUpright()
		{
			if (_floatingScreen == null)
			{
				return;
			}
			
			_timeTweeningManager.KillAllTweens(_floatingScreen);
			var previousRotation = _floatingScreen.gameObject.transform.rotation;
			var newRotation = Quaternion.Euler(0f, previousRotation.eulerAngles.y, 0f);
			var tween = new FloatTween(0f, 1f, val => _floatingScreen.gameObject.transform.rotation = Quaternion.Lerp(previousRotation, newRotation, val), 0.5f, EaseType.OutQuart);
			_timeTweeningManager.AddTween(tween, _floatingScreen);
			_pluginConfig.FloatingScreenRotation = newRotation;
		}

		public void FaceHeadset()
		{
			if (_floatingScreen == null)
			{
				return;
			}
			
			_timeTweeningManager.KillAllTweens(_floatingScreen);
			var rootTransform = _floatingScreen.gameObject.transform;
			var previousRotation = rootTransform.rotation;
			var newRotation = Quaternion.LookRotation(rootTransform.position - _mainCamera.transform.position);
			var tween = new FloatTween(0f, 1f, val => _floatingScreen.gameObject.transform.rotation = Quaternion.Lerp(previousRotation, newRotation, val), 0.5f, EaseType.OutQuart);
			_timeTweeningManager.AddTween(tween, _floatingScreen);
			_pluginConfig.FloatingScreenRotation = newRotation;
		}

		private void GreetingsShown() => SetFloatingScreenActive(false);

		private void GreetingsHidden()
		{
			SetFloatingScreenActive(true);
			_gameScenesManager.transitionDidStartEvent -= GameScenesManagerOntransitionDidStartEvent;
		}

		private void FloatingScreenOnHandleReleased(object sender, FloatingScreenHandleEventArgs e)
		{
			_pluginConfig.FloatingScreenPosition = e.Position;
			_pluginConfig.FloatingScreenRotation = e.Rotation;
		}

		[UIAction("clicked")]
		private void Clicked()
		{
			if (Interactable)
			{
				SetFloatingScreenActive(false);
				_greetingsScreenManager.StartGreetings(GreetingsUtils.VideoType.RandomVideo);
				_gameScenesManager.transitionDidStartEvent += GameScenesManagerOntransitionDidStartEvent;
			}
		}

		public void Initialize()
		{
			if (_pluginConfig.FloatingScreenEnabled && _floatingScreen == null)
			{
				CreateFloatingScreen();
			}
		}

		// For Multiplayer and Tournament Assistant
		private void GameScenesManagerOntransitionDidStartEvent(float obj)
		{
			if (SceneManager.GetActiveScene().name == "MainMenu")
			{
				_greetingsScreenManager.DismissGreetings(true);
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
			_gameScenesManager.transitionDidStartEvent -= GameScenesManagerOntransitionDidStartEvent;
		}
	}
}
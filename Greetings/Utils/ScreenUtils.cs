using System;
using System.IO;
using System.Reflection;
using Greetings.Configuration;
using IPA.Utilities;
using SiraUtil.Extras;
using SiraUtil.Logging;
using Tweening;
using UnityEngine;
using UnityEngine.Video;
using Random = System.Random;

namespace Greetings.Utils
{
	internal class ScreenUtils
	{
		private readonly SiraLog _siraLog;
		private readonly PluginConfig _pluginConfig;
		private readonly SongPreviewPlayer _songPreviewPlayer;
		private readonly TimeTweeningManager _timeTweeningManager;

		public VideoPlayer? VideoPlayer;
		public GameObject? GreetingsScreen;
		public readonly string GreetingsPath = Path.Combine(UnityGame.UserDataPath, nameof(Greetings));

		private static readonly int MainTex = Shader.PropertyToID("_MainTex");
		private FloatTween? _currentMoveTween;
		private Vector3 _screenScale;
		private Shader? _screenShader;
		private GameObject? _greetingsUnderline;

		public ScreenUtils(SiraLog siraLog, PluginConfig pluginConfig, SongPreviewPlayer songPreviewPlayer, TimeTweeningManager timeTweeningManager)
		{
			_siraLog = siraLog;
			_pluginConfig = pluginConfig;
			_songPreviewPlayer = songPreviewPlayer;
			_timeTweeningManager = timeTweeningManager;
		}

		public void CreateScreen(bool randomVideo = false)
		{
			if (GreetingsScreen == null)
			{
				_siraLog.Info("Creating GreetingScreen");
				GreetingsScreen = GameObject.CreatePrimitive(PrimitiveType.Quad);
				GreetingsScreen.gameObject.name = "GreetingsScreen";
				GreetingsScreen.transform.position = new Vector3(0, 1.5f, _pluginConfig.ScreenDistance);
				GreetingsScreen.transform.localScale = Vector3.zero;

				VideoPlayer = GreetingsScreen.gameObject.AddComponent<VideoPlayer>();
				VideoPlayer.playOnAwake = false;
				VideoPlayer.renderMode = VideoRenderMode.MaterialOverride;

				var screenRenderer = GreetingsScreen.GetComponent<Renderer>();
				screenRenderer.material = new Material(GetShader())
				{
					color = Color.white
				};
				screenRenderer.material.SetTexture(MainTex, VideoPlayer.texture);
				VideoPlayer.targetMaterialProperty = "_MainTex";
				VideoPlayer.targetMaterialRenderer = screenRenderer;
				
				VideoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
				var audioSource = VideoPlayer.gameObject.AddComponent<AudioSource>();
				VideoPlayer.SetTargetAudioSource(0, audioSource);
				audioSource.outputAudioMixerGroup = _songPreviewPlayer.GetField<AudioSource, SongPreviewPlayer>("_audioSourcePrefab").outputAudioMixerGroup;
			}
			else
			{
				GreetingsScreen.SetActive(true);
			}
			
			if (randomVideo)
			{
				var files = Directory.GetFiles(GreetingsPath);
				var rand = new Random();
				VideoPlayer!.url = files[rand.Next(files.Length)];
			}
			else
			{
				VideoPlayer!.url = Path.Combine(GreetingsPath, _pluginConfig.SelectedVideo);
			}
		}

		public void ShowScreen(bool doTransition = true, bool playOnComplete = true, bool randomVideo = false)
		{
			CreateScreen(randomVideo);

			VideoPlayer!.Prepare();
			VideoPlayer.prepareCompleted += ShowScreenDelegate;
			
			
			async void ShowScreenDelegate(VideoPlayer source)
			{
				VideoPlayer.prepareCompleted -= ShowScreenDelegate;
				VideoPlayer!.StepForward();

				// Sometimes greetings tries to start before the menu music starts to play, so fade out won't work and the background music will come in anyways
				var count = 1;
				while (_songPreviewPlayer.activeAudioClip == null && count <= 3)
				{
					await Utilities.AwaitSleep(250);
					count++;
				}

				// This obviously could be improved as the player's aspect won't exactly fit the video's
				// Unfortunately I am too stupid to figure out a nice way to achieve that
				if (VideoPlayer.width > VideoPlayer.height)
					_screenScale = new Vector3(4f, 2.5f, 0f);
				else if (VideoPlayer.width < VideoPlayer.height)
					_screenScale = new Vector3(2f, 3f, 0f);
				else
					_screenScale = new Vector3(2.5f, 2.5f, 0f);

				if (!doTransition)
				{
					GreetingsScreen!.transform.localScale = _screenScale;
					if (playOnComplete)
					{
						PlayAndFadeOutAudio();
					}

					return;
				}

				_timeTweeningManager.KillAllTweens(GreetingsScreen);
				var tween = new FloatTween(0f, _screenScale.y, val => GreetingsScreen!.transform.localScale = new Vector3(_screenScale.x, val, _screenScale.z), 0.3f, EaseType.OutExpo)
				{
					onCompleted = delegate
					{
						if (playOnComplete)
						{
							PlayAndFadeOutAudio();
						}
					}
				};
				_timeTweeningManager.AddTween(tween, GreetingsScreen);
			}
		}

		public void HideScreen(bool doTransition = true, bool reloadVideo = false)
		{
			if (GreetingsScreen == null || VideoPlayer == null || !GreetingsScreen.gameObject.activeSelf)
			{
				return;
			}

			VideoPlayer.Pause();
			_songPreviewPlayer.CrossfadeToDefault();

			if (!doTransition || GreetingsScreen.transform.localScale == Vector3.zero)
			{
				VideoPlayer.Stop();
				GreetingsScreen.transform.localScale = Vector3.zero;

				if (reloadVideo)
					ShowScreen(false);

				else
					GreetingsScreen.SetActive(false);

				return;
			}

			_timeTweeningManager.KillAllTweens(GreetingsScreen);
			var tween = new FloatTween(_screenScale.y, 0f, val => GreetingsScreen.transform.localScale = new Vector3(_screenScale.x, val, _screenScale.z), 0.3f, EaseType.OutExpo)
			{
				onCompleted = delegate
				{
					if (reloadVideo)
						ShowScreen(playOnComplete: false);

					else
					{
						VideoPlayer.Stop();
						GreetingsScreen.SetActive(false);
					}
				}
			};
			_timeTweeningManager.AddTween(tween, GreetingsScreen);
		}

		public void MoveScreen(float newZValue)
		{
			if (GreetingsScreen == null || !GreetingsScreen.gameObject.activeSelf)
				CreateScreen();

			if (Math.Abs(GreetingsScreen!.transform.position.z - newZValue) < 0)
				return;

			_timeTweeningManager.KillAllTweens(GreetingsScreen);
			var previousPosition = GreetingsScreen!.transform.position;
			var newPosition = new Vector3(previousPosition.x, previousPosition.y, newZValue);
			var tween = new FloatTween(0f, 1f, val => GreetingsScreen.transform.position = Vector3.Lerp(previousPosition, newPosition, val), 0.5f, EaseType.OutQuint);
			_timeTweeningManager.AddTween(tween, GreetingsScreen);
		}

		private void PlayAndFadeOutAudio()
		{
			if (GreetingsScreen == null || !GreetingsScreen.activeSelf || GreetingsScreen.transform.localScale.y == 0f)
			{
				ShowScreen();
			}

			_songPreviewPlayer.FadeOut(0.8f);
			VideoPlayer!.Play();
		}

		private Shader GetShader()
		{
			if (_screenShader != null)
			{
				return _screenShader;
			}

			var loadedAssetBundle = AssetBundle.LoadFromMemory(BeatSaberMarkupLanguage.Utilities.GetResource(Assembly.GetExecutingAssembly(), "Greetings.Resources.greetings.bundle"));
			_screenShader = loadedAssetBundle.LoadAsset<Shader>("ScreenShader");
			loadedAssetBundle.Unload(false);
			return _screenShader;
		}

		private void CreateUnderline()
		{
			if (_greetingsUnderline == null)
			{
				_siraLog.Info("Creating GreetingsUnderline");
				_greetingsUnderline = GameObject.CreatePrimitive(PrimitiveType.Cube);
				_greetingsUnderline.gameObject.name = "GreetingsUnderline";
				_greetingsUnderline.GetComponent<MeshRenderer>().material = new Material(Shader.Find("Custom/SimpleLit"));
				_greetingsUnderline.GetComponent<MeshRenderer>().material.color = Color.red;
				_greetingsUnderline.transform.localScale = Vector3.zero;
			}
			else
			{
				_greetingsUnderline.gameObject.SetActive(true);
			}
		}

		private void ShowUnderline(float zPosition)
		{
			CreateUnderline();
			_timeTweeningManager.KillAllTweens(_greetingsUnderline);

			_greetingsUnderline!.transform.position = new Vector3(0f, 0.05f, zPosition);
			_greetingsUnderline.gameObject.transform.localScale = new Vector3(0f, 0.05f, 0.15f);
			var oldScale = _greetingsUnderline.transform.localScale;
			var newScale = new Vector3(4f, 0.05f, 0.15f);
			var tween = new Vector3Tween(oldScale, newScale, val => _greetingsUnderline.transform.localScale = val, 0.5f, EaseType.OutQuart)
			{
				onStart = () => _greetingsUnderline.gameObject.SetActive(true)
			};
			_timeTweeningManager.AddTween(tween, _greetingsUnderline);
		}

		public void MoveUnderline(float zPosition)
		{
			if (_greetingsUnderline == null || _greetingsUnderline.gameObject.activeSelf == false)
				ShowUnderline(zPosition);


			_currentMoveTween?.Kill();

			var previousPosition = _greetingsUnderline!.transform.position;
			var newPosition = new Vector3(previousPosition.x, previousPosition.y, zPosition);
			_currentMoveTween = new FloatTween(0f, 1f, val => _greetingsUnderline.transform.position = Vector3.Lerp(previousPosition, newPosition, val), 0.5f, EaseType.OutQuint);
			_timeTweeningManager.AddTween(_currentMoveTween, _greetingsUnderline);
		}

		public void HideUnderline()
		{
			if (_greetingsUnderline != null)
			{
				var oldScale = _greetingsUnderline.transform.localScale;
				var newScale = new Vector3(0f, 0.05f, 0.15f);
				var tween = new Vector3Tween(oldScale, newScale, val => _greetingsUnderline.transform.localScale = val, 0.2f, EaseType.OutQuart)
				{
					onCompleted = delegate
					{
						_greetingsUnderline!.transform.position = new Vector3(0f, 0.05f, 0.25f);
						_greetingsUnderline.gameObject.SetActive(false);
					}
				};
				_timeTweeningManager.AddTween(tween, _greetingsUnderline);
			}
		}
	}
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Greetings.Components;
using Greetings.Configuration;
using IPA.Utilities;
using SiraUtil.Extras;
using SiraUtil.Logging;
using Tweening;
using UnityEngine;
using UnityEngine.Video;
using Zenject;
using Random = System.Random;

namespace Greetings.Utils
{
	internal sealed class GreetingsUtils
	{
		public enum VideoType
		{
			StartVideo,
			QuitVideo,
			RandomVideo
		}
		
		public bool SkipRequested;
		public VideoPlayer? VideoPlayer;
		public VideoType CurrentVideoType;
		public GameObject? GreetingsScreen;
		public SkipController? SkipController;
		public GreetingsAwaiter? GreetingsAwaiter;

		private const float MaxWidth = 4f;
		private const float MaxHeight = 2.6f;

		private Vector3 _screenScale;
		private Shader? _screenShader;
		private string? _previousRandomVideo;
		private AudioSource? _screenAudioSource;
		private FloatTween? _underlineMoveTween;
		private GameObject? _greetingsUnderline;
		private Vector3Tween? _underlineShowTween;
		private List<FileInfo> _videoList = new List<FileInfo>();
		private static readonly int MainTex = Shader.PropertyToID("_MainTex");

		private readonly Random _random;
		private readonly SiraLog _siraLog;
		private readonly DiContainer _diContainer;
		private readonly PluginConfig _pluginConfig;
		private readonly SongPreviewPlayer _songPreviewPlayer;
		private readonly TimeTweeningManager _timeTweeningManager;
		
		public GreetingsUtils(SiraLog siraLog, DiContainer diContainer, PluginConfig pluginConfig, SongPreviewPlayer songPreviewPlayer, TimeTweeningManager timeTweeningManager)
		{
			_random = new Random();
			_siraLog = siraLog;
			_diContainer = diContainer;
			_pluginConfig = pluginConfig;
			_songPreviewPlayer = songPreviewPlayer;
			_timeTweeningManager = timeTweeningManager;

			PopulateVideoList();
		}
		
		public void CreateScreen()
		{
			SkipRequested = false;
			
			if (GreetingsScreen == null)
			{
				_siraLog.Info("Creating GreetingScreen");
				GreetingsScreen = GameObject.CreatePrimitive(PrimitiveType.Quad);
				GreetingsScreen.gameObject.name = "GreetingsScreen";
				GreetingsScreen.gameObject.name = "GreetingsScreen";
				GreetingsScreen.transform.position = new Vector3(0, -10f, _pluginConfig.ScreenDistance);
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
				_screenAudioSource = VideoPlayer.gameObject.AddComponent<AudioSource>();
				VideoPlayer.SetTargetAudioSource(0, _screenAudioSource);
				_screenAudioSource.outputAudioMixerGroup = _songPreviewPlayer.GetField<AudioSource, SongPreviewPlayer>("_audioSourcePrefab").outputAudioMixerGroup;
				_screenAudioSource.mute = true;

				SkipController = _diContainer.InstantiateComponent<SkipController>(GreetingsScreen);
				GreetingsAwaiter = _diContainer.InstantiateComponent<GreetingsAwaiter>(GreetingsScreen);
			}
			else
			{
				GreetingsScreen.SetActive(true);
			}
		}

		public void CreateScreen(VideoType videoType)
		{
			CreateScreen();
			
			switch (videoType)
			{
				case VideoType.StartVideo:
				{
					if (_pluginConfig.SelectedStartVideo == null)
					{
						return;
					}
					
					VideoPlayer!.url = Path.Combine(_pluginConfig.VideoPath, _pluginConfig.SelectedStartVideo);
					break;
				}
				case VideoType.QuitVideo:
				{
					if (_pluginConfig.SelectedQuitVideo == null)
					{
						return;
					}
					
					VideoPlayer!.url = Path.Combine(_pluginConfig.VideoPath, _pluginConfig.SelectedQuitVideo);
					break;
				}
				default:
				case VideoType.RandomVideo:
				{
					List<FileInfo> filteredList = _videoList;
					if (_previousRandomVideo != null && _videoList.Count > 1)
					{
						filteredList = _videoList.Where(val => val.FullName != _previousRandomVideo).ToList();
					}
					_previousRandomVideo = filteredList[_random.Next(filteredList.Count)].FullName;
					VideoPlayer!.url = _previousRandomVideo;

					break;
				}
			}
			
			_siraLog.Info($"Preparing {Path.GetFileName(VideoPlayer.url)}");
			VideoPlayer.Prepare();
			VideoPlayer.prepareCompleted += PrepareCompletedFunction;

			void PrepareCompletedFunction(VideoPlayer source)
			{
				VideoPlayer.prepareCompleted -= PrepareCompletedFunction;

				if (SkipRequested)
				{
					return;
				}
				
				float width = VideoPlayer.width;
				float height = VideoPlayer.height;

				if (width > MaxWidth)
				{
					var ratio = MaxWidth / VideoPlayer.width;

					height = VideoPlayer.height * ratio;
					width = VideoPlayer.width * ratio;
				}

				if (height > MaxHeight)
				{
					var ratio = MaxHeight / VideoPlayer.height;

					width = VideoPlayer.width * ratio;
					height = VideoPlayer.height * ratio;
				}

				_screenScale = new Vector3(width, height);
			}
		}
		
		public void ShowScreen(bool doTransition = true, bool playOnComplete = true, VideoType? videoType = null)
		{
			if (videoType.HasValue || VideoPlayer == null || !VideoPlayer.isPrepared)
			{
				CreateScreen(videoType.GetValueOrDefault());

				VideoPlayer!.Prepare();
				VideoPlayer.prepareCompleted += PrepareCompletedFunction;
				return;
			}
			
			PrepareCompletedFunction(VideoPlayer);
			
			
			async void PrepareCompletedFunction(VideoPlayer source)
			{
				VideoPlayer.prepareCompleted -= PrepareCompletedFunction;
				
				if (SkipRequested)
				{
					return;
				}
				
				VideoPlayer!.Pause();
				VideoPlayer!.StepForward();

				// Sometimes greetings tries to start before the menu music starts to play, so fade out won't work and the background music will come in anyways
				var count = 1;
				while (_songPreviewPlayer.activeAudioClip == null && count <= 3)
				{
					await Utilities.AwaitSleep(250);
					count++;
				}
				
				AdjustUnderlineWidth(_screenScale.x);

				if (!doTransition)
				{
					GreetingsScreen!.transform.localScale = _screenScale;
					if (playOnComplete)
					{
						PlayAndFadeOutMenuMusic();
					}

					return;
				}

				GreetingsScreen!.transform.position = new Vector3(0, 1.5f, GreetingsScreen.transform.position.z);
				_timeTweeningManager.KillAllTweens(GreetingsScreen);
				var tween = new FloatTween(0f, _screenScale.y, val => GreetingsScreen!.transform.localScale = new Vector3(_screenScale.x, val, _screenScale.z), 0.3f, EaseType.OutExpo)
				{
					onCompleted = delegate
					{
						if (playOnComplete)
						{
							PlayAndFadeOutMenuMusic();
						}
					}
				};
				_timeTweeningManager.AddTween(tween, GreetingsScreen);
			}
		}
		
		public void HideScreen(bool doTransition = true, bool reloadVideo = false)
		{
			if (GreetingsScreen == null || VideoPlayer == null)
			{
				return;
			}

			PauseAndFadeInMenuMusic();

			if (!doTransition || GreetingsScreen.transform.localScale == Vector3.zero)
			{
				VideoPlayer.Stop();
				GreetingsScreen.transform.position = new Vector3(0, -10f, _pluginConfig.ScreenDistance);
				GreetingsScreen.transform.localScale = Vector3.zero;

				if (reloadVideo)
				{
					ShowScreen(false, videoType: CurrentVideoType);
				}

				else
				{
					GreetingsScreen.SetActive(false);
				}

				return;
			}

			_timeTweeningManager.KillAllTweens(GreetingsScreen);
			var tween = new FloatTween(_screenScale.y, 0f, val => GreetingsScreen.transform.localScale = new Vector3(_screenScale.x, val, _screenScale.z), 0.3f, EaseType.OutExpo)
			{
				onCompleted = delegate
				{
					if (reloadVideo)
					{
						ShowScreen(playOnComplete: false, videoType: CurrentVideoType);
					}

					else
					{
						VideoPlayer.Stop();
						GreetingsScreen.transform.position = new Vector3(0, -10f, _pluginConfig.ScreenDistance);
						GreetingsScreen.SetActive(false);
					}
				}
			};
			_timeTweeningManager.AddTween(tween, GreetingsScreen);
		}
		
		public void MoveScreen(float newZValue)
		{
			if (GreetingsScreen == null || !GreetingsScreen.gameObject.activeSelf)
			{
				CreateScreen();
			}

			if (Math.Abs(GreetingsScreen!.transform.position.z - newZValue) < 0)
			{
				return;
			}

			_timeTweeningManager.KillAllTweens(GreetingsScreen);
			var previousPosition = GreetingsScreen!.transform.position;
			var newPosition = new Vector3(previousPosition.x, previousPosition.y, newZValue);
			var tween = new FloatTween(0f, 1f, val => GreetingsScreen.transform.position = Vector3.Lerp(previousPosition, newPosition, val), 0.5f, EaseType.OutQuint);
			_timeTweeningManager.AddTween(tween, GreetingsScreen);
		}

		public void PlayAndFadeOutMenuMusic()
		{
			if (GreetingsScreen == null || !GreetingsScreen.activeSelf || GreetingsScreen.transform.localScale.y == 0f)
			{
				ShowScreen();
			}
			
			_songPreviewPlayer.FadeOut(0.8f);
			_screenAudioSource!.mute = false;
			VideoPlayer!.Play();
		}

		public void PauseAndFadeInMenuMusic()
		{
			if (GreetingsScreen == null || !GreetingsScreen.activeSelf || GreetingsScreen.transform.localScale.y == 0f)
			{
				return;
			}
			
			VideoPlayer!.Pause();
			_songPreviewPlayer.CrossfadeToDefault();
			_screenAudioSource!.mute = true;
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
			var newScale = new Vector3(_screenScale.x, 0.05f, 0.15f);
			_underlineShowTween = new Vector3Tween(oldScale, newScale, val => _greetingsUnderline.transform.localScale = val, 0.5f, EaseType.OutExpo);
			_timeTweeningManager.AddTween(_underlineShowTween, _greetingsUnderline);
		}

		private void AdjustUnderlineWidth(float newWidth)
		{
			if (_greetingsUnderline == null || !_greetingsUnderline.activeSelf || (int) newWidth == (int) _greetingsUnderline.transform.localScale.x)
			{
				return;
			}
			
			_timeTweeningManager.KillAllTweens(_greetingsUnderline);
			
			var oldScale = _greetingsUnderline.transform.localScale;
			var newScale = new Vector3(newWidth, oldScale.y, oldScale.z);
			var tween = new Vector3Tween(oldScale, newScale, val => _greetingsUnderline.transform.localScale = val, 0.5f, EaseType.OutCubic);
			_timeTweeningManager.AddTween(tween, _greetingsUnderline);
		}
		
		public void MoveUnderline(float zPosition)
		{
			if (_greetingsUnderline == null || !_greetingsUnderline.gameObject.activeSelf)
			{
				if (VideoPlayer != null && !VideoPlayer.isPrepared)
				{
					VideoPlayer.prepareCompleted += PrepareCompletedFunction;

					void PrepareCompletedFunction(VideoPlayer source)
					{
						VideoPlayer.prepareCompleted -= PrepareCompletedFunction;
						ShowUnderline(zPosition);
					}
				}
				else
				{
					ShowUnderline(zPosition);
				}

				return;
			}
			
			_underlineMoveTween?.Kill();

			var previousPosition = _greetingsUnderline!.transform.position;
			var newPosition = new Vector3(previousPosition.x, previousPosition.y, zPosition);
			_underlineMoveTween = new FloatTween(0f, 1f, val => _greetingsUnderline.transform.position = Vector3.Lerp(previousPosition, newPosition, val), 0.5f, EaseType.OutQuint);
			_timeTweeningManager.AddTween(_underlineMoveTween, _greetingsUnderline);
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

		public List<FileInfo> PopulateVideoList()
		{
			_videoList.Clear();
			var files = new DirectoryInfo(_pluginConfig.VideoPath).GetFiles("*.mp4");
			foreach (var file in files)
			{
				if (file.Length > 100000000)
				{
					// Had issues with the video player's prepare event just not being invoked if the video is too large.
					// No clue why it happens, I doubt anyone will be trying to watch a 4k movie or something with Greeting's tiny ass screen
					_siraLog.Warn($"Ignoring {file.Name} as it's above 100 MB");
					continue;
				}
				
				_videoList.Add(file);
			}

			return _videoList;
		}
	}
}
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.ViewControllers;
using Greetings.Configuration;
using Greetings.Utils;
using IPA.Loader;
using SiraUtil.Logging;
using SiraUtil.Web.SiraSync;
using SiraUtil.Zenject;
using TMPro;
using Tweening;
using UnityEngine;
using UnityEngine.Video;
using Zenject;

namespace Greetings.UI.ViewControllers
{
	[HotReload(RelativePathToLayout = @"..\Views\ScreenControlsView")]
	[ViewDefinition("Greetings.UI.Views.ScreenControlsView.bsml")]
	internal sealed class ScreenControlsViewController : BSMLAutomaticViewController
	{
		private bool _loop;
		private bool _updateAvailable;

		[UIComponent("update-text")] private readonly TextMeshProUGUI _updateText = null!;

		[UIComponent("play-or-pause-image")] private readonly ClickableImage _playOrPauseImage = null!;

		[UIComponent("loop-image")] private readonly ClickableImage _loopImage = null!;

		private SiraLog _siraLog = null!;
		private GreetingsUtils _greetingsUtils = null!;
		private PluginMetadata _metadata = null!;
		private PluginConfig _pluginConfig = null!;
		private ISiraSyncService _siraSyncService = null!;
		private TimeTweeningManager _timeTweeningManager = null!;

		[Inject]
		public void Construct(SiraLog siraLog, GreetingsUtils greetingsUtils, UBinder<Plugin, PluginMetadata> metadata, PluginConfig pluginConfig, ISiraSyncService siraSyncService, TimeTweeningManager timeTweeningManager)
		{
			_siraLog = siraLog;
			_greetingsUtils = greetingsUtils;
			_metadata = metadata.Value;
			_pluginConfig = pluginConfig;
			_siraSyncService = siraSyncService;
			_timeTweeningManager = timeTweeningManager;
		}

		protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
		{
			base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);


			_greetingsUtils.ShowScreen(playOnComplete: false);

			if (_pluginConfig.ScreenDistance < 4.5f)
			{
				var screenPosition = _greetingsUtils.GreetingsScreen!.transform.position;
				_greetingsUtils.GreetingsScreen.transform.position = new Vector3(screenPosition.x, screenPosition.y, 4.5f);
				_greetingsUtils.MoveUnderline(_pluginConfig.ScreenDistance);
			}
		}

		protected override void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling)
		{
			base.DidDeactivate(removedFromHierarchy, screenSystemDisabling);

			if (_greetingsUtils.VideoPlayer != null)
			{
				_greetingsUtils.HideScreen();
				_greetingsUtils.VideoPlayer.isLooping = false;
				_greetingsUtils.VideoPlayer.loopPointReached -= VideoPlayer_loopPointReached;
			}

			_greetingsUtils.HideUnderline();
		}

		[UIValue("update-available")]
		private bool UpdateAvailable
		{
			get => _updateAvailable;
			set
			{
				_updateAvailable = value;
				NotifyPropertyChanged();
			}
		}

		[UIAction("#post-parse")]
		private async void PostParse()
		{
			var gitVersion = await _siraSyncService.LatestVersion();
				if (gitVersion != null && gitVersion > _metadata.HVersion)
				{
					_siraLog.Info($"{nameof(Greetings)} v{gitVersion} is available on GitHub!");
					_updateText.text = $"{nameof(Greetings)} v{gitVersion} is available on GitHub!";
					_updateText.alpha = 0f;
					UpdateAvailable = true;
					_timeTweeningManager.AddTween(new FloatTween(0f, 1f, val => _updateText.alpha = val, 0.4f, EaseType.InCubic), this);
				}
		}

		[UIAction("back-clicked")]
		private void RestartVideo()
		{
			var videoPlayer = _greetingsUtils.VideoPlayer;

			if (videoPlayer != null && videoPlayer.isPrepared)
			{
				videoPlayer.time = 0;
			}
		}

		[UIAction("play-or-pause-clicked")]
		private void ToggleVideo()
		{
			var videoPlayer = _greetingsUtils.VideoPlayer;

			if (videoPlayer != null && videoPlayer.isPlaying)
			{
				_greetingsUtils.PauseAndFadeInMenuMusic();
			}
			else if (videoPlayer != null && videoPlayer.isPrepared)
			{
				_greetingsUtils.PlayAndFadeOutMenuMusic();
			}
		}

		[UIAction("loop-clicked")]
		private void LoopVideo()
		{
			_loop = !_loop;
			_loopImage.DefaultColor = _loop ? _loopImage.HighlightColor : Color.white;

			if (_greetingsUtils.VideoPlayer != null)
			{
				_greetingsUtils.VideoPlayer.isLooping = _loop;
			}
		}

		private void VideoPlayer_loopPointReached(VideoPlayer source)
		{
			if (_greetingsUtils.VideoPlayer!.isLooping)
			{
				return;
			}

			_greetingsUtils.HideScreen();
			_greetingsUtils.VideoPlayer!.loopPointReached -= VideoPlayer_loopPointReached;
		}
	}
}
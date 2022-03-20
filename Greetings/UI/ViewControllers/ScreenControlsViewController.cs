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
    internal class ScreenControlsViewController : BSMLAutomaticViewController
    {
        private bool _loop;
        private bool _updateAvailable;
        
        [UIComponent("update-text")] 
        private readonly TextMeshProUGUI _updateText = null!;
        
        [UIComponent("play-or-pause-image")] 
        private readonly ClickableImage _playOrPauseImage = null!;
        
        [UIComponent("loop-image")] 
        private readonly ClickableImage _loopImage = null!;
        
        private SiraLog _siraLog = null!;
        private ScreenUtils _screenUtils = null!;
        private PluginMetadata _metadata = null!;
        private PluginConfig _pluginConfig = null!;
        private ISiraSyncService _siraSyncService = null!;
        private IPlatformUserModel _platformUserModel = null!;
        private TimeTweeningManager _timeTweeningManager = null!;

        [Inject]
        public void Construct(SiraLog siraLog, ScreenUtils screenUtils, UBinder<Plugin, PluginMetadata> metadata, PluginConfig pluginConfig, ISiraSyncService siraSyncService, IPlatformUserModel platformUserModel, TimeTweeningManager timeTweeningManager)
        {
            _siraLog = siraLog;
            _screenUtils = screenUtils;
            _metadata = metadata.Value;
            _pluginConfig = pluginConfig;
            _siraSyncService = siraSyncService;
            _platformUserModel = platformUserModel;
            _timeTweeningManager = timeTweeningManager;
        }

        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);

            _screenUtils.ShowScreen(playOnComplete: false);
        }

        protected override void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling)
        {
            base.DidDeactivate(removedFromHierarchy, screenSystemDisabling);

            if (_screenUtils.VideoPlayer != null)
            {
                _screenUtils.HideScreen();
                _screenUtils.VideoPlayer.loopPointReached -= VideoPlayer_loopPointReached;
            }
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
            if (!_updateAvailable)
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
            /* Fuck you Kryptec*/ if (_pluginConfig.EasterEggs && (await _platformUserModel.GetUserInfo()).platformUserId == "76561198200744503") _playOrPauseImage.SetImage("Greetings.Resources.FUCKUSPAM.png");
        }

        [UIAction("back-clicked")]
        private void RestartVideo()
        {
            var videoPlayer = _screenUtils.VideoPlayer;
            
            if (videoPlayer != null && videoPlayer.isPrepared)
            {
                videoPlayer.time = 0;
            }
        }
        
        [UIAction("play-or-pause-clicked")]
        private void ToggleVideo()
        {
            var videoPlayer = _screenUtils.VideoPlayer;
            
            if (videoPlayer != null && videoPlayer.isPlaying)
            {
                videoPlayer.Pause();
            }
            else if (videoPlayer != null && videoPlayer.isPrepared)
            {
                videoPlayer.Play();
            }
        }
        
        [UIAction("loop-clicked")]
        private void LoopVideo()
        {
            _loop = !_loop;
            _loopImage.DefaultColor = _loop ? _loopImage.HighlightColor : Color.white;

            if (_screenUtils.VideoPlayer != null)
            {
                _screenUtils.VideoPlayer.isLooping = _loop;
            }
        }

        private void VideoPlayer_loopPointReached(VideoPlayer source)
        {
            if (_screenUtils.VideoPlayer!.isLooping)
            {
                return;
            }
            
            _screenUtils.HideScreen();
            _screenUtils.VideoPlayer!.loopPointReached -= VideoPlayer_loopPointReached;
        }
    }
}
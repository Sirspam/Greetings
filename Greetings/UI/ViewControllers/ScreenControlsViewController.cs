using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.ViewControllers;
using Greetings.Utils;
using IPA.Loader;
using SiraUtil.Logging;
using SiraUtil.Web.SiraSync;
using SiraUtil.Zenject;
using TMPro;
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
        
        [UIComponent("loop-image")] 
        private readonly ClickableImage _loopImage = null!;
        
        private SiraLog _siraLog = null!;
        private ScreenUtils _screenUtils = null!;
        private PluginMetadata _metadata = null!;
        private ISiraSyncService _siraSyncService = null!;

        [Inject]
        public void Construct(SiraLog siraLog, ScreenUtils screenUtils, UBinder<Plugin, PluginMetadata> metadata,ISiraSyncService siraSyncService)
        {
            _siraLog = siraLog;
            _screenUtils = screenUtils;
            _metadata = metadata.Value;
            _siraSyncService = siraSyncService;
        }

        protected override void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling)
        {
            base.DidDeactivate(removedFromHierarchy, screenSystemDisabling);

            _screenUtils.HideScreen(false);
            _screenUtils.VideoPlayer!.loopPointReached -= VideoPlayer_loopPointReached;
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
            if (gitVersion == null || _metadata.HVersion >= gitVersion)
            {
                UpdateAvailable = false;
                return;
            }
            _siraLog.Info($"{nameof(Greetings)} v{gitVersion} is available on GitHub!");
            _updateText.text = $"{nameof(Greetings)} v{gitVersion} is available on GitHub!";
            UpdateAvailable = true;
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
            else
            {
                _screenUtils.ShowScreen();
                videoPlayer!.isLooping = _loop;
                videoPlayer.loopPointReached += VideoPlayer_loopPointReached;
            }
        }
        
        [UIAction("stop-clicked")]
        private void StopVideo()
        {
            var videoPlayer = _screenUtils.VideoPlayer;

            if (videoPlayer != null && videoPlayer.isPrepared)
            {
                _screenUtils.HideScreen();
                videoPlayer.isLooping = false;
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
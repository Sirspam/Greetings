using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.ViewControllers;
using Greetings.Utils;
using IPA.Loader;
using SiraUtil.Web.SiraSync;
using TMPro;
using UnityEngine.Video;
using Zenject;

namespace Greetings.UI.ViewControllers
{
    [HotReload(RelativePathToLayout = @"..\Views\ScreenControlsView")]
    [ViewDefinition("Greetings.UI.Views.ScreenControlsView.bsml")]
    internal class ScreenControlsViewController : BSMLAutomaticViewController
    {
        private ScreenUtils _screenUtils = null!;
        private ISiraSyncService _siraSyncService = null!;

        private bool _updateAvailable;

        [UIComponent("update-text")] 
        private readonly TextMeshProUGUI _updateText = null!;

        [Inject]
        public void Construct(ScreenUtils screenUtils, ISiraSyncService siraSyncService)
        {
            _screenUtils = screenUtils;
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
            if (gitVersion == null || PluginManager.GetPluginFromId(nameof(Greetings)).HVersion != gitVersion)
            {
                UpdateAvailable = false;
                return;
            }
            _updateText.text = $"{nameof(Greetings)} v{gitVersion} is available on GitHub!";
            UpdateAvailable = true;
        }

        [UIAction("pause-or-play-clicked")]
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

                videoPlayer!.loopPointReached += VideoPlayer_loopPointReached;
            }
        }

        private void VideoPlayer_loopPointReached(VideoPlayer source)
        {
            _screenUtils.HideScreen();
            _screenUtils.VideoPlayer!.loopPointReached -= VideoPlayer_loopPointReached;
        }
    }
}
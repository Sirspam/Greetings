using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.ViewControllers;
using Greetings.Configuration;
using Greetings.Utils;
using HMUI;
using IPA.Utilities;
using UnityEngine.UI;
using Zenject;
using static BeatSaberMarkupLanguage.Components.CustomListTableData;

namespace Greetings.UI.ViewControllers
{
    [HotReload(RelativePathToLayout = @"..\Views\VideoSelectionView")]
    [ViewDefinition("Greetings.UI.Views.VideoSelectionView.bsml")]
    internal class VideoSelectionViewController : BSMLAutomaticViewController
    {
        private UIUtils _uIUtils = null!;
        private ScreenUtils _screenUtils = null!;
        private PluginConfig _pluginConfig = null!;

        [UIComponent("video-list")] 
        private readonly CustomListTableData _videoList = null!;

        [UIComponent("open-folder-button")] 
        private readonly Button _openFolderButton = null!;

        [UIComponent("reload-videos-button")] 
        private readonly Button _reloadVideosButton = null!;

        [Inject]
        public void Construct(UIUtils uIUtils, ScreenUtils screenUtils, PluginConfig config)
        {
            _uIUtils = uIUtils;
            _pluginConfig = config;
            _screenUtils = screenUtils;
        }

        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);

            GetVideoListData();
        }

        [UIAction("video-clicked")]
        // ReSharper disable once UnusedParameter.Local
        private void VideoClicked(TableView tableView, int index)
        {
            _pluginConfig.SelectedVideo = _videoList.data[index].text + ".mp4";
            var videoPlayer = _screenUtils.VideoPlayer;
            if (videoPlayer != null && (videoPlayer.isPrepared || videoPlayer.isPlaying)) _screenUtils.HideScreen();
            // videoPlayer.url = Path.Combine(UnityGame.UserDataPath, nameof(Greetings), _pluginConfig.SelectedVideo);
        }

        [UIAction("open-folder-clicked")]
        private void OpenGreetingsFolder()
        {
            _uIUtils.ButtonUnderlineClick(_openFolderButton.gameObject);
            Process.Start(Path.Combine(UnityGame.UserDataPath, nameof(Greetings)));
        }

        [UIAction("reload-videos-clicked")]
        private void ReloadVideos()
        {
            _uIUtils.ButtonUnderlineClick(_reloadVideosButton.gameObject);
            GetVideoListData();
        }

        private void GetVideoListData()
        {
            var data = new List<CustomCellInfo>();

            // I have absolutely no clue what file formats the video player works with and
            // I'm too lazy to find them all so we'll just limit everything to mp4, just to be safe.
            var index = 0;
            var selectedFound = false;
            var files = new DirectoryInfo(Path.Combine(UnityGame.UserDataPath, nameof(Greetings))).GetFiles("*.mp4");

            if (files.Length == 0) return;

            foreach (var file in files)
            {
                if (file.Name != _pluginConfig.SelectedVideo)
                {
                    if (!selectedFound) index += 1;
                }
                else
                {
                    selectedFound = true;
                }

                data.Add(new CustomCellInfo(file.Name.Remove(file.Name.Length - 4), GetFileSize(file.Length)));
            }

            _videoList.data = data;
            _videoList.tableView.ReloadData();
            _videoList.tableView.SelectCellWithIdx(index);
        }
        
        private static string GetFileSize(long size)
        {
            if (size > 1000000000) 
                return $"{Math.Round((double)size / 1024 / 1024 / 1024, 0)} GB";
            if (size > 1000000) 
                return $"{Math.Round((double)size / 1024 / 1024, 0)} MB";
            if (size > 1000) 
                return $"{Math.Round((double)size / 1024, 0)} KB";
            return $"{size} Bytes";
        }
    }
}
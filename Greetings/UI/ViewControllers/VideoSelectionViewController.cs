using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.ViewControllers;
using Greetings.Configuration;
using Greetings.Utils;
using HMUI;
using IPA.Utilities;
using SiraUtil.Logging;
using Tweening;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using static BeatSaberMarkupLanguage.Components.CustomListTableData;

namespace Greetings.UI.ViewControllers
{
	[HotReload(RelativePathToLayout = @"..\Views\VideoSelectionView")]
	[ViewDefinition("Greetings.UI.Views.VideoSelectionView.bsml")]
	internal class VideoSelectionViewController : BSMLAutomaticViewController
	{
		private FileInfo _selectedFile = null!;

		[UIComponent("video-list")] private readonly CustomListTableData _videoList = null!;

		[UIComponent("open-folder-button")] private readonly Button _openFolderButton = null!;

		[UIComponent("reload-videos-button")] private readonly Button _reloadVideosButton = null!;

		[UIComponent("delete-video-button")] private readonly ClickableImage _deleteVideoButton = null!;

		private SiraLog _siraLog = null!;
		private UIUtils _uIUtils = null!;
		private ScreenUtils _screenUtils = null!;
		private PluginConfig _pluginConfig = null!;
		private TimeTweeningManager _timeTweeningManager = null!;
		private YesNoViewController _yesNoViewController = null!;

		[Inject]
		public void Construct(SiraLog siraLog, UIUtils uIUtils, ScreenUtils screenUtils, PluginConfig pluginConfig, TimeTweeningManager timeTweeningManager, YesNoViewController yesNoViewController)
		{
			_siraLog = siraLog;
			_uIUtils = uIUtils;
			_screenUtils = screenUtils;
			_pluginConfig = pluginConfig;
			_timeTweeningManager = timeTweeningManager;
			_yesNoViewController = yesNoViewController;
		}

		protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
		{
			base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);

			GetVideoListData();
		}

		[UIAction("#post-parse")]
		private void PostParse()
		{
			_deleteVideoButton.SetField<ImageView, float>("_skew", 0.2f);
		}

		[UIAction("video-clicked")]
		// ReSharper disable once UnusedParameter.Local
		private void VideoClicked(TableView tableView, int index)
		{
			var fileName = _videoList.data[index].text + ".mp4";
			_selectedFile = new FileInfo(_screenUtils.GreetingsPath + "\\" + fileName);
			_pluginConfig.SelectedVideo = fileName;

			var videoPlayer = _screenUtils.VideoPlayer;
			if (videoPlayer != null && (videoPlayer.isPrepared || videoPlayer.isPlaying))
				_screenUtils.HideScreen(reloadVideo: true);
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

		[UIAction("delete-video-clicked")]
		private void DeleteVideo()
		{
			_yesNoViewController.ShowModal(_deleteVideoButton.transform, "Are you sure you want to delete this video?", 5, DeleteSelectedVideo);
		}

		private void DeleteSelectedVideo()
		{
			try
			{
				_selectedFile.Delete();
				_siraLog.Info("Successfully deleted " + _selectedFile.Name);
				GetVideoListData();

				_timeTweeningManager.KillAllTweens(_deleteVideoButton);
				var tween = new FloatTween(0f, 1f, val => _deleteVideoButton.color = Color.Lerp(new Color(0f, 0.7f, 1f), new Color(1f, 1f, 1f, 1f), val), 1f, EaseType.InSine);
				_timeTweeningManager.AddTween(tween, _deleteVideoButton);
			}
			catch (Exception e)
			{
				_siraLog.Error("Failed to delete " + _selectedFile);
				_siraLog.Error(e);
			}
		}

		private void GetVideoListData()
		{
			var data = new List<CustomCellInfo>();

			// I have absolutely no clue what file formats the video player works with and
			// I'm too lazy to find them all so we'll just limit everything to mp4, just to be safe.
			var index = 0;
			var selectedFound = false;
			var files = new DirectoryInfo(_screenUtils.GreetingsPath).GetFiles("*.mp4");

			_deleteVideoButton.enabled = files.Length > 1;

			if (files.Length == 0)
			{
				// Video selection list will be reloaded before the greetings folder is populated again, leaving the list blank
				// but Greetings will still work if the game is restarted so this will do
				_pluginConfig.Changed();
			}
			
			foreach (var file in files)
			{
				if (file.Length > 100000000)
				{
					_siraLog.Warn($"Ignoring {file.Name} as it's above 100 MB");
					continue;
				}

				if (file.Name != _pluginConfig.SelectedVideo)
				{
					if (!selectedFound)
					{
						index += 1;

						if (index == files.Length)
						{
							index = 0;
							_selectedFile = file;
							selectedFound = true;
							VideoClicked(null!, 0);
						}
					}
				}
				else
				{
					_selectedFile = file;
					selectedFound = true;
				}

				data.Add(new CustomCellInfo(file.Name.Remove(file.Name.Length - 4), GetFileSize(file.Length), Utilities.ImageResources.BlankSprite));
			}

			_videoList.data = data;
			_videoList.tableView.ReloadData();
			_videoList.tableView.SelectCellWithIdx(index);
		}

		private static string GetFileSize(long size)
		{
			if (size > 1000000)
				return $"{Math.Round((double) size / 1024 / 1024, 0)} MB";
			if (size > 1000)
				return $"{Math.Round((double) size / 1024, 0)} KB";
			return $"{size} Bytes";
		}
	}
}
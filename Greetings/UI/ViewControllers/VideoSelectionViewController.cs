using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.Parser;
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
		private int _selectedVideoTab;
		private int _selectedQuitVideoIndex;
		private int _selectedStartVideoIndex;

		[UIParams] private readonly BSMLParserParams _parserParams = null!;
		[UIComponent("video-list")] private readonly CustomListTableData _videoList = null!;
		[UIComponent("bottom-buttons-layout")] private readonly HorizontalOrVerticalLayoutGroup _bottomButtonsLayout = null!;
		[UIComponent("open-folder-button")] private readonly Button _openFolderButton = null!;
		[UIComponent("reload-videos-button")] private readonly Button _reloadVideosButton = null!;
		[UIComponent("delete-video-button")] private readonly ClickableImage _deleteVideoButton = null!;

		private SiraLog _siraLog = null!;
		private UIUtils _uIUtils = null!;
		private GreetingsUtils _greetingsUtils = null!;
		private PluginConfig _pluginConfig = null!;
		private TimeTweeningManager _timeTweeningManager = null!;
		private YesNoViewController _yesNoViewController = null!;

		[Inject]
		public void Construct(SiraLog siraLog, UIUtils uIUtils, GreetingsUtils greetingsUtils, PluginConfig pluginConfig, TimeTweeningManager timeTweeningManager, YesNoViewController yesNoViewController)
		{
			_siraLog = siraLog;
			_uIUtils = uIUtils;
			_greetingsUtils = greetingsUtils;
			_pluginConfig = pluginConfig;
			_timeTweeningManager = timeTweeningManager;
			_yesNoViewController = yesNoViewController;
		}

		[UIValue("play-on-start")]
		private bool PlayOnStart
		{
			get => _pluginConfig.PlayOnStart;
			set => _pluginConfig.PlayOnStart = value;
		}
		
		[UIValue("random-start-video")]
		private bool RandomStartVideo
		{
			get => _pluginConfig.RandomStartVideo;
			set => _pluginConfig.RandomStartVideo = value;
		}
		
		[UIValue("play-on-quit")]
		private bool PlayOnQuit
		{
			get => _pluginConfig.PlayOnQuit;
			set => _pluginConfig.PlayOnQuit = value;
		}
		
		[UIValue("random-quit-video")]
		private bool RandomQuitVideo
		{
			get => _pluginConfig.RandomQuitVideo;
			set => _pluginConfig.RandomQuitVideo = value;
		}
		
		protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
		{
			base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);

			_selectedVideoTab = 0;
			_selectedQuitVideoIndex = 0;
			_selectedStartVideoIndex = 0;
			
			GetVideoListData(firstActivation);
		}

		[UIAction("#post-parse")]
		private void PostParse()
		{
			_deleteVideoButton.SetField<ImageView, float>("_skew", 0.2f);
			_bottomButtonsLayout.GetComponent<ImageView>().SetField("_skew", 0.18f);
		}

		[UIAction("video-tab-selected")]
		private void VideoTabSelected(SegmentedControl segmentedControl, int index)
		{
			_selectedVideoTab = index;

			switch (index)
			{
				case 0:
					_greetingsUtils.CurrentVideoType = GreetingsUtils.VideoType.StartVideo;
					_selectedFile = new FileInfo(_pluginConfig.VideoPath + "\\" + _videoList.data[_selectedStartVideoIndex].text + ".mp4");
					_videoList.tableView.SelectCellWithIdx(_selectedStartVideoIndex);
					break;
				case 1:
					_greetingsUtils.CurrentVideoType = GreetingsUtils.VideoType.QuitVideo;
					_selectedFile = new FileInfo(_pluginConfig.VideoPath + "\\" + _videoList.data[_selectedQuitVideoIndex].text + ".mp4");
					_videoList.tableView.SelectCellWithIdx(_selectedQuitVideoIndex);
					break;
			}
			_greetingsUtils.HideScreen(reloadVideo: true);
		}
		
		[UIAction("video-clicked")]
		// ReSharper disable once UnusedParameter.Local
		private void VideoClicked(TableView? tableView, int index)
		{
			var fileName = _videoList.data[index].text + ".mp4";
			_selectedFile = new FileInfo(_pluginConfig.VideoPath + "\\" + fileName);
			switch (_selectedVideoTab)
			{
				case 0:
					_pluginConfig.SelectedStartVideo = fileName;
					_selectedStartVideoIndex = index;
					break;
				case 1:
					_pluginConfig.SelectedQuitVideo = fileName;
					_selectedQuitVideoIndex = index;
					break;
			}

			var videoPlayer = _greetingsUtils.VideoPlayer;
			if (videoPlayer != null && (videoPlayer.isPrepared || videoPlayer.isPlaying))
			{
				_greetingsUtils.HideScreen(reloadVideo: true);
			}
		}

		[UIAction("open-folder-clicked")]
		private void OpenGreetingsFolder()
		{
			_uIUtils.ButtonUnderlineClick(_openFolderButton.gameObject);
			Process.Start(_pluginConfig.VideoPath);
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

		[UIAction("settings-clicked")]
		private void SettingsClicked()
		{
			_parserParams.EmitEvent("close-modal");
			_parserParams.EmitEvent("open-modal");
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

		private async void GetVideoListData(bool firstActivation = false)
		{
			var data = new List<CustomCellInfo>();

			// I have absolutely no clue what file formats the video player works with and
			// I'm too lazy to find them all so we'll just limit everything to mp4, just to be safe.
			var index = 0;
			var selectIndex = 0;
			var foundSelectedQuitVideo = false;
			var foundSelectedStartVideo = false;
			var files = new DirectoryInfo(_pluginConfig.VideoPath).GetFiles("*.mp4");

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
					// Had issues with the video player's prepare event just not being invoked if the video is too large.
					// No clue why it happens, don't care enough to fix it. I doubt anyone will be trying to watch Shrek or something with Greeting's tiny ass screen
					_siraLog.Warn($"Ignoring {file.Name} as it's above 100 MB");
					continue;
				}

				if (file.Name == _pluginConfig.SelectedStartVideo)
				{
					_selectedStartVideoIndex = index;
					foundSelectedStartVideo = true;
					
					if (_selectedVideoTab == 0)
					{
						_selectedFile = file;
						selectIndex = index;
					}
				}

				if (file.Name == _pluginConfig.SelectedQuitVideo)
				{
					_selectedQuitVideoIndex = index;
					foundSelectedQuitVideo = true;

					if (_selectedVideoTab == 1)
					{
						_selectedFile = file;
						selectIndex = index;
					}
				}

				index += 1;
				data.Add(new CustomCellInfo(file.Name.Remove(file.Name.Length - 4), GetFileSize(file.Length), Utilities.ImageResources.BlankSprite));
			}

			if (!foundSelectedStartVideo)
			{
				_selectedStartVideoIndex = 0;
				
				if (_selectedVideoTab == 0)
				{
					_selectedFile = files[0];
				}
			}

			if (!foundSelectedQuitVideo)
			{
				_selectedQuitVideoIndex = 0;
				
				if (_selectedVideoTab == 1)
				{
					_selectedFile = files[0];
				}
			}
			

			_videoList.data = data;
			_videoList.tableView.SelectCellWithIdx(selectIndex);
			
			// List is moved down a bit until refreshed, for whatever reason
			if (firstActivation)
			{
				await SiraUtil.Extras.Utilities.PauseChamp;	
			}
			_videoList.tableView.ReloadData();
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
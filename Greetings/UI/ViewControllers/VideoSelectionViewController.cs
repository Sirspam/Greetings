﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.ViewControllers;
using Greetings.Configuration;
using Greetings.UI.FlowCoordinator;
using Greetings.Utils;
using HMUI;
using SiraUtil.Logging;
using UnityEngine.UI;
using Zenject;
using static BeatSaberMarkupLanguage.Components.CustomListTableData;

namespace Greetings.UI.ViewControllers
{
	[HotReload(RelativePathToLayout = @"..\Views\VideoSelectionView")]
	[ViewDefinition("Greetings.UI.Views.VideoSelectionView.bsml")]
	internal sealed class VideoSelectionViewController : BSMLAutomaticViewController
	{
		private FileInfo _selectedFile = null!;
		private int _selectedVideoTab;
		private int _selectedQuitVideoIndex;
		private int _selectedStartVideoIndex;
		
		[UIComponent("video-list")] private readonly CustomListTableData _videoList = null!;
		[UIComponent("bottom-buttons-layout")] private readonly HorizontalOrVerticalLayoutGroup _bottomButtonsLayout = null!;
		[UIComponent("open-folder-button")] private readonly Button _openFolderButton = null!;
		[UIComponent("reload-videos-button")] private readonly Button _reloadVideosButton = null!;
		[UIComponent("delete-video-button")] private readonly ClickableImage _deleteVideoButton = null!;

		private SiraLog _siraLog = null!;
		private UIUtils _uiUtils = null!;
		private PluginConfig _pluginConfig = null!;
		private GreetingsUtils _greetingsUtils = null!;
		private MainFlowCoordinator _mainFlowCoordinator = null!;
		private NoVideosFlowCoordinator _noVideosFlowCoordinator = null!;
		private YesNoModalViewController _yesNoModalViewController = null!;

		[Inject]
		public void Construct(SiraLog siraLog, UIUtils uiUtils, GreetingsUtils greetingsUtils, PluginConfig pluginConfig, MainFlowCoordinator mainFlowCoordinator, NoVideosFlowCoordinator noVideosFlowCoordinator, YesNoModalViewController yesNoModalViewController)
		{
			_siraLog = siraLog;
			_uiUtils = uiUtils;
			_pluginConfig = pluginConfig;
			_greetingsUtils = greetingsUtils;
			_mainFlowCoordinator = mainFlowCoordinator;
			_noVideosFlowCoordinator = noVideosFlowCoordinator;
			_yesNoModalViewController = yesNoModalViewController;
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
			// TODO: To skew or not to skew
			// _deleteVideoButton._skew = 0.2f;
			// _bottomButtonsLayout.GetComponent<ImageView>()._skew = 0.18f;
		}

		[UIAction("video-tab-selected")]
		private void VideoTabSelected(SegmentedControl segmentedControl, int index)
		{
			_selectedVideoTab = index;

			switch (index)
			{
				case 0:
					_greetingsUtils.CurrentVideoType = GreetingsUtils.VideoType.StartVideo;
					_selectedFile = new FileInfo(Path.Combine(_pluginConfig.VideoPath, _videoList.Data[_selectedStartVideoIndex].Text) + ".mp4");
					_videoList.TableView.SelectCellWithIdx(_selectedStartVideoIndex);
					_videoList.TableView.ScrollToCellWithIdx(_selectedStartVideoIndex, TableView.ScrollPositionType.Center, false);
					break;
				case 1:
					_greetingsUtils.CurrentVideoType = GreetingsUtils.VideoType.QuitVideo;
					_selectedFile = new FileInfo(Path.Combine(_pluginConfig.VideoPath, _videoList.Data[_selectedQuitVideoIndex].Text) + ".mp4");
					_videoList.TableView.SelectCellWithIdx(_selectedQuitVideoIndex);
					_videoList.TableView.ScrollToCellWithIdx(_selectedQuitVideoIndex, TableView.ScrollPositionType.Center, false);
					break;
			}
			_greetingsUtils.HideScreen(reloadVideo: true);
		}
		
		[UIAction("video-clicked")]
		// ReSharper disable once UnusedParameter.Local
		private void VideoClicked(TableView? tableView, int index)
		{
			var fileName = _videoList.Data[index].Text + ".mp4";
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
			_uiUtils.ButtonUnderlineClick(_openFolderButton.gameObject);
			Process.Start(_pluginConfig.VideoPath);
		}

		[UIAction("reload-videos-clicked")]
		private void ReloadVideos()
		{
			_uiUtils.ButtonUnderlineClick(_reloadVideosButton.gameObject);
			GetVideoListData();
		}

		[UIAction("delete-video-clicked")]
		private void DeleteVideo()
		{
			const int displayNameLength = 14;
			
			var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(_selectedFile.Name);
			var displayName = fileNameWithoutExtension.Length > displayNameLength ? fileNameWithoutExtension.Substring(0, displayNameLength) + "..." : fileNameWithoutExtension;
			_yesNoModalViewController.ShowModal(_deleteVideoButton.transform, $"Are you sure you want to delete {displayName}?", 5, DeleteSelectedVideo);
		}
		
		private void DeleteSelectedVideo()
		{
			try
			{
				_selectedFile.Delete();
				_siraLog.Info("Successfully deleted " + _selectedFile.Name);
				GetVideoListData();
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

			if (_pluginConfig.CheckIfVideoPathEmpty())
			{
				_mainFlowCoordinator.DismissFlowCoordinator(_mainFlowCoordinator.YoungestChildFlowCoordinatorOrSelf(), () => _mainFlowCoordinator.PresentFlowCoordinator(_noVideosFlowCoordinator), immediately: true);
				return;
			}

			var files = _greetingsUtils.PopulateVideoList();
			foreach (var file in files)
			{
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
					_pluginConfig.SelectedStartVideo = _selectedFile.Name;
				}
			}

			if (!foundSelectedQuitVideo)
			{
				_selectedQuitVideoIndex = 0;
				
				if (_selectedVideoTab == 1)
				{
					_selectedFile = files[0];
					_pluginConfig.SelectedQuitVideo = _selectedFile.Name;
				}
			}
			

			_videoList.Data = data;
			_videoList.TableView.SelectCellWithIdx(selectIndex);
			_videoList.TableView.ScrollToCellWithIdx(selectIndex, TableView.ScrollPositionType.Center, false);
			
			// List is moved down a bit until refreshed, for whatever reason
			if (firstActivation)
			{
				await SiraUtil.Extras.Utilities.PauseChamp;	
			}
			_videoList.TableView.ReloadData();
		}

		private static string GetFileSize(long size)
		{
			return size switch
			{
				> 1000000000 => $"{Math.Round((double) size / 1024 / 1024 / 1024, 0)} GB",
				> 1000000 => $"{Math.Round((double) size / 1024 / 1024, 0)} MB",
				> 1000 => $"{Math.Round((double) size / 1024, 0)} KB",
				_ => $"{size} Bytes"
			};
		}
	}
}
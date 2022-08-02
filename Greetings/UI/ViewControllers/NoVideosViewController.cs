using System;
using System.Diagnostics;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.ViewControllers;
using Greetings.Configuration;
using Greetings.Utils;
using UnityEngine;
using Zenject;

namespace Greetings.UI.ViewControllers
{
	[ViewDefinition("Greetings.UI.Views.NoVideosView.bsml")]
	[HotReload(RelativePathToLayout = @"..\Views\NoVideosView.bsml")]
	internal class NoVideosViewController : BSMLAutomaticViewController
	{
		private int _reloadCount;
		public event Action? VideosAddedEvent;

		[UIObject("open-folder-button")] private readonly GameObject _openFolderButton = null!;
		[UIObject("reload-videos-button")] private readonly GameObject _reloadVideosButton = null!;
		
		private UIUtils _uiUtils = null!;
		private PluginConfig _pluginConfig = null!;

		[Inject]
		public void Construct(UIUtils uiUtils, PluginConfig pluginConfig)
		{
			_uiUtils = uiUtils;
			_pluginConfig = pluginConfig;
		}

		private int ReloadCount
		{
			get => _reloadCount;
			set
			{
				_reloadCount = value;
				NotifyPropertyChanged(nameof(NoVideosText));
			}
		}

		[UIValue("no-videos-text")]
		private string NoVideosText
		{
			get
			{
				switch (_reloadCount)
				{
					case 0:
					{
						return "No videos found!";
					}
					case 1:
					{
						return "Nope, still no videos.";
					}
					case 2:
					{
						return "Still nothing";
					}
					case 3:
					{
						return ":Nopers:";
					}
					case 4:
					{
						return "Are you sure you're putting in MP4 files?";
					}
					default:
					{
						_reloadCount = -1;
						return "If you're stuck, try asking for help in BSMG";
					}
				}
			}
		}

		private void OnEnable()
		{
			ReloadCount = 0;
		}

		[UIAction("open-folder-clicked")]
		private void OpenGreetingsFolder()
		{
			Process.Start(_pluginConfig.VideoPath);
			_uiUtils.ButtonUnderlineClick(_openFolderButton);
		}

		[UIAction("reload-videos-clicked")]
		private void ReloadVideos()
		{
			if (!_pluginConfig.CheckIfVideoPathEmpty())
			{
				VideosAddedEvent?.Invoke();
			}
			else
			{
				ReloadCount += 1;
			}
			
			_uiUtils.ButtonUnderlineClick(_reloadVideosButton);
			
		}
	}
}
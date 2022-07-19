using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.ViewControllers;
using Greetings.Configuration;
using Greetings.Utils;
using HMUI;
using IPA.Loader;
using SiraUtil.Zenject;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Greetings.UI.ViewControllers
{
	[HotReload(RelativePathToLayout = @"..\Views\GreetingsSettingsView")]
	[ViewDefinition("Greetings.UI.Views.GreetingsSettingsView.bsml")]
	internal class GreetingsSettingsViewController : BSMLAutomaticViewController
	{
		private bool _underlineActive;

		[UIComponent("top-panel")] private readonly HorizontalOrVerticalLayoutGroup _topPanel = null!;
		[UIComponent("underline-text")] private readonly Transform _underlineText = null!;
		[UIComponent("version-text")] private readonly CurvedTextMeshPro _versionText = null!;

		private UIUtils _uiUtils = null!;
		private GreetingsUtils _greetingsUtils = null!;
		private PluginConfig _pluginConfig = null!;
		private PluginMetadata _pluginMetadata = null!;
		private YesNoViewController _yesNoViewController = null!;

		[Inject]
		public void Construct(UIUtils uiUtils, GreetingsUtils greetingsUtils, PluginConfig pluginConfig, UBinder<Plugin, PluginMetadata> pluginMetadata, YesNoViewController yesNoViewController)
		{
			_uiUtils = uiUtils;
			_greetingsUtils = greetingsUtils;
			_pluginConfig = pluginConfig;
			_pluginMetadata = pluginMetadata.Value;
			_yesNoViewController = yesNoViewController;
		}

		#region Values
		
		[UIValue("underline-active")]
		private bool UnderlineActive
		{
			get => _underlineActive;
			set
			{
				if (_underlineActive != value)
				{
					_underlineActive = value;
					var textGameObject = _underlineText.gameObject;
					if (value)
					{
						NotifyPropertyChanged();
						_uiUtils.PresentPanelAnimation.ExecuteAnimation(textGameObject);
					}
					else
					{
						_uiUtils.DismissPanelAnimation.ExecuteAnimation(textGameObject, () => NotifyPropertyChanged());
					}
				}
			}
		}
		
		[UIValue("play-once")]
		private bool PlayOnce
		{
			get => _pluginConfig.PlayOnce;
			set => _pluginConfig.PlayOnce = value;
		}

		[UIValue("screen-distance")]
		private float ScreenDistance
		{
			get => _pluginConfig.ScreenDistance;
			set => _pluginConfig.ScreenDistance = value;
		}
		
		[UIValue("easter-eggs")]
		private bool EasterEggs
		{
			get => _pluginConfig.EasterEggs;
			set => _pluginConfig.EasterEggs = value;
		}

		[UIValue("await-fps")]
		private bool AwaitFps
		{
			get => _pluginConfig.AwaitFps;
			set => _pluginConfig.AwaitFps = value;
		}

		[UIValue("await-hmd")]
		private bool AwaitHmd
		{
			get => _pluginConfig.AwaitHmd;
			set => _pluginConfig.AwaitHmd = value;
		}

		[UIValue("await-songcore")]
		private bool AwaitSongCore
		{
			get => _pluginConfig.AwaitSongCore;
			set => _pluginConfig.AwaitSongCore = value;
		}

		[UIValue("target-fps")]
		private int TargetFps
		{
			get => _pluginConfig.TargetFps;
			set => _pluginConfig.TargetFps = value;
		}

		[UIValue("fps-streak")]
		private int FpsStreak
		{
			get => _pluginConfig.FpsStreak;
			set => _pluginConfig.FpsStreak = value;
		}

		[UIValue("max-wait-time")]
		private int MaxWaitTime
		{
			get => _pluginConfig.MaxWaitTime;
			set => _pluginConfig.MaxWaitTime = value;
		}

		[UIValue("version-text-value")]
		private string VersionText => $"{_pluginMetadata.Name} v{_pluginMetadata.HVersion} by {_pluginMetadata.Author}";

		#endregion

		protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
		{
			base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);
			
			if (firstActivation)
			{
				var imageView = _topPanel.GetComponent<ImageView>();
				imageView.color0 = imageView.color0.ColorWithAlpha(1f);
				imageView.color1 = imageView.color1.ColorWithAlpha(0f);
			}
			
			if (_pluginConfig.ScreenDistance < 4.5f)
			{
				UnderlineActive = true;
			}
		}

		[UIAction("move-screen")]
		private void MoveScreen(float value)
		{
			if (value > 4.25f)
			{
				_greetingsUtils.MoveScreen(value);
				_greetingsUtils.HideUnderline();
				UnderlineActive = false;
			}
			else
			{
				_greetingsUtils.MoveScreen(4.5f);
				_greetingsUtils.MoveUnderline(value);
				UnderlineActive = true;
			}
		}

		[UIAction("version-text-clicked")]
		private void VersionTextClicked()
		{
			if (_pluginMetadata.PluginHomeLink == null)
			{
				return;
			}
			
			_yesNoViewController.ShowModal(_versionText.transform, $"Open {_pluginMetadata.Name}'s GitHub page?", 6,
				() => Application.OpenURL(_pluginMetadata.PluginHomeLink!.ToString()));
		}
	}
}
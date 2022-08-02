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
		[UIComponent("reset-position-button")] private readonly Button _resetPositionButton = null!;
		[UIComponent("face-headset-button")] private readonly Button _faceHeadsetButton = null!;
		[UIComponent("set-upright-button")] private readonly Button _setUprightButton = null!;
		[UIComponent("underline-text")] private readonly Transform _underlineText = null!;
		[UIComponent("version-text")] private readonly CurvedTextMeshPro _versionText = null!;

		private UIUtils _uiUtils = null!;
		private GreetingsUtils _greetingsUtils = null!;
		private PluginConfig _pluginConfig = null!;
		private PluginMetadata _pluginMetadata = null!;
		private YesNoModalViewController _yesNoModalViewController = null!;
		private RandomVideoFloatingScreenController _randomVideoFloatingScreenController = null!;

		[Inject]
		public void Construct(UIUtils uiUtils, GreetingsUtils greetingsUtils, PluginConfig pluginConfig, UBinder<Plugin, PluginMetadata> pluginMetadata, YesNoModalViewController yesNoModalViewController, RandomVideoFloatingScreenController randomVideoFloatingScreenController)
		{
			_uiUtils = uiUtils;
			_greetingsUtils = greetingsUtils;
			_pluginConfig = pluginConfig;
			_pluginMetadata = pluginMetadata.Value;
			_yesNoModalViewController = yesNoModalViewController;
			_randomVideoFloatingScreenController = randomVideoFloatingScreenController;
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
		
		[UIValue("floating-screen-enabled")]
		private bool FloatingScreenEnabled
		{
			get => _pluginConfig.FloatingScreenEnabled;
			set
			{
				_pluginConfig.FloatingScreenEnabled = value;
				_randomVideoFloatingScreenController.SetFloatingScreenActive(value);
				NotifyPropertyChanged();
			}
		}

		[UIValue("handle-enabled")]
		private bool HandleEnabled
		{
			get => _pluginConfig.HandleEnabled;
			set
			{
				_pluginConfig.HandleEnabled = value;
				_randomVideoFloatingScreenController.SetHandleActive(value);
			}
		}

		[UIValue("floating-screen-scale")]
		private float FloatingScreenScale
		{
			get => _pluginConfig.FloatingScreenScale;
			set
			{
				_pluginConfig.FloatingScreenScale = FloatingScreenScale;
				_randomVideoFloatingScreenController.SetScale(value);
			}
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

		[UIAction("floating-screen-scale-formatter")]
		private string FloatingScreenScaleFormatter(float value)
		{
			return value * 100 + "%";
		}

		[UIAction("reset-position")]
		private void ResetPosition()
		{
			_randomVideoFloatingScreenController.ResetPosition();
			_uiUtils.ButtonUnderlineClick(_resetPositionButton.gameObject);
		}

		[UIAction("face-headset")]
		private void FaceHeadset()
		{
			_randomVideoFloatingScreenController.FaceHeadset();
			_uiUtils.ButtonUnderlineClick(_faceHeadsetButton.gameObject);
		}

		[UIAction("set-upright")]
		private void SetUpright()
		{
			_randomVideoFloatingScreenController.SetUpright();
			_uiUtils.ButtonUnderlineClick(_setUprightButton.gameObject);
		}

		[UIAction("version-text-clicked")]
		private void VersionTextClicked()
		{
			if (_pluginMetadata.PluginHomeLink == null)
			{
				return;
			}
			
			_yesNoModalViewController.ShowModal(_versionText.transform, $"Open {_pluginMetadata.Name}'s GitHub page?", 6,
				() => Application.OpenURL(_pluginMetadata.PluginHomeLink!.ToString()));
		}

		private void OnEnable() => _randomVideoFloatingScreenController.Interactable = false;

		private void OnDisable()
		{
			if (!_pluginConfig.FloatingScreenEnabled)
			{
				_randomVideoFloatingScreenController.Dispose();
			}
			else
			{
				_randomVideoFloatingScreenController.Interactable = true;
			}
		}
	}
}
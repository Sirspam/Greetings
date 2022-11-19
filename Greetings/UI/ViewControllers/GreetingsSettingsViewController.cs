using System;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components.Settings;
using BeatSaberMarkupLanguage.Parser;
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
		private enum RandomiserSliders
		{
			Min,
			Max
		}
		
		private bool _underlineActive;

		[UIComponent("top-panel")] private readonly HorizontalOrVerticalLayoutGroup _topPanel = null!;
		[UIComponent("reset-position-button")] private readonly Button _resetPositionButton = null!;
		[UIComponent("face-headset-button")] private readonly Button _faceHeadsetButton = null!;
		[UIComponent("set-upright-button")] private readonly Button _setUprightButton = null!;
		[UIComponent("underline-text")] private readonly Transform _underlineText = null!;
		[UIComponent("randomiser-modal-slider")] private readonly SliderSetting _randomiserModalSlider = null!;
		[UIComponent("version-text")] private readonly CurvedTextMeshPro _versionText = null!;
		
		[UIParams] private readonly BSMLParserParams _parserParams = null!;

		private UIUtils _uiUtils = null!;
		private PluginConfig _pluginConfig = null!;
		private GreetingsUtils _greetingsUtils = null!;
		private PluginMetadata _pluginMetadata = null!;
		private YesNoModalViewController _yesNoModalViewController = null!;
		private RandomVideoFloatingScreenController _randomVideoFloatingScreenController = null!;

		[Inject]
		public void Construct(UIUtils uiUtils, PluginConfig pluginConfig, GreetingsUtils greetingsUtils, UBinder<Plugin, PluginMetadata> pluginMetadata, YesNoModalViewController yesNoModalViewController, RandomVideoFloatingScreenController randomVideoFloatingScreenController)
		{
			_uiUtils = uiUtils;
			_pluginConfig = pluginConfig;
			_greetingsUtils = greetingsUtils;
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
				_pluginConfig.FloatingScreenScale = value;
				_randomVideoFloatingScreenController.SetScale(value);
			}
		}

		[UIValue("randomiser-enabled")]
		private bool RandomiserEnabled
		{
			get => _pluginConfig.RandomiserEnabled;
			set
			{
				_pluginConfig.RandomiserEnabled = value;
				NotifyPropertyChanged();
				NotifyPropertyChanged(nameof(RandomiserMinButtonInteractive));
				NotifyPropertyChanged(nameof(RandomiserMaxButtonInteractive));
			}
		}
		
		[UIValue("randomiser-min-minutes")]
		private string RandomiserMinMinutes => MinutesFormatter(_pluginConfig.RandomiserMinMinutes);

		[UIValue("randomiser-max-minutes")]
		private string RandomiserMaxMinutes => MinutesFormatter(_pluginConfig.RandomiserMaxMinutes);

		[UIValue("randomiser-min-button-interactive")]
		private bool RandomiserMinButtonInteractive => RandomiserEnabled && _pluginConfig.RandomiserMaxMinutes != 1;

		[UIValue("randomiser-max-button-interactive")]
		private bool RandomiserMaxButtonInteractive => RandomiserEnabled && _pluginConfig.RandomiserMinMinutes != 59;
		
		[UIValue("version-text-value")]
		private string VersionText => $"{_pluginMetadata.Name} v{_pluginMetadata.HVersion} by {_pluginMetadata.Author}";

		#endregion

		#region RandomiserModal

		private string _randomiserSliderText = null!;

		[UIValue("randomiser-slider-text")]
		private string RandomiserSliderText
		{
			get => _randomiserSliderText;
			set
			{
				_randomiserSliderText = value;
				NotifyPropertyChanged();
			}
		}
		
		private void ShowRandomiserModal(RandomiserSliders slider)
		{
			_randomiserModalSlider.slider.valueDidChangeEvent -= MinMinutesSliderDidChangeEvent;
			_randomiserModalSlider.slider.valueDidChangeEvent -= MaxMinutesSliderDidChangeEvent;
			
			if (slider == RandomiserSliders.Min)
			{
				RandomiserSliderText = "Min Minutes";
				_randomiserModalSlider.slider.minValue = 0;
				_randomiserModalSlider.slider.maxValue = _pluginConfig.RandomiserMaxMinutes - 1;
				_randomiserModalSlider.slider.value = _pluginConfig.RandomiserMinMinutes;
				_randomiserModalSlider.slider.valueDidChangeEvent += MinMinutesSliderDidChangeEvent;
			}
			else
			{
				RandomiserSliderText = "Max Minutes";
				_randomiserModalSlider.slider.minValue = _pluginConfig.RandomiserMinMinutes + 1;
				_randomiserModalSlider.slider.maxValue = 60;
				_randomiserModalSlider.slider.value = _pluginConfig.RandomiserMaxMinutes;
				_randomiserModalSlider.slider.valueDidChangeEvent += MaxMinutesSliderDidChangeEvent;
			}
			
			_parserParams.EmitEvent("open-modal");
		}

		private void MinMinutesSliderDidChangeEvent(RangeValuesTextSlider slider, float value)
		{
			_pluginConfig.RandomiserMinMinutes = (int) Math.Round(value);
			NotifyPropertyChanged(nameof(RandomiserMinMinutes));
			NotifyPropertyChanged(nameof(RandomiserMaxButtonInteractive));
		}
		
		private void MaxMinutesSliderDidChangeEvent(RangeValuesTextSlider slider, float value)
		{
			_pluginConfig.RandomiserMaxMinutes = (int) Math.Round(value);
			NotifyPropertyChanged(nameof(RandomiserMaxMinutes));
			NotifyPropertyChanged(nameof(RandomiserMinButtonInteractive));
		}

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

		[UIAction("edit-min-time-clicked")]
		private void EditMinTimeClicked() => ShowRandomiserModal(RandomiserSliders.Min);
		
		[UIAction("edit-max-time-clicked")]
		private void EditMaxTimeClicked() => ShowRandomiserModal(RandomiserSliders.Max);

		
		[UIAction("minutes-formatter")]
		private string MinutesFormatter(int value)
		{
			return value == 1 ? "1 Minute" : $"{value} Minutes";
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
			_randomiserModalSlider.slider.valueDidChangeEvent -= MinMinutesSliderDidChangeEvent;
			_randomiserModalSlider.slider.valueDidChangeEvent -= MaxMinutesSliderDidChangeEvent;
			
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
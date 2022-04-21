using System.Threading.Tasks;
using HMUI;
using Tweening;
using UnityEngine;

namespace Greetings.Utils
{
	internal class UIUtils
	{
		private PanelAnimationSO? _presentPanelAnimation;
		private PanelAnimationSO? _dismissPanelAnimation;
		private readonly TimeTweeningManager _uwuTweenyManager; // Thanks once again, PixelBoom
		private ColorsOverrideSettingsPanelController _colorsOverrideSettingsPanelController;
		
		public UIUtils(TimeTweeningManager timeTweeningManager, GameplaySetupViewController gameplaySetupViewController)
		{
			_uwuTweenyManager = timeTweeningManager;
			_colorsOverrideSettingsPanelController = Accessors.ColorsPanelAccessor(ref gameplaySetupViewController);
		}
		
		public PanelAnimationSO PresentPanelAnimation => _presentPanelAnimation ??= Accessors.PresentAnimationAccessor(ref _colorsOverrideSettingsPanelController);
		public PanelAnimationSO DismissPanelAnimation => _dismissPanelAnimation ??= Accessors.DismissAnimationAccessor(ref _colorsOverrideSettingsPanelController);

		public async void ButtonUnderlineClick(GameObject gameObject)
		{
			var underline = await Task.Run(() => gameObject.transform.Find("Underline").gameObject.GetComponent<ImageView>());

			_uwuTweenyManager.KillAllTweens(underline);

			var tween = new FloatTween(0f, 1f, val => underline.color = Color.Lerp(new Color(0f, 0.7f, 1f), new Color(1f, 1f, 1f, 0.502f), val), 1f, EaseType.InSine);
			_uwuTweenyManager.AddTween(tween, underline);
		}
	}
}
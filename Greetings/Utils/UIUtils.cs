using System.Threading.Tasks;
using HMUI;
using Tweening;
using UnityEngine;

namespace Greetings.Utils
{
	internal class UIUtils
	{
		private Color? _defaultUnderlineColor;
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

        public void ButtonUnderlineClick(GameObject gameObject)
        {
            var underline = gameObject.transform.Find("Underline").gameObject.GetComponent<ImageView>();
            _defaultUnderlineColor ??= underline.color;

            _uwuTweenyManager.KillAllTweens(underline);
            var tween = new FloatTween(0f, 1f, val => underline.color = Color.Lerp(new Color(0f, 0.75f, 1f), (Color) _defaultUnderlineColor, val), 0.6f, EaseType.InQuad);
            _uwuTweenyManager.AddTween(tween, underline);
        }
	}
}
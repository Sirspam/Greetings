using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.FloatingScreen;
using BeatSaberMarkupLanguage.ViewControllers;
using Greetings.Utils;
using HMUI;
using Tweening;
using UnityEngine;
using Zenject;

namespace Greetings.UI.ViewControllers
{
	[ViewDefinition("Greetings.UI.Views.FloorTextView.bsml")]
	[HotReload(RelativePathToLayout = @"..\Views\FloorTextView.bsml")]
	internal class FloorTextViewController : BSMLAutomaticViewController
	{
		private FloatingScreen? _floatingScreen;

		[UIComponent("skip-text")] 
		private readonly CurvedTextMeshPro _skipText = null!;

		[UIComponent("fps-text")] 
		private readonly CurvedTextMeshPro _fpsText = null!;

		private CheeseUtils _cheeseUtils = null!;
		private TimeTweeningManager _timeTweeningManager = null!;

		[Inject]
		public void Construct(CheeseUtils cheeseUtils, TimeTweeningManager timeTweeningManager)
		{
			_cheeseUtils = cheeseUtils;
			_timeTweeningManager = timeTweeningManager;
		}

		public enum TextChange
		{
			ShowSkipText,
			ShowFpsText,
			HideSkipText,
			HideFpsText
		}

		private void CreateScreen()
		{
			if (_floatingScreen != null)
			{
				return;
			}

			_floatingScreen = FloatingScreen.CreateFloatingScreen(new Vector2(105f, 50f), false, new Vector3(0f, 0.1f, 1.5f), new Quaternion(0.5f,0f,0f, 1f));
			_floatingScreen.name = "GreetingsFloorTextFloatingScreen";
			
			_floatingScreen.SetRootViewController(this, AnimationType.None);

			if (_cheeseUtils.TheTimeHathCome)
			{
				_skipText.text = "Greetings cannot be skipped with Trigger or Left Mouse";
			}
		}
		
		public void ChangeText(TextChange action)
		{
			if (_floatingScreen == null)
			{
				CreateScreen();
			}

			FloatTween tween;
			switch (action)
			{
				case TextChange.ShowSkipText:
					tween = new FloatTween(0f, 1f, val => _skipText.alpha = val, 0.5f, EaseType.InOutQuad);
					break;
				case TextChange.ShowFpsText:
					tween = new FloatTween(0f, 1f, val => _fpsText.alpha = val, 0.5f, EaseType.InOutQuad);
					break;
				case TextChange.HideSkipText:
					tween = new FloatTween(1f, 0f, val => _skipText.alpha = val, 0.5f, EaseType.InOutQuad);
					break;
				case TextChange.HideFpsText:
					tween = new FloatTween(1f, 0f, val => _fpsText.alpha = val, 0.5f, EaseType.InOutQuad);
					break;
				default:
					return;
			}

			_timeTweeningManager.AddTween(tween, this);
		}

		public void HideScreen()
		{
			if (_floatingScreen == null)
			{
				return;
			}

			var cg = GetComponent<CanvasGroup>();
			if (cg == null)
			{
				Destroy(_floatingScreen.gameObject);
				return;
			}

			_timeTweeningManager.KillAllTweens(this);
			var tween = new FloatTween(1f, 0f, val => cg.alpha = val, 0.5f, EaseType.InOutQuad)
			{
				onCompleted = delegate
				{
					// This shouldn't be used again after the greeting, so we'll chuck it
					Destroy(_floatingScreen.gameObject);
				}
			};
			_timeTweeningManager.AddTween(tween, this);
		}
	}
}
using System;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.FloatingScreen;
using BeatSaberMarkupLanguage.ViewControllers;
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

		[UIComponent("top-text")] private readonly CurvedTextMeshPro _topText = null!;

		[UIComponent("bottom-text")] private readonly CurvedTextMeshPro _bottomText = null!;
		
		private TimeTweeningManager _timeTweeningManager = null!;

		[Inject]
		public void Construct(TimeTweeningManager timeTweeningManager)
		{
			_timeTweeningManager = timeTweeningManager;
		}

		public enum VisibilityChange
		{
			ShowTopText,
			ShowBottomText,
			HideTopText,
			HideBottomText
		}

		public enum TextChange
		{
			SkipText,
			AwaitingVideoPreparationText,
			AwaitingHmdFocus,
			AwaitingSongCore,
			AwaitingFpsStabilisation
		}

		public enum ChangeSpeed
		{
			Slow,
			Quick
		}

		private void OnEnable()
		{
			CreateScreen();
		}

		private void CreateScreen()
		{
			if (_floatingScreen != null)
			{
				return;
			}

			_floatingScreen = FloatingScreen.CreateFloatingScreen(new Vector2(105f, 50f), false, new Vector3(0f, 0.1f, 1.5f), new Quaternion(0.5f, 0f, 0f, 1f));
			_floatingScreen.name = "GreetingsFloorTextFloatingScreen";

			_floatingScreen.SetRootViewController(this, AnimationType.None);
			_topText.alpha = 0;
			_bottomText.alpha = 0;
		}

		public void ChangeTextTo(TextChange action)
		{
			if (_floatingScreen == null)
			{
				CreateScreen();
			}

			switch (action)
			{
				case TextChange.SkipText:
					ChangeVisibility(VisibilityChange.HideTopText, ChangeSpeed.Quick, () =>
					{
						_topText.text = "Greetings can be skipped with Trigger or Left Mouse";
						ChangeVisibility(VisibilityChange.ShowTopText);
					});
					break;
				case TextChange.AwaitingVideoPreparationText:
					ChangeVisibility(VisibilityChange.HideBottomText, ChangeSpeed.Quick, () =>
					{
						_bottomText.text = "Awaiting Video Preparation";
						ChangeVisibility(VisibilityChange.ShowBottomText);
					});
					break;
				case TextChange.AwaitingHmdFocus:
					ChangeVisibility(VisibilityChange.HideBottomText, ChangeSpeed.Quick, () =>
					{
						_bottomText.text = "Awaiting HMD Focus";
						ChangeVisibility(VisibilityChange.ShowBottomText);
					});
					break;
				case TextChange.AwaitingSongCore:
					ChangeVisibility(VisibilityChange.HideBottomText, ChangeSpeed.Quick, () =>
					{
						_bottomText.text = "Awaiting SongCore";
						ChangeVisibility(VisibilityChange.ShowBottomText);
					});
					break;
				case TextChange.AwaitingFpsStabilisation:
					ChangeVisibility(VisibilityChange.HideBottomText, ChangeSpeed.Quick, () =>
					{
						_bottomText.text = "Awaiting FPS Stabilisation";
						ChangeVisibility(VisibilityChange.ShowBottomText);
					});
					break;
				default:
					return;
			}
		}
		
		public void ChangeVisibility(VisibilityChange action, ChangeSpeed speed = ChangeSpeed.Slow, Action? finishedCallback = null)
		{
			if (_floatingScreen == null)
			{
				CreateScreen();
			}
			
			float duration;
			switch (speed)
			{
				case ChangeSpeed.Quick:
					duration = 0.05f;
					break;
				default: case ChangeSpeed.Slow:
					duration = 0.5f;
					break;
			}

			FloatTween tween;
			object owner;
			switch (action)
			{
				case VisibilityChange.ShowTopText:
					if (_topText.alpha.Equals(1))
					{
						finishedCallback?.Invoke();
						return;
					}
					
					tween = new FloatTween(0f, 1f, val => _topText.alpha = val, duration, EaseType.InOutQuad);
					owner = _topText;
					break;
				case VisibilityChange.ShowBottomText:
					if (_topText.alpha.Equals(1))
					{
						finishedCallback?.Invoke();
						return;
					}
					
					tween = new FloatTween(0f, 1f, val => _bottomText.alpha = val, duration, EaseType.InOutQuad);
					owner = _bottomText;
					break;
				case VisibilityChange.HideTopText:
					if (_topText.alpha.Equals(0))
					{
						finishedCallback?.Invoke();
						return;
					}
					
					tween = new FloatTween(1f, 0f, val => _topText.alpha = val, duration, EaseType.InOutQuad);
					owner = _bottomText;
					break;
				case VisibilityChange.HideBottomText:
					if (_topText.alpha.Equals(0))
					{
						finishedCallback?.Invoke();
						return;
					}
					
					tween = new FloatTween(1f, 0f, val => _bottomText.alpha = val, duration, EaseType.InOutQuad);
					owner = _bottomText;
					break;
				default:
					return;
			}

			tween.onCompleted = finishedCallback;
			_timeTweeningManager.KillAllTweens(owner);
			_timeTweeningManager.AddTween(tween, owner);
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
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
	internal sealed class FloorTextFloatingScreenController : BSMLAutomaticViewController
	{
		private bool _topTextShowing;
		private bool _bottomTextShowing;
		private FloatingScreen? _floatingScreen;

		[UIComponent("top-text")] private readonly CurvedTextMeshPro _topText = null!;
		[UIComponent("bottom-text")] private readonly CurvedTextMeshPro _bottomText = null!;
		
		private TimeTweeningManager _timeTweeningManager = null!;

		[Inject]
		public void Construct(TimeTweeningManager timeTweeningManager) => _timeTweeningManager = timeTweeningManager;

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

		private void CreateScreen()
		{
			if (_floatingScreen != null)
			{
				_floatingScreen.gameObject.SetActive(true);
				return;
			}

			_floatingScreen = FloatingScreen.CreateFloatingScreen(new Vector2(105f, 50f), false, new Vector3(0f, 0.1f, 1.5f), new Quaternion(0.5f, 0f, 0f, 1f));
			_floatingScreen.name = "GreetingsFloorTextFloatingScreen";

			_floatingScreen.SetRootViewController(this, AnimationType.None);
			_topText.alpha = 0;
			_topTextShowing = false;
			_bottomText.alpha = 0;
			_bottomTextShowing = false;
		}

		public void ChangeTextTo(TextChange action)
		{
			switch (action)
			{
				case TextChange.SkipText:
				{
					ChangeVisibility(VisibilityChange.HideTopText, ChangeSpeed.Quick, () =>
					{
						_topText.text = "Greetings can be skipped with Trigger or Left Mouse";
						ChangeVisibility(VisibilityChange.ShowTopText);
					});
					break;
				}
				case TextChange.AwaitingVideoPreparationText:
				{
					ChangeVisibility(VisibilityChange.HideBottomText, ChangeSpeed.Quick, () =>
					{
						_bottomText.text = "Awaiting Video Preparation";
						ChangeVisibility(VisibilityChange.ShowBottomText);
					});
					break;
				}
				case TextChange.AwaitingHmdFocus:
				{
					ChangeVisibility(VisibilityChange.HideBottomText, ChangeSpeed.Quick, () =>
					{
						_bottomText.text = "Awaiting HMD Focus";
						ChangeVisibility(VisibilityChange.ShowBottomText);
					});
					break;
				}
				case TextChange.AwaitingSongCore:
				{
					ChangeVisibility(VisibilityChange.HideBottomText, ChangeSpeed.Quick, () =>
					{
						_bottomText.text = "Awaiting SongCore";
						ChangeVisibility(VisibilityChange.ShowBottomText);
					});
					break;
				}
				case TextChange.AwaitingFpsStabilisation:
				{
					ChangeVisibility(VisibilityChange.HideBottomText, ChangeSpeed.Quick, () =>
					{
						_bottomText.text = "Awaiting FPS Stabilisation";
						ChangeVisibility(VisibilityChange.ShowBottomText);
					});
					break;
				}
				default:
				{
					return;
				}
			}
		}
		
		public void ChangeVisibility(VisibilityChange action, ChangeSpeed speed = ChangeSpeed.Slow, Action? finishedCallback = null)
		{
			CreateScreen();
			
			float duration;
			switch (speed)
			{
				case ChangeSpeed.Quick:
				{
					duration = 0.05f;
					break;
				}
				default:
				case ChangeSpeed.Slow:
				{
					duration = 0.5f;
					break;	
				}
			}

			FloatTween tween;
			object owner;
			switch (action)
			{
				case VisibilityChange.ShowTopText:
				{
					if (_topTextShowing)
					{
						finishedCallback?.Invoke();
						return;
					}
					
					tween = new FloatTween(0f, 1f, val => _topText.alpha = val, duration, EaseType.InOutQuad);
					owner = _topText;
					_topTextShowing = true;
					break;
				}
				case VisibilityChange.ShowBottomText:
				{
					if (_bottomTextShowing)
					{
						finishedCallback?.Invoke();
						return;
					}
					
					tween = new FloatTween(0f, 1f, val => _bottomText.alpha = val, duration, EaseType.InOutQuad);
					owner = _bottomText;
					_bottomTextShowing = true;
					break;
				}
				case VisibilityChange.HideTopText:
				{
					if (!_topTextShowing)
					{
						finishedCallback?.Invoke();
						return;
					}
					
					tween = new FloatTween(1f, 0f, val => _topText.alpha = val, duration, EaseType.InOutQuad);
					owner = _topText;
					_topTextShowing = false;
					break;
				}
				case VisibilityChange.HideBottomText:
				{
					if (!_bottomTextShowing)
					{
						finishedCallback?.Invoke();
						return;
					}
					
					tween = new FloatTween(1f, 0f, val => _bottomText.alpha = val, duration, EaseType.InOutQuad);
					owner = _bottomText;
					_bottomTextShowing = false;
					break;
				}
				default:
				{
					return;
				}
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

			if (_topTextShowing)
			{
				ChangeVisibility(VisibilityChange.HideTopText, finishedCallback: () => _floatingScreen.gameObject.SetActive(false));
				ChangeVisibility(VisibilityChange.HideBottomText);
			}
			else if (_bottomTextShowing)
			{
				ChangeVisibility(VisibilityChange.HideTopText);
				ChangeVisibility(VisibilityChange.HideBottomText, finishedCallback: () => _floatingScreen.gameObject.SetActive(false));
			}
			else
			{
				_floatingScreen.gameObject.SetActive(false);
			}
		}
	}
}
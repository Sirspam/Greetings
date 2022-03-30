using System.Reflection;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Parser;
using HMUI;
using UnityEngine;

namespace Greetings.UI.ViewControllers
{
	public class DeleteConfirmationViewController
	{
		public delegate void ButtonPressed();

		private ButtonPressed _yesButtonPressed = null!;

		[UIComponent("modal")] private readonly ModalView _modalView = null!;

		[UIParams] private readonly BSMLParserParams _parserParams = null!;

		private void Parse(Component parentTransform)
		{
			BSMLParser.instance.Parse(Utilities.GetResourceContent(Assembly.GetExecutingAssembly(), "Greetings.UI.Views.DeleteConfirmationModalView.bsml"), parentTransform.gameObject, this);
			_modalView.name = "GreetingsDeleteConfirmationModal";
		}

		public void ShowModal(Transform parentTransform, ButtonPressed yesButtonCallback)
		{
			_yesButtonPressed = yesButtonCallback;
			Parse(parentTransform);
			_parserParams.EmitEvent("close-modal");
			_parserParams.EmitEvent("open-modal");
		}

		[UIAction("yes-clicked")]
		private void YesClicked()
		{
			_yesButtonPressed.Invoke();
			_parserParams.EmitEvent("close-modal");
		}

		[UIAction("no-clicked")]
		private void NoClicked()
		{
			_parserParams.EmitEvent("close-modal");
		}
	}
}
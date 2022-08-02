using System.ComponentModel;
using System.Reflection;
using System.Threading.Tasks;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Parser;
using Greetings.Utils;
using HMUI;
using UnityEngine;
using Component = UnityEngine.Component;

namespace Greetings.UI.ViewControllers
{
	public class YesNoModalViewController : INotifyPropertyChanged
	{
		private bool _parsed;
		private int _fontSize;
		private string _text = null!;

		public delegate void ButtonPressed();
		private ButtonPressed _yesButtonPressed = null!;
		
		public event PropertyChangedEventHandler? PropertyChanged;

		[UIComponent("modal")] private ModalView _modalView = null!;

		[UIParams] private readonly BSMLParserParams _parserParams = null!;

		[UIValue("text")]
		private string Text
		{
			get => _text;
			set
			{
				_text = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Text)));
			}
		}

		[UIValue("font-size")]
		private int FontSize
		{
			get => _fontSize;
			set
			{
				_fontSize = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FontSize)));
			}
		}

		private async Task Parse(Component parentTransform)
		{
			if (!_parsed)
			{
				BSMLParser.instance.Parse(Utilities.GetResourceContent(Assembly.GetExecutingAssembly(), "Greetings.UI.Views.YesNoModalView.bsml"), parentTransform.gameObject, this);
				_modalView.name = "GreetingsYesNoModal";				
				_parsed = true;
				return;
			}
			
			if (_modalView.gameObject.activeInHierarchy)
			{
				_parserParams.EmitEvent("close-modal");
				await SiraUtil.Extras.Utilities.PauseChamp;
			}
			
			_modalView.transform.SetParent(parentTransform.transform);
			Accessors.ViewValidAccessor(ref _modalView) = false;
		}

		public async void ShowModal(Transform parentTransform, string text, int fontSize, ButtonPressed yesButtonCallback)
		{
			await Parse(parentTransform);
			
			Text = text;
			FontSize = fontSize;
			_yesButtonPressed = yesButtonCallback;
			
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
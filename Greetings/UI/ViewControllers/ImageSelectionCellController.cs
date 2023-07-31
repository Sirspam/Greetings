using System;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using Greetings.Utils;
using HMUI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Greetings.UI.ViewControllers
{
	internal sealed class ImageSelectionCellController : TableCell, IPointerEnterHandler, IPointerExitHandler
	{
		[UIComponent("image")]
		private readonly ImageView _imageView = null!;
		
		private MaterialGrabber _materialGrabber = null!;

		public void Construct(MaterialGrabber materialGrabber)
		{
			_materialGrabber = materialGrabber;

			_imageView.material = _materialGrabber.NoGlowRoundEdge;
		}
		
		public ImageSelectionCellController PopulateCell (string imagePath)
		{
			if (imagePath == "Greetings Icon")
			{
				_imageView.SetImage("Greetings.Resources.Greetings.png");
			}
			else if (imagePath.EndsWith(".gif", StringComparison.OrdinalIgnoreCase) || imagePath.EndsWith(".apng", StringComparison.OrdinalIgnoreCase))
			{
				_imageView.SetImage(imagePath);
			}
			else
			{
				var scaleOptions = new BeatSaberUI.ScaleOptions
				{
					Height = 128,
					Width = 128,
					ShouldScale = true,
					MaintainRatio = true
				};
				_imageView.SetImage(imagePath, true, scaleOptions);
			}
			
			return this;
		}

		public override void OnPointerEnter(PointerEventData eventData)
		{
			base.OnPointerEnter(eventData);
			
			_imageView.color = new Color(0.60f, 0.80f, 1);
		}
		
		public override void OnPointerExit(PointerEventData eventData)
		{
			base.OnPointerExit(eventData);
			
			_imageView.color = Color.white;
		}
	}
}
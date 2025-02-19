﻿using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using Greetings.Utils;
using HMUI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Greetings.UI.ViewControllers
{
	internal sealed class ImageSelectionCellController : TableCell, INotifyPropertyChanged
	{
		[UIComponent("image")]
		private readonly ImageView _imageView = null!;
		
		private MaterialGrabber _materialGrabber = null!;
		
		public event PropertyChangedEventHandler? PropertyChanged;

		private string _imageName = string.Empty;
		[UIValue("image-name")]
		private string ImageName
		{
			get => _imageName;
			set
			{
				_imageName = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ImageName)));
			}
		}

		public void Construct(MaterialGrabber materialGrabber)
		{
			_materialGrabber = materialGrabber;

			_imageView.material = _materialGrabber.NoGlowRoundEdge;
		}
		
		public async Task<ImageSelectionCellController> PopulateCell (string imagePath)
		{
			if (imagePath == "Greetings Icon")
			{
				await _imageView.SetImageAsync("Greetings.Resources.Greetings.png");
			}
			else if (imagePath.EndsWith(".gif", StringComparison.OrdinalIgnoreCase) || imagePath.EndsWith(".apng", StringComparison.OrdinalIgnoreCase))
			{
				await _imageView.SetImageAsync(imagePath);
			}
			else
			{
				var scaleOptions = new BeatSaberUI.ScaleOptions
				{
					Height = 256,
					Width = 256,
					ShouldScale = true,
					MaintainRatio = true
				};
				await _imageView.SetImageAsync(imagePath, true, scaleOptions);
			}
			
			ImageName = Path.GetFileNameWithoutExtension(imagePath);
			
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
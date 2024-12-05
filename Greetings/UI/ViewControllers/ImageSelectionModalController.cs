using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.Parser;
using Greetings.Configuration;
using Greetings.Utils;
using HMUI;
using IPA.Utilities.Async;
using SiraUtil.Logging;
using UnityEngine;

namespace Greetings.UI.ViewControllers
{
	internal sealed class ImageSelectionModalController : TableView.IDataSource
	{
		private bool _parsed;
		private readonly List<string> _files = new(){"Greetings Icon"};
		
		[UIComponent("modal")] private readonly ModalView _modalView = null!;

		[UIComponent("image-list")] private readonly CustomListTableData _customListTableData = null!;

		[UIParams] private readonly BSMLParserParams _parserParams = null!;

		private readonly SiraLog _siraLog;
		private readonly PluginConfig _pluginConfig;
		private readonly MaterialGrabber _materialGrabber;
		private readonly RandomVideoFloatingScreenController _randomVideoFloatingScreenController;

		public ImageSelectionModalController(SiraLog siraLog, PluginConfig pluginConfig, MaterialGrabber materialGrabber, RandomVideoFloatingScreenController randomVideoFloatingScreenController)
		{
			_siraLog = siraLog;
			_pluginConfig = pluginConfig;
			_materialGrabber = materialGrabber;
			_randomVideoFloatingScreenController = randomVideoFloatingScreenController;
		}

		private void Parse(Component parentTransform)
		{
			if (!_parsed)
			{
				_files.AddRange(Directory.GetFiles(Plugin.FloatingScreenImagesPath).Where(file => file.EndsWith(".png", StringComparison.OrdinalIgnoreCase) || file.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) || file.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || file.EndsWith(".gif", StringComparison.OrdinalIgnoreCase) || file.EndsWith(".apng", StringComparison.OrdinalIgnoreCase)).ToList());
				
				BSMLParser.Instance.Parse(Utilities.GetResourceContent(Assembly.GetExecutingAssembly(), "Greetings.UI.Views.ImageSelectionModalView.bsml"), parentTransform.gameObject, this);
				_modalView.name = "GreetingsImageSelectionModal";

				_parsed = true;
			}
		}

		public void ShowModal(Transform parentTransform)
		{
			Parse(parentTransform);
			_parserParams.EmitEvent("close-modal");
			_parserParams.EmitEvent("open-modal");
			
			_customListTableData.TableView.ClearSelection();

			for (var i = 0; i < _files.Count; i++)
			{
				if (_files[i] == _pluginConfig.FloatingScreenImage)
				{
					_customListTableData.TableView.ScrollToCellWithIdx(i, TableView.ScrollPositionType.Beginning, false);
					break;
				}
			}
		}
		
		[UIAction("#post-parse")]
		private async void PostParse()
		{
			_customListTableData.TableView.SetDataSource(this, true);
			
			await SiraUtil.Extras.Utilities.PauseChamp;	
			_customListTableData!.TableView.ReloadData();
		}
		
		[UIAction("cell-selected")]
		private async Task CellSelected(TableView tableView, int index)
		{
			var file = _files[index];
			
			if (file == "Greetings Icon")
			{
				_pluginConfig.FloatingScreenImage = null;
				await _randomVideoFloatingScreenController.SetImage(null);
			}
			else
			{
				_pluginConfig.FloatingScreenImage = file;
				await _randomVideoFloatingScreenController.SetImage(file);
			}
			
			BeatSaberUI.BasicUIAudioManager.HandleButtonClickEvent();
		}
		
		private const string ReuseId = "GreetingsImageSelectionCell";

		private ImageSelectionCellController GetCell()
		{
			var tableCell = _customListTableData.TableView.DequeueReusableCellForIdentifier(ReuseId);

			if (tableCell is null)
			{
				var imageCell = new GameObject(nameof(ImageSelectionCellController), typeof(Touchable)).AddComponent<ImageSelectionCellController>();
				BSMLParser.Instance.Parse(Utilities.GetResourceContent(Assembly.GetExecutingAssembly(), "Greetings.UI.Views.ImageSelectionCellView.bsml"), imageCell.gameObject, imageCell);
				imageCell.Construct(_materialGrabber);
				imageCell.interactable = true;
				imageCell.reuseIdentifier = ReuseId;

				tableCell = imageCell;
			}

			return (ImageSelectionCellController) tableCell;
		}

		public float CellSize(int idx)
		{
			return 21f;
		}

		public int NumberOfCells()
		{
			return _files.Count;
		}

		public TableCell CellForIdx(TableView tableView, int idx)
		{
			var cell = GetCell();
			UnityMainThreadTaskScheduler.Factory.StartNew(async () => await cell.PopulateCell(_files[idx]));
			return cell;
		}
	}
}
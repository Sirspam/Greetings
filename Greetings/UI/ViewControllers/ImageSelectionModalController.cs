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
using HMUI;
using SiraUtil.Logging;
using UnityEngine;

namespace Greetings.UI.ViewControllers
{
	internal sealed class ImageSelectionModalController
	{
		private bool _parsed;
		
		[UIComponent("modal")] private ModalView _modalView = null!;

		[UIComponent("image-list")] private CustomListTableData _customListTableData = null!;

		[UIParams] private readonly BSMLParserParams _parserParams = null!;

		private readonly SiraLog _siraLog;
		private readonly PluginConfig _pluginConfig;
		private readonly RandomVideoFloatingScreenController _randomVideoFloatingScreenController;

		public ImageSelectionModalController(PluginConfig pluginConfig, RandomVideoFloatingScreenController randomVideoFloatingScreenController, SiraLog siraLog)
		{
			_pluginConfig = pluginConfig;
			_randomVideoFloatingScreenController = randomVideoFloatingScreenController;
			_siraLog = siraLog;
		}

		private void Parse(Component parentTransform)
		{
			if (!_parsed)
			{
				BSMLParser.instance.Parse(Utilities.GetResourceContent(Assembly.GetExecutingAssembly(), "Greetings.UI.Views.ImageSelectionModalView.bsml"), parentTransform.gameObject, this);
				_modalView.name = "GreetingsImageSelectionModal";
				_parsed = true;
			}
		}

		public void ShowModal(Transform parentTransform)
		{
			Parse(parentTransform);
			_parserParams.EmitEvent("close-modal");
			_parserParams.EmitEvent("open-modal");
			_customListTableData.tableView.ClearSelection();
			PopulateList();
		}

		private void PopulateList()
		{
			var selectedIndex = 0;
			var iteration = 0;
			var data = new List<CustomListTableData.CustomCellInfo> {new CustomListTableData.CustomCellInfo("Greetings Icon")};
			Utilities.GetData("Greetings.Resources.Greetings.png", bytes => data[0].icon = Utilities.LoadSpriteRaw(bytes));
			
			var files = Directory.GetFiles(Plugin.FloatingScreenImagesPath).Where(file => file.EndsWith(".png") || file.EndsWith(".jpeg") || file.EndsWith(".jpg")).ToArray();
			foreach (var file in files)
			{
				iteration++;
				var fileName = Path.GetFileName(file);
				if (fileName == _pluginConfig.FloatingScreenImage)
				{
					selectedIndex = iteration;
				}
				
				// This downscales the image on the main branch. Can't do it for 1.29.1 because of BSML.
				data.Add(new CustomListTableData.CustomCellInfo(fileName, icon: Utilities.LoadSpriteRaw(File.ReadAllBytes(file))));
			}
			
			
			_customListTableData.data = data;
			_customListTableData.tableView.ReloadData();
			_customListTableData.tableView.SelectCellWithIdx(selectedIndex);
			_customListTableData.tableView.ScrollToCellWithIdx(selectedIndex, TableView.ScrollPositionType.Center, false);
		}
		
		[UIAction("cell-selected")]
		private void CellSelected(TableView tableView, int index)
		{
			if (index == 0)
			{
				_pluginConfig.FloatingScreenImage = null;
				_randomVideoFloatingScreenController.SetImage(null);
			}
			else
			{
				var file = _customListTableData.data[index].text;
				_pluginConfig.FloatingScreenImage = file;
				_randomVideoFloatingScreenController.SetImage(file);
			}
		}
	}
}
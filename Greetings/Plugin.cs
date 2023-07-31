using System.IO;
using Greetings.Configuration;
using Greetings.Installers;
using IPA;
using IPA.Config;
using IPA.Config.Stores;
using IPA.Logging;
using IPA.Utilities;
using SiraUtil.Zenject;

namespace Greetings
{
	[Plugin(RuntimeOptions.DynamicInit)]
	[NoEnableDisable]
	public sealed class Plugin
	{
		public static readonly string BaseVideoPath = Path.Combine(UnityGame.UserDataPath, nameof(Greetings));
		public static readonly string FloatingScreenImagesPath = Path.Combine(BaseVideoPath, "FloatingScreen Images");

		[Init]
		public Plugin(Config conf, Logger logger, Zenjector zenjector)
		{
			Directory.CreateDirectory(BaseVideoPath);
			Directory.CreateDirectory(FloatingScreenImagesPath);
			
			zenjector.UseLogger(logger);
			zenjector.UseMetadataBinder<Plugin>();
			zenjector.UseSiraSync();

			zenjector.Install<GreetingsMenuInstaller>(Location.Menu, conf.Generated<PluginConfig>());
		}
	}
}
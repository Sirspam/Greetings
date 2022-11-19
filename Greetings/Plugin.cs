using Greetings.Configuration;
using Greetings.Installers;
using IPA;
using IPA.Config;
using IPA.Config.Stores;
using IPA.Logging;
using SiraUtil.Zenject;

namespace Greetings
{
	[Plugin(RuntimeOptions.DynamicInit)]
	[NoEnableDisable]
	public sealed class Plugin
	{
		[Init]
		public Plugin(Config conf, Logger logger, Zenjector zenjector)
		{
			zenjector.UseLogger(logger);
			zenjector.UseMetadataBinder<Plugin>();
			zenjector.UseSiraSync();

			zenjector.Install<GreetingsMenuInstaller>(Location.Menu, conf.Generated<PluginConfig>());
		}
	}
}
using System;
using Greetings.Configuration;
using Zenject;

namespace Greetings.Utils
{
	internal class CheeseUtils : IInitializable
	{
		public bool TheTimeHathCome;

		private readonly PluginConfig _pluginConfig;

		public CheeseUtils(PluginConfig pluginConfig)
		{
			_pluginConfig = pluginConfig;
		}

		public void Initialize()
		{
			if (_pluginConfig.EasterEggs && DateTime.Now.Day == 1 && DateTime.Now.Month == 4)
			{
				TheTimeHathCome = true;
			}
		}
	}
}
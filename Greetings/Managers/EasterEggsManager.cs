using System;
using Greetings.Configuration;
using HMUI;
using IPA.Utilities;
using UnityEngine;
using Zenject;
using Random = System.Random;

namespace Greetings.Managers
{
	// I'll put other funny things here when I think of them
	internal sealed class EasterEggsManager : IInitializable, IDisposable
	{
		private float? _originalYScale;
		
		private readonly Random _random;
		private readonly PluginConfig _pluginConfig;
		private readonly ScreenSystem _screenSystem;
		private readonly GreetingsScreenManager _greetingsScreenManager;

		public EasterEggsManager(PluginConfig pluginConfig, HierarchyManager hierarchyManager, GreetingsScreenManager greetingsScreenManager)
		{
			_random = new Random();
			_pluginConfig = pluginConfig;
			_screenSystem = hierarchyManager.GetField<ScreenSystem, HierarchyManager>("_screenSystem");
			_greetingsScreenManager = greetingsScreenManager;
		}

		public void Initialize()
		{
			if (_pluginConfig.EasterEggs && DateTime.Now is {Month: 4, Day: 1})
			{
				_greetingsScreenManager.GreetingsHidden += AprilFoolsEasterEgg;
			}
		}

		public void Dispose()
		{
			_greetingsScreenManager.GreetingsHidden -= AprilFoolsEasterEgg;

			if (_originalYScale is not null)
			{
				SetScreenSystemYScale((float) _originalYScale);
			}
		}
		
		// Hey don't look at this if it's before April
		// You'll ruin the joke for yourself
		// Shoo, scram!
		// (Unless you're reviewing this for BeatMods approval, then hi hello hiii!! thanks for looking at my silly code) 
		private void AprilFoolsEasterEgg()
		{
			const float max = 2.5f;
			const float min = 0.5f;
			
			SetScreenSystemYScale((float) _random.NextDouble() * (max - min) + min);
		}

		private void SetScreenSystemYScale(float yScale)
		{
			var transform = _screenSystem.transform;
			var localScale = transform.localScale;
			_originalYScale ??= localScale.y;
			
			transform.localScale = new Vector3(localScale.x, yScale, localScale.z);
		}
	}
}
using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using BeatSaberMarkupLanguage;
using IPA.Config.Stores;
using IPA.Utilities;
using UnityEngine.XR;

[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]

namespace Greetings.Configuration
{
	internal class PluginConfig
	{
		public virtual string SelectedVideo { get; set; } = "Greetings.mp4";
		public virtual bool RandomVideo { get; set; } = false;
		public virtual bool PlayOnce { get; set; } = true;
		public virtual float ScreenDistance { get; set; } = 6f;
		public virtual bool EasterEggs { get; set; } = true;
		public virtual bool AwaitFps { get; set; } = true;
		public virtual bool AwaitHmd { get; set; } = true;
		public virtual bool AwaitSongCore { get; set; } = true;
		public virtual int TargetFps { get; set; } = GetDefaultTargetFps();
		public virtual int FpsStreak { get; set; } = 8;
		public virtual int MaxWaitTime { get; set; } = 10;

		public virtual void OnReload()
		{
			FixConfigIssues();
		}

		public virtual void Changed()
		{
			FixConfigIssues();
		}

		private void FixConfigIssues()
		{
			var folderPath = Path.Combine(UnityGame.UserDataPath, nameof(Greetings));
			Directory.CreateDirectory(folderPath);

			if (!File.Exists(Path.Combine(folderPath, SelectedVideo)))
			{
				var files = new DirectoryInfo(folderPath).GetFiles("*.mp4");
				if (files.Length == 0)
				{
					File.WriteAllBytes(Path.Combine(folderPath, "Greetings.mp4"), Utilities.GetResource(Assembly.GetExecutingAssembly(), "Greetings.Resources.Greetings.mp4"));
				}
				else
				{
					SelectedVideo = files[0].Name;
				}
			}
		}

		private static int GetDefaultTargetFps()
		{
			var refreshRate = Convert.ToInt16(XRDevice.refreshRate - 10);
			if (refreshRate <= 0)
			{
				refreshRate = 60;
			}

			return refreshRate;
		}
	}
}
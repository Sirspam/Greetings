using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using BeatSaberMarkupLanguage;
using Greetings.UI.ViewControllers;
using IPA.Config.Stores;
using IPA.Utilities;
using UnityEngine;
using UnityEngine.XR;

[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]

namespace Greetings.Configuration
{
	internal class PluginConfig
	{
		public virtual string VideoPath { get; set; } = BaseGameVideoPath;
		public virtual string SelectedStartVideo { get; set; } = "Greetings.mp4";
		public virtual string SelectedQuitVideo { get; set; } = "Greetings.mp4";
		public virtual bool RandomStartVideo { get; set; } = false;
		public virtual bool RandomQuitVideo { get; set; } = false;
		public virtual bool PlayOnStart { get; set; } = true;
		public virtual bool PlayOnQuit { get; set; } = true;
		public virtual bool PlayOnce { get; set; } = true;
		public virtual float ScreenDistance { get; set; } = 6f;
		public virtual bool EasterEggs { get; set; } = true;
		public virtual bool AwaitFps { get; set; } = true;
		public virtual bool AwaitHmd { get; set; } = true;
		public virtual bool AwaitSongCore { get; set; } = true;
		public virtual int TargetFps { get; set; } = GetDefaultTargetFps();
		public virtual int FpsStreak { get; set; } = 5;
		public virtual int MaxWaitTime { get; set; } = 10;
		public virtual bool FloatingScreenEnabled { get; set; } = false;
		public virtual bool HandleEnabled { get; set; } = true;
		public virtual float FloatingScreenScale { get; set; } = 1f;
		public virtual Vector3 FloatingScreenPosition { get; set; } = RandomVideoFloatingScreenController.DefaultPosition;
		public virtual Quaternion FloatingScreenRotation { get; set; } = RandomVideoFloatingScreenController.DefaultRotation;

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
			if (!Directory.Exists(VideoPath))
			{
				VideoPath = BaseGameVideoPath;
			}
			
			Directory.CreateDirectory(VideoPath);

			if (!File.Exists(Path.Combine(VideoPath, SelectedStartVideo)))
			{
				var files = new DirectoryInfo(VideoPath).GetFiles("*.mp4");
				if (files.Length == 0)
				{
					WriteGreetingsVideoToDisk(VideoPath);
				}
				else
				{
					SelectedStartVideo = files[0].Name;
				}
			}
			
			if (!File.Exists(Path.Combine(VideoPath, SelectedQuitVideo)))
			{
				var files = new DirectoryInfo(VideoPath).GetFiles("*.mp4");
				if (files.Length == 0)
				{
					WriteGreetingsVideoToDisk(VideoPath);
				}
				else
				{
					SelectedQuitVideo = files[0].Name;
				}
			}
		}

		private void WriteGreetingsVideoToDisk(string folderPath)
		{
			File.WriteAllBytes(Path.Combine(folderPath, "Greetings.mp4"), Utilities.GetResource(Assembly.GetExecutingAssembly(), "Greetings.Resources.Goodnight.mp4"));
		}

		private static string BaseGameVideoPath => Path.Combine(UnityGame.UserDataPath, nameof(Greetings));

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
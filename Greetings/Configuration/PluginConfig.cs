using System;
using System.IO;
using System.Runtime.CompilerServices;
using Greetings.UI.ViewControllers;
using IPA.Config.Stores;
using IPA.Config.Stores.Attributes;
using UnityEngine;
using UnityEngine.XR;

[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]

namespace Greetings.Configuration
{
	internal class PluginConfig
	{
		public virtual string VideoPath { get; set; } = Plugin.BaseVideoPath;
		public virtual string? SelectedStartVideo { get; set; }
		public virtual string? SelectedQuitVideo { get; set; }
		public virtual bool RandomStartVideo { get; set; } = false;
		public virtual bool RandomQuitVideo { get; set; } = false;
		public virtual bool PlayOnStart { get; set; } = true;
		public virtual bool PlayOnQuit { get; set; } = false;
		public virtual bool PlayOnce { get; set; } = true;
		public virtual float ScreenDistance { get; set; } = 5.5f;
		public virtual bool EasterEggs { get; set; } = true;
		public virtual bool AwaitFps { get; set; } = true;
		public virtual bool AwaitHmd { get; set; } = true;
		public virtual bool AwaitSongCore { get; set; } = true;
		public virtual int TargetFps { get; set; } = GetDefaultTargetFps();
		public virtual int FpsStreak { get; set; } = 5;
		public virtual int MaxWaitTime { get; set; } = 10;
		public virtual bool FloatingScreenEnabled { get; set; } = false;
		public virtual bool HandleEnabled { get; set; } = false;
		public virtual float FloatingScreenScale { get; set; } = 1f;
		public virtual Vector3 FloatingScreenPosition { get; set; } = RandomVideoFloatingScreenController.DefaultPosition;
		public virtual Quaternion FloatingScreenRotation { get; set; } = RandomVideoFloatingScreenController.DefaultRotation;
		public virtual string? FloatingScreenImage { get; set; } = null; // Uses mod icon if null
		public virtual bool RandomiserEnabled { get; set; } = false;
		public virtual int RandomiserMinMinutes { get; set; } = 5;
		public virtual int RandomiserMaxMinutes { get; set; } = 30;
		
		[Ignore]
		public bool IsVideoPathEmpty;

		public virtual void Changed() => FixConfigIssues();

		public bool CheckIfVideoPathEmpty()
		{
			var files = new DirectoryInfo(VideoPath).GetFiles("*.mp4");
			if (files.Length > 0)
			{
				IsVideoPathEmpty = false;
				if (SelectedStartVideo == null || !File.Exists(Path.Combine(VideoPath, SelectedStartVideo)))
				{
					SelectedStartVideo = files[0].Name;
				}
				
				if (SelectedQuitVideo == null || !File.Exists(Path.Combine(VideoPath, SelectedQuitVideo)))
				{
					SelectedQuitVideo = files[0].Name;
				}
			}
			else
			{
				IsVideoPathEmpty = true;
				// Stops IPA from writing to the config when it isn't necessary
				if (SelectedStartVideo != null)
				{
					SelectedStartVideo = null;	
				}

				if (SelectedQuitVideo != null)
				{
					SelectedQuitVideo = null;	
				}
			}

			return IsVideoPathEmpty;
		}
		
		private void FixConfigIssues()
		{
			if (!Directory.Exists(VideoPath))
			{
				VideoPath = Plugin.BaseVideoPath;
			}

			if (FloatingScreenImage is not null && !File.Exists(Path.Combine(Plugin.FloatingScreenImagesPath, FloatingScreenImage)))
			{
				FloatingScreenImage = null;
			}
			
			Directory.CreateDirectory(VideoPath);
			Directory.CreateDirectory(Plugin.FloatingScreenImagesPath);

			CheckIfVideoPathEmpty();
		}

		private static int GetDefaultTargetFps()
		{
			var refreshRate = Convert.ToInt16(XRDevice.refreshRate - 10);
			return refreshRate <= 0 ? 60 : refreshRate;
		}
	}
}
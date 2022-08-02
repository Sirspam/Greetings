using System;
using System.IO;
using System.Runtime.CompilerServices;
using Greetings.UI.ViewControllers;
using IPA.Config.Stores;
using IPA.Config.Stores.Attributes;
using IPA.Utilities;
using UnityEngine;
using UnityEngine.XR;

[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]

namespace Greetings.Configuration
{
	internal class PluginConfig
	{
		public virtual string VideoPath { get; set; } = BaseGameVideoPath;
		public virtual string? SelectedStartVideo { get; set; }
		public virtual string? SelectedQuitVideo { get; set; }
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
		[Ignore]
		public bool IsVideoPathEmpty;

		public virtual void OnReload() => FixConfigIssues();

		public bool CheckIfVideoPathEmpty()
		{
			var files = new DirectoryInfo(VideoPath).GetFiles("*.mp4");
			if (files.Length > 0)
			{
				IsVideoPathEmpty = false;
				SelectedStartVideo ??= files[0].Name;
				SelectedQuitVideo ??= files[0].Name;
			}
			else
			{
				IsVideoPathEmpty = true;
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
				VideoPath = BaseGameVideoPath;
			}
			
			Directory.CreateDirectory(VideoPath);

			CheckIfVideoPathEmpty();
		}
		
		private static string BaseGameVideoPath => Path.Combine(UnityGame.UserDataPath, nameof(Greetings));

		private static int GetDefaultTargetFps()
		{
			var refreshRate = Convert.ToInt16(XRDevice.refreshRate - 10);
			return refreshRate <= 0 ? 60 : refreshRate;
		}
	}
}
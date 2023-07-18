﻿using System.Collections;
using Greetings.Managers;
using Greetings.UI.ViewControllers;
using SiraUtil.Logging;
using UnityEngine;
using UnityEngine.XR;
using Zenject;

namespace Greetings.Components
{
	internal sealed class SkipController : MonoBehaviour
	{
		private SiraLog _siraLog = null!;
		private GreetingsScreenManager _greetingsScreenManager = null!;
		private VRControllersInputManager _vrControllersInputManager = null!;
		private FloorTextFloatingScreenController _floorTextFloatingScreenController = null!;

		[Inject]
		public void Construct(SiraLog siraLog, GreetingsScreenManager greetingsScreenManager, VRControllersInputManager vrControllersInputManager, FloorTextFloatingScreenController floorTextFloatingScreenController)
		{
			_siraLog = siraLog;
			_greetingsScreenManager = greetingsScreenManager;
			_vrControllersInputManager = vrControllersInputManager;
			_floorTextFloatingScreenController = floorTextFloatingScreenController;
		}

		public void StartCoroutine()
		{
			enabled = true;
			_floorTextFloatingScreenController.ChangeTextTo(FloorTextFloatingScreenController.TextChange.SkipText);
			StartCoroutine(AwaitKeyCoroutine());
		}

		private IEnumerator AwaitKeyCoroutine()
		{
			while (_vrControllersInputManager.TriggerValue(XRNode.LeftHand) <= 0.8f && _vrControllersInputManager.TriggerValue(XRNode.RightHand) <= 0.8f && !Input.GetKeyDown(KeyCode.Mouse0))
			{
				yield return null;
			}
			
			_siraLog.Info("Skip requested");
			_greetingsScreenManager.GreetingsUtils.SkipRequested = true;
			_greetingsScreenManager.VideoEnded();
			
			// Terrible fix to the Greetings screen not disappearing correctly if skipped at a frame specific time
			yield return new WaitForSeconds(3);
			if (_greetingsScreenManager.GreetingsUtils.GreetingsScreen != null)
			{
				_greetingsScreenManager.GreetingsUtils.GreetingsScreen.SetActive(false);
			}

			enabled = false;
		}
	}
}
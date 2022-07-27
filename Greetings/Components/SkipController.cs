using System.Collections;
using Greetings.Managers;
using Greetings.UI.ViewControllers;
using SiraUtil.Logging;
using UnityEngine;
using UnityEngine.XR;
using Zenject;

namespace Greetings.Components
{
	internal class SkipController : MonoBehaviour
	{
		private SiraLog _siraLog = null!;
		private GreetingsScreenManager _greetingsScreenManager = null!;
		private FloorTextFloatingScreenController _floorTextFloatingScreenController = null!;
		private VRControllersInputManager _vrControllersInputManager = null!;

		[Inject]
		public void Construct(SiraLog siraLog, GreetingsScreenManager greetingsScreenManager, FloorTextFloatingScreenController floorTextFloatingScreenController, VRControllersInputManager vrControllersInputManager)
		{
			_siraLog = siraLog;
			_greetingsScreenManager = greetingsScreenManager;
			_floorTextFloatingScreenController = floorTextFloatingScreenController;
			_vrControllersInputManager = vrControllersInputManager;
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
			_greetingsScreenManager.SkipRequested = true;
			_greetingsScreenManager.VideoEnded();
			enabled = false;
		}
	}
}
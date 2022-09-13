using System;
using System.Collections;
using Greetings.Configuration;
using Greetings.UI.FlowCoordinator;
using Greetings.Utils;
using SiraUtil.Logging;
using UnityEngine;
using Zenject;
using Random = System.Random;

namespace Greetings.Managers
{
	internal class RandomiserManager : MonoBehaviour, IDisposable
	{
		private bool _awoken;
		private DateTime _itsGreetinTime; // Stand back, I'm beginning to greet!
		private Coroutine? _timerCoroutine;
		private DateTime? _previouslyActiveTime;

		private Random _random = null!;
		private SiraLog _siraLog = null!;
		private PluginConfig _pluginConfig = null!;
		private GameScenesManager _gameScenesManager = null!;
		private ResultsViewController _resultsViewController = null!;
		private GreetingsScreenManager _greetingsScreenManager = null!;
		private GreetingsFlowCoordinator _greetingsFlowCoordinator = null!;

		[Inject]
		public void Construct(SiraLog siraLog, PluginConfig pluginConfig, GameScenesManager gameScenesManager, ResultsViewController resultsViewController, GreetingsScreenManager greetingsScreenManager, GreetingsFlowCoordinator greetingsFlowCoordinator)
		{
			_random = new Random();
			_siraLog = siraLog;
			_pluginConfig = pluginConfig;
			_gameScenesManager = gameScenesManager;
			_resultsViewController = resultsViewController;
			_greetingsScreenManager = greetingsScreenManager;
			_greetingsFlowCoordinator = greetingsFlowCoordinator;
		}

		private IEnumerator TimerCoroutine()
		{
			yield return new WaitUntil(() => _itsGreetinTime <= DateTime.Now);
			_siraLog.Info("Playing Greetings");
			_greetingsScreenManager.StartGreetings(GreetingsUtils.VideoType.RandomVideo, () =>
			{
				SetNewGreetingsTime();
				_timerCoroutine = StartCoroutine(TimerCoroutine());
			});
		}
		
		private IEnumerator AwaitResultsShenanigans(Action callback)
		{
			yield return _gameScenesManager.waitUntilSceneTransitionFinish;
			yield return new WaitUntil(() => !_resultsViewController.isActiveAndEnabled);
			callback.Invoke();
		}

		private void SetNewGreetingsTime()
		{
			_itsGreetinTime = DateTime.Now.AddSeconds(_random.Next(_pluginConfig.RandomiserMinMinutes * 60, _pluginConfig.RandomiserMaxMinutes * 60));
			LogTimeUntilGreetings();
		}

		private void AddTimeFromPreviouslyActive()
		{
			if (_previouslyActiveTime != null)
			{
				_itsGreetinTime += DateTime.Now - _previouslyActiveTime.Value;
				_previouslyActiveTime = null;
				LogTimeUntilGreetings();
			}
			else
			{
				SetNewGreetingsTime();
			}
		}

		private void LogTimeUntilGreetings()
		{
			var timeSpan = _itsGreetinTime - DateTime.Now;
			_siraLog.Info($"Playing Greetings in {timeSpan:mm\\:ss}");
		}

		private void GreetingsFlowCoordinatorOnGreetingsFlowCoordinatorActiveChangedEvent(bool active)
		{
			if (active)
			{
				gameObject.SetActive(false);
			}
			else
			{
				if (_pluginConfig.RandomiserEnabled)
				{
					_previouslyActiveTime = null;
					gameObject.SetActive(true);
				}
			}
		}

		private void Awake()
		{
			_greetingsFlowCoordinator.GreetingsFlowCoordinatorActiveChangedEvent += GreetingsFlowCoordinatorOnGreetingsFlowCoordinatorActiveChangedEvent;
			
			if (!_pluginConfig.RandomiserEnabled)
			{
				gameObject.SetActive(false);
			}
			else if (!_greetingsScreenManager.HasStartGreetingsPlayed)
			{
				_greetingsScreenManager.StartGreetingsFinished += GreetingsScreenManagerOnStartGreetingsFinished;
			}
			else
			{
				SetNewGreetingsTime();
				_timerCoroutine = StartCoroutine(TimerCoroutine());
			}
		}

		private void GreetingsScreenManagerOnStartGreetingsFinished()
		{
			_greetingsScreenManager.StartGreetingsFinished -= GreetingsScreenManagerOnStartGreetingsFinished;
			SetNewGreetingsTime();
			_timerCoroutine = StartCoroutine(TimerCoroutine());
		}

		private void OnEnable()
		{
			// Stops OnEnable from running when the GO is first enabled, that required logic is handled in the Awake method
			if (!_awoken)
			{
				_awoken = true;
				return;
			}

			if (_gameScenesManager.isInTransition)
			{
				StartCoroutine(AwaitResultsShenanigans(() =>
				{
					AddTimeFromPreviouslyActive();
					_timerCoroutine = StartCoroutine(TimerCoroutine());
				}));
			}
			else
			{
				_siraLog.Info("lmao");
				AddTimeFromPreviouslyActive();
				_timerCoroutine = StartCoroutine(TimerCoroutine());
			}
		}
		
		private void OnDisable()
		{
			if (_pluginConfig.RandomiserEnabled)
			{
				_previouslyActiveTime = DateTime.Now;
			}
			else
			{
				_previouslyActiveTime = null;
			}

			if (_timerCoroutine != null)
			{
				StopCoroutine(_timerCoroutine);	
			}
		}

		public void Dispose()
		{
			_greetingsFlowCoordinator.GreetingsFlowCoordinatorActiveChangedEvent -= GreetingsFlowCoordinatorOnGreetingsFlowCoordinatorActiveChangedEvent;
			_greetingsScreenManager.StartGreetingsFinished -= GreetingsScreenManagerOnStartGreetingsFinished;
		}
	}
}
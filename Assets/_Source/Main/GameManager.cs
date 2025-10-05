using System;
using System.Linq;
using GameActors;
using GameControllers;
using UnityEngine;
using Zenject;

namespace Main
{
    public class GameManager : MonoBehaviour
    {
        //Input
        [field: SerializeField] private Transform PlayerSpawnPoint { get; set; }
        [field: SerializeField] private Player Player { get; set; }

        [field: SerializeField] public LeveledPoints[] LeveledPoints { get; private set; }
        [field: SerializeField] private SystemItem[] SystemItems { get; set; }
        [field: SerializeField] private ItemToCollect[] CollectableItems { get; set; }
        private SystemItem _deathWall;

        private UiManager _uiManager;

        private void Start()
        {
            Subscribe();
            _deathWall = SystemItems.FirstOrDefault(si => si.SystemActionType == SystemActionType.LeftDeathWall);
            Player.LeveledPoints = LeveledPoints;
        }

        private void Update()
        {
            _deathWall.PlayerPosition = Player.gameObject.transform;
            _deathWall.PlayerSpeed = Player.OriginalSpeed;
        }

        private void OnDestroy()
        {
            UnSubscribe();
        }

        [Inject]
        public void Init(UiManager uiManager)
        {
            _uiManager = uiManager;
        }

        private void ResetGame()
        {
            Player.ResetStats(PlayerSpawnPoint);
            _uiManager.ResetUi();
            foreach (var item in CollectableItems) item.Respawn();

            foreach (var item in SystemItems) item.ResetGame();
        }

        private void Subscribe()
        {
            _uiManager.OnReplayPressed += ResetGame;
            Player.OnScoreChanged += ScoreChanged;
            Player.OnFingerPrintLeft += SetTimer;
            foreach (var item in SystemItems) item.OnSystemActionTriggered += InvokeSystemAction;
        }

        private void SetTimer(EffectFingerPrint effectFingerPrint)
        {
            _uiManager.ShowTimerWithTime(effectFingerPrint);
        }

        private void ScoreChanged(int newScore)
        {
            _uiManager.SeScoreDisplay(newScore);
        }

        private void UnSubscribe()
        {
            _uiManager.OnReplayPressed -= ResetGame;
            Player.OnScoreChanged -= ScoreChanged;
            foreach (var item in SystemItems) item.OnSystemActionTriggered -= InvokeSystemAction;
        }

        private void InvokeSystemAction(SystemActionType action)
        {
            switch (action)
            {
                case SystemActionType.LeftDeathWall:
                    ProcessResult();
                    _uiManager.AddHint(new UiHint
                    {
                        Text = "You've came late to the Party"
                    });
                    break;
                case SystemActionType.FinishLine:
                    ProcessResult();
                    _uiManager.AddHint(new UiHint
                    {
                        Text = "You've reached the Party!"
                    });
                    break;
                case SystemActionType.BadInvestments:
                    ProcessResult();
                    _uiManager.AddHint(new UiHint
                    {
                        Text = "You've invested to bitcoin"
                    });
                    break;
            }
        }

        private void ProcessResult()
        {
            var currentScore = Player.CoolScore;

            var result = LeveledPoints
                .Where(level => level.Points <= currentScore)
                .OrderByDescending(level => level.Points)
                .FirstOrDefault();

            if (result.Equals(default(LeveledPoints))) result = LeveledPoints[0];

            Player.StopPlayer();

            _deathWall.StopDeathWall();

            _uiManager.ShowResult(result);
        }

        // private void FinishGame()
        // {
        //     ProcessResult();
        // }
    }

    [Serializable]
    public struct LeveledPoints
    {
        [field: SerializeField] public ResultType ResultType { get; private set; }
        [field: SerializeField] public int Points { get; private set; }
    }

    public enum ResultType
    {
        DogWater,
        Lame,
        Npc,
        Wojak,
        GigaChad
    }
}
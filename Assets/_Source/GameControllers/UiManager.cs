using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using GameActors;
using Main;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameControllers
{
    public class UiManager : MonoBehaviour
    {
        //Main
        [field: SerializeField] public TextMeshProUGUI ScoreDisplay { get; private set; }
        [field: SerializeField] public GameObject CoolPointsContainer { get; private set; }

        //Timer
        [field: SerializeField] public TextMeshProUGUI TimerDisplay { get; private set; }
        [field: SerializeField] public TextMeshProUGUI EffectNameDisplay { get; private set; }
        [field: SerializeField] public Image Icon { get; private set; }
        [field: SerializeField] public GameObject TimerContainer { get; private set; }

        [field: SerializeField] public LeveledUiResult[] LeveledUiResult { get; private set; }
        private EffectFingerPrint _currentEffect;

        private LeveledUiResult? _showedUiResult;

        private CancellationTokenSource _timerCancellationTokenSource;
        public Action OnReplayPressed;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        private void Start()
        {
            CoolPointsContainer.gameObject.SetActive(true);
            foreach (var uiResult in LeveledUiResult) uiResult.ResultUiContainer.SetActive(false);

            TimerContainer.SetActive(false);
            _timerCancellationTokenSource = new CancellationTokenSource();
        }

        private void OnDestroy()
        {
            // Отменяем все задачи при уничтожении объекта
            _timerCancellationTokenSource?.Cancel();
            _timerCancellationTokenSource?.Dispose();
        }

        public void ShowResult(LeveledPoints level)
        {
            var uiResult = LeveledUiResult.FirstOrDefault(r => r.ResultType == level.ResultType);
            _showedUiResult = uiResult;
            uiResult.PointsText.text = $"{level.Points}";
            uiResult.ResultUiContainer.SetActive(true);
            CoolPointsContainer.gameObject.SetActive(false);
        }

        public void ReplayCalled()
        {
            OnReplayPressed.Invoke();
        }

        public void SeScoreDisplay(int newScore)
        {
            ScoreDisplay.text = $"{newScore}";
        }

        public void ResetUi()
        {
            foreach (var uiContainers in LeveledUiResult)
            {
                uiContainers.TextHint.text = "";
                uiContainers.PointsText.text = "";
                uiContainers.ResultUiContainer.SetActive(false);
            }

            ScoreDisplay.text = "0";
            CoolPointsContainer.gameObject.SetActive(true);
            _showedUiResult = null;

            ResetTimer();
        }

        public void AddHint(UiHint hint)
        {
            if (_showedUiResult != null) _showedUiResult.Value.TextHint.text = hint.Text;
        }

        public void ShowTimerWithTime(EffectFingerPrint effectFingerPrint)
        {
            // Отменяем предыдущий таймер если есть
            _timerCancellationTokenSource?.Cancel();
            _timerCancellationTokenSource?.Dispose();
            _timerCancellationTokenSource = new CancellationTokenSource();

            _currentEffect = effectFingerPrint;

            // Устанавливаем название эффекта и иконку
            EffectNameDisplay.text = effectFingerPrint.EffectName;
            Icon.sprite = effectFingerPrint.Icon;

            // Показываем контейнер
            TimerContainer.SetActive(true);

            // Проверяем тип эффекта (с таймером или без)
            if (effectFingerPrint.Timer > 0)
            {
                // Эффект с таймером - показываем обратный отсчет
                UpdateTimerAsync(effectFingerPrint.Timer, _timerCancellationTokenSource.Token).Forget();
            }
            else
            {
                // Эффект без таймера - показываем подсказку вместо таймера
                TimerDisplay.text = effectFingerPrint.EffectHint;

                // Автоматически скрываем через 2 секунды
                AutoHideNotificationAsync(2f, _timerCancellationTokenSource.Token).Forget();
            }
        }

        private async UniTaskVoid UpdateTimerAsync(float duration, CancellationToken cancellationToken)
        {
            var timeLeft = duration;

            try
            {
                while (timeLeft > 0 && !cancellationToken.IsCancellationRequested)
                {
                    // Обновляем отображение таймера
                    TimerDisplay.text = FormatTime(timeLeft);
                    timeLeft -= Time.deltaTime;

                    // Ждем следующий кадр с проверкой отмены
                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                }

                // Если таймер не был отменен, скрываем его
                if (!cancellationToken.IsCancellationRequested) ResetTimer();
            }
            catch (OperationCanceledException)
            {
                // Ожидаемое исключение при отмене - просто выходим
            }
        }

        private async UniTaskVoid AutoHideNotificationAsync(float delay, CancellationToken cancellationToken)
        {
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: cancellationToken);

                if (!cancellationToken.IsCancellationRequested) ResetTimer();
            }
            catch (OperationCanceledException)
            {
                // Ожидаемое исключение при отмене - просто выходим
            }
        }

        private string FormatTime(float timeInSeconds)
        {
            var seconds = Mathf.FloorToInt(timeInSeconds);
            var milliseconds = Mathf.FloorToInt((timeInSeconds - seconds) * 100);
            return $"{seconds:00}:{milliseconds:00}";
        }

        public void ResetTimer()
        {
            // Отменяем текущую задачу таймера
            _timerCancellationTokenSource?.Cancel();
            _timerCancellationTokenSource?.Dispose();
            _timerCancellationTokenSource = new CancellationTokenSource();

            TimerContainer.SetActive(false);
            TimerDisplay.text = "00:00";
            EffectNameDisplay.text = "";
            Icon.sprite = null;
            _currentEffect = null;
        }

        public void UpdateTimerDisplay(float timeLeft)
        {
            TimerDisplay.text = FormatTime(timeLeft);
        }
    }

    [Serializable]
    public struct LeveledUiResult
    {
        [field: SerializeField] public ResultType ResultType { get; private set; }
        [field: SerializeField] public GameObject ResultUiContainer { get; private set; }
        [field: SerializeField] public TextMeshProUGUI PointsText { get; private set; }
        [field: SerializeField] public TextMeshProUGUI TextHint { get; private set; }
    }

    [Serializable]
    public struct UiHint
    {
        public string Text { get; set; }
    }
}
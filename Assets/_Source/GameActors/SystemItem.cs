using System;
using UnityEngine;

namespace GameActors
{
    public class SystemItem : MonoBehaviour
    {
        [field: SerializeField] public SystemActionType SystemActionType { get; private set; }
        [SerializeField] private float baseSpeed = 5f; // Базовая скорость

        private Vector2 _originalPosition;
        private bool _stopped;

        public Action<SystemActionType> OnSystemActionTriggered;

        public Transform PlayerPosition { get; set; }
        public float PlayerSpeed { get; set; }

        private void Start()
        {
            _originalPosition = transform.position;
        }

        private void FixedUpdate()
        {
            if (SystemActionType == SystemActionType.LeftDeathWall && !_stopped)
            {
                // Вычисляем разницу по X между объектом и игроком
                var xDifference = gameObject.transform.position.x - PlayerPosition.transform.position.x;

                if (xDifference >= -20)
                    // Двигаем с базовой скоростью
                    transform.Translate(Vector3.right * (baseSpeed * Time.fixedDeltaTime));
                else
                    // Двигаем с утроенной скоростью
                    transform.Translate(Vector3.right * (baseSpeed * 3 * Time.fixedDeltaTime));
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.gameObject.CompareTag("Player")) OnSystemActionTriggered.Invoke(SystemActionType);
        }

        public void StopDeathWall()
        {
            if (SystemActionType == SystemActionType.LeftDeathWall) _stopped = true;
        }

        public void ResetGame()
        {
            if (SystemActionType == SystemActionType.LeftDeathWall)
            {
                gameObject.transform.position = _originalPosition;
                _stopped = false;
            }
        }
    }

    public enum SystemActionType
    {
        LeftDeathWall,
        FinishLine,
        BadInvestments
    }
}
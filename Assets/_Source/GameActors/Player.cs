using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Main;
using UnityEngine;

namespace GameActors
{
    [RequireComponent(typeof(CapsuleCollider2D))]
    [RequireComponent(typeof(Rigidbody2D))]
    public class Player : MonoBehaviour
    {
        //Input
        [Header("Move Settings")]
        [field: SerializeField]
        private float Speed { get; set; } = 20f;

        [field: SerializeField] private float JumpPower { get; set; } = 10f;
        [field: SerializeField] private float maxHorizontalSpeed = 8f; // Максимальная горизонтальная скорость
        [SerializeField] private float airControlFactor = 0.7f; // Контроль в воздухе (0-1)
        [SerializeField] private float deceleration = 15f; // Сила замедления

        [Header("Game Logic")]
        [field: SerializeField]
        public CharacterSprite[] characterSprites { get; private set; }

        // Ground Check
        [SerializeField] private float groundCheckRadius = 0.2f; // Радиус проверки земли
        [SerializeField] private Transform groundCheckPoint; // Точка для проверки земли
        [SerializeField] private LayerMask groundLayerMask; // Слой земли
        private float _currentSpeed;
        private bool _isGrounded;
        private bool _isPlayerActive = true; // Флаг активности игрока

        private float _originalLinearVelocityY;
        private Rigidbody2D _rigidBody2D;
        private SpriteRenderer _spriteRenderer;
        private Sequence _tweenSequence;
        public Action<EffectFingerPrint> OnFingerPrintLeft;
        public Action<int> OnScoreChanged;

        //System Vars
        public float OriginalSpeed { get; private set; }

        //Class Logic
        public int CoolScore { get; private set; }
        public LeveledPoints[] LeveledPoints { get; set; }

        private void Start()
        {
            OriginalSpeed = Speed;
            _currentSpeed = Speed;

            _rigidBody2D = GetComponent<Rigidbody2D>();
            _spriteRenderer = GetComponent<SpriteRenderer>();

            _originalLinearVelocityY = _rigidBody2D.linearVelocityY;
            _rigidBody2D.linearVelocityY *= 0.5f;

            if (groundCheckPoint == null)
                groundCheckPoint = transform;

            SetCharacterSprite();
        }

        private void FixedUpdate()
        {
            if (!_isPlayerActive) return;

            CheckGrounded();

            HandleMovement();

            HandleJump();

            ApplyDeceleration();

            LimitHorizontalVelocity();
        }

        private void OnCollisionEnter2D(Collision2D other)
        {
            if (other.collider.CompareTag("Item") && other.gameObject.TryGetComponent(out ItemToCollect item))
                switch (item.ItemType)
                {
                    case ItemType.CoolItem:
                        break;
                    case ItemType.LameItem:
                        break;
                    // default:
                    //     throw new ArgumentOutOfRangeException();
                }
        }

        private void OnDrawGizmosSelected()
        {
            if (groundCheckPoint != null)
            {
                Gizmos.color = _isGrounded ? Color.green : Color.red;
                Gizmos.DrawWireSphere(groundCheckPoint.position, groundCheckRadius);
            }
        }

        private void HandleMovement()
        {
            var horizontalInput = 0f;

            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
                horizontalInput -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
                horizontalInput += 1f;

            if (Mathf.Abs(horizontalInput) > 0.1f)
            {
                var movementForce = _currentSpeed;
                if (!_isGrounded)
                    movementForce *= airControlFactor;

                _rigidBody2D.AddForce(Vector2.right * (horizontalInput * movementForce), ForceMode2D.Force);
            }
        }

        private void HandleJump()
        {
            if (_isGrounded && Input.GetKey(KeyCode.Space))
            {
                _rigidBody2D.AddForce(Vector2.up * JumpPower, ForceMode2D.Impulse);
                _isGrounded = false; // Сразу устанавливаем false, чтобы предотвратить множественные прыжки
            }
        }

        private void ApplyDeceleration()
        {
            var noHorizontalInput = !Input.GetKey(KeyCode.A) && !Input.GetKey(KeyCode.D) &&
                                    !Input.GetKey(KeyCode.LeftArrow) && !Input.GetKey(KeyCode.RightArrow);

            if (noHorizontalInput && _isGrounded)
            {
                var velocity = _rigidBody2D.linearVelocity;
                velocity.x = Mathf.Lerp(velocity.x, 0, deceleration * Time.fixedDeltaTime);
                _rigidBody2D.linearVelocity = velocity;
            }
        }

        private void LimitHorizontalVelocity()
        {
            var velocity = _rigidBody2D.linearVelocity;

            var currentMaxSpeed = _isGrounded ? maxHorizontalSpeed : maxHorizontalSpeed * 1.2f;

            if (Mathf.Abs(velocity.x) > currentMaxSpeed)
            {
                velocity.x = Mathf.Sign(velocity.x) * currentMaxSpeed;
                _rigidBody2D.linearVelocity = velocity;
            }
        }

        private void CheckGrounded()
        {
            var colliders = Physics2D.OverlapCapsuleAll(
                groundCheckPoint.position,
                new Vector2(1f, 0.2f),
                CapsuleDirection2D.Horizontal,
                90
            );

            _isGrounded = false;

            foreach (var collider in colliders)
                if (!collider.isTrigger && collider.gameObject != gameObject)
                {
                    _isGrounded = true;
                    break;
                }
        }

        public void SetCoolScore(int points)
        {
            CoolScore += points;
            OnScoreChanged.Invoke(CoolScore);
            SetCharacterSprite();
        }

        private void SetCharacterSprite()
        {
            var result = LeveledPoints
                .Where(level => level.Points <= CoolScore)
                .OrderByDescending(level => level.Points)
                .FirstOrDefault();

            if (result.Equals(default(LeveledPoints))) result = LeveledPoints[0];

            var newCharacter = characterSprites.FirstOrDefault(cs => cs.ResultType == result.ResultType);

            _spriteRenderer.sprite = newCharacter.Sprite;
        }

        public async void EffectSpeed(float speedEffectPower, float duration, EffectFingerPrint effectFingerPrint)
        {
            _currentSpeed *= speedEffectPower;
            OnFingerPrintLeft.Invoke(effectFingerPrint);
            await UniTask.Delay(Mathf.FloorToInt(duration * 1000));
            _currentSpeed = OriginalSpeed;
        }

        public void ResetStats(Transform newPosition)
        {
            gameObject.transform.position = newPosition.position;

            CoolScore = 0;

            _currentSpeed = OriginalSpeed;

            _tweenSequence.Kill();

            _rigidBody2D.linearVelocity = Vector2.zero;
            _rigidBody2D.angularVelocity = 0f;

            _rigidBody2D.isKinematic = false;

            GetComponent<CapsuleCollider2D>().enabled = true;

            _isPlayerActive = true;

            SetCharacterSprite();

            _isGrounded = false;

            Debug.Log("Player stats completely reset");
        }

        public void PutFingerPrint(EffectFingerPrint effectFingerPrint)
        {
            OnFingerPrintLeft.Invoke(effectFingerPrint);
        }

        public void StopPlayer()
        {
            _isPlayerActive = false;

            _rigidBody2D.linearVelocity = Vector2.zero;
            _rigidBody2D.angularVelocity = 0f;

            _rigidBody2D.isKinematic = true;

            GetComponent<CapsuleCollider2D>().enabled = false;

            Debug.Log("Player stopped - game ended");
        }
    }

    [Serializable]
    public struct CharacterSprite
    {
        [field: SerializeField] public ResultType ResultType;
        [field: SerializeField] public Sprite Sprite;
    }
}
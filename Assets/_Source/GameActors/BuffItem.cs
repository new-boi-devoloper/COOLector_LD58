using UnityEngine;

namespace GameActors
{
    public class BuffItem : MonoBehaviour
    {
        [field: SerializeField] public BuffItemType buffItemType { get; private set; }

        [field: Header("Tramp Logic")] [field: SerializeField] [Tooltip("For Tramp Effect")]
        public float trampPushPower;


        [field: Header("Speed Effect Logic")] [field: SerializeField] [Tooltip("For Speed Effect")]
        public float slowDownPower;

        [field: SerializeField] [Tooltip("For Speed Effect")]
        public float duration;

        private Transform _originalPosition;

        private SpriteRenderer _spriteRenderer;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        private void Start()
        {
            _originalPosition = transform;
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }


        private void OnTriggerEnter2D(Collider2D other)
        {
            if (buffItemType == BuffItemType.Tramp)
                if (other.gameObject.CompareTag("Player") &&
                    other.gameObject.TryGetComponent(out Rigidbody2D playerRb) &&
                    other.gameObject.TryGetComponent(out Player player))
                {
                    // Сбрасываем вертикальную скорость перед применением силы
                    var velocity = playerRb.linearVelocity;
                    velocity.y = 0f;
                    playerRb.linearVelocity = velocity;

                    // Применяем постоянную силу
                    playerRb.AddForce(Vector2.up * trampPushPower, ForceMode2D.Impulse);

                    player.PutFingerPrint(new EffectFingerPrint
                    {
                        EffectName = "Oh, pull-up bar",
                        EffectHint = "Wild JUMP!",
                        Icon = _spriteRenderer.sprite,
                        Timer = 0
                    });
                }

            if (buffItemType == BuffItemType.SpeedUpEffect)
                if (other.gameObject.CompareTag("Player") && other.gameObject.TryGetComponent(out Player player))
                    player.EffectSpeed(slowDownPower, duration, new EffectFingerPrint
                    {
                        EffectName = "Slow Down Boy!",
                        Icon = _spriteRenderer.sprite,
                        Timer = duration
                    });
            if (buffItemType == BuffItemType.SpeedUpEffect)
                if (other.gameObject.CompareTag("Player") && other.gameObject.TryGetComponent(out Player player))
                    player.EffectSpeed(slowDownPower, duration, new EffectFingerPrint
                    {
                        EffectName = "MONSTER!",
                        Icon = _spriteRenderer.sprite,
                        Timer = duration
                    });
        }
    }

    public class EffectFingerPrint
    {
        public string EffectHint = "";
        public string EffectName = "";
        public Sprite Icon;
        public float Timer;
    }

    public enum BuffItemType
    {
        Tramp,
        SpeedUpEffect
    }
}
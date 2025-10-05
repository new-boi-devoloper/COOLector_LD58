using UnityEngine;

namespace GameActors
{
    public class ItemToCollect : MonoBehaviour
    {
        [field: SerializeField] public ItemType ItemType { get; private set; }

        [field: SerializeField] private int pointsToAdd;
        private Transform _originalPosition;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        private void Start()
        {
            _originalPosition = transform;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.gameObject.CompareTag("Player") &&
                other.gameObject.TryGetComponent(out Player player))
            {
                player.SetCoolScore(pointsToAdd);

                gameObject.SetActive(false);
            }
        }

        public void Respawn()
        {
            gameObject.transform.position = _originalPosition.position;
            gameObject.SetActive(true);
        }
    }

    public enum ItemType
    {
        CoolItem,
        LameItem
    }
}
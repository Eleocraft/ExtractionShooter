using UnityEngine;

namespace ExoplanetStudios.ExtractionShooter
{
    public class MagicProjectile : MonoBehaviour
    {
        private Vector3 _direction;
        [SerializeField] private float Speed;
        [SerializeField] private float SizeIncreaseSpeed;
        [SerializeField] private float Lifetime;
        [SerializeField] private float Damage;
        private ulong _ownerID;
        private int _tickDiff;
        public void OnInitialisation(Vector3 direction, ulong ownerID, int tickDiff)
        {
            _direction = direction;
            _ownerID = ownerID;
            _tickDiff = tickDiff;
        }
        private void FixedUpdate()
        {
            transform.position += _direction * Speed * Time.fixedDeltaTime;
            transform.localScale += Vector3.one * SizeIncreaseSpeed * Time.fixedDeltaTime;
            Lifetime -= Time.fixedDeltaTime;
            if (Lifetime < 0)
                Destroy(gameObject);
        }
        private void OnTriggerEnter(Collider col)
        {
            if (col.gameObject.TryGetComponent(out IDamagable damagable))
                damagable.Damage(Damage, _ownerID, _tickDiff);
        }
    }
}

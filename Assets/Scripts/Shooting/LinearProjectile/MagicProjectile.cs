using UnityEngine;

namespace ExoplanetStudios.ExtractionShooter
{
    public class MagicProjectile : MonoBehaviour
    {
        [SerializeField] private float Speed;
        [SerializeField] private float SizeIncreaseSpeed;
        [SerializeField] private float Lifetime;
        [SerializeField] private float Fadetime;
        [SerializeField] private float Damage;
        private ulong _ownerID;
        private int _tickDiff;
        public void OnInitialisation(Vector3 direction, ulong ownerID, int tickDiff)
        {
            _ownerID = ownerID;
            _tickDiff = tickDiff;
            GetComponent<Rigidbody>().velocity = direction * Speed;
            GetComponent<FadeController>().StartTimer(Lifetime, Fadetime, () => Destroy(gameObject));
        }
        private void Update()
        {
            transform.localScale += Vector3.one * SizeIncreaseSpeed * Time.deltaTime;
        }
        private void OnTriggerEnter(Collider col)
        {
            if (col.attachedRigidbody?.TryGetComponent(out IDamagable damagable) == true)
                damagable.Damage(Damage, _ownerID, _tickDiff);
        }
    }
}

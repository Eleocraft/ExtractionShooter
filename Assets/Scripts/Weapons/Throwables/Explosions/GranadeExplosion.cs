using UnityEngine;
using UnityEngine.VFX;

namespace ExoplanetStudios.ExtractionShooter
{
    public class GranadeExplosion : ThrowableExplosion
    {
        [SerializeField] private VisualEffect sparkParticles;
        [SerializeField] private float Radius;
        [SerializeField] [MinMaxRange(0, 200)] private RangeSlider Damage;
        
        private void Awake() => sparkParticles.Stop();

        public override void ExecuteExplosion(ulong ownerId, int tickDiff)
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, Radius);
            foreach (Collider col in colliders)
            {
                if (col.gameObject.TryGetComponent(out IDamagable damagable))
                {
                    float relativeDist = 1f - ((col.transform.position - transform.position).magnitude / Radius);
                    if (relativeDist > 0)
                        damagable.ExplosionDamage(Damage.Evaluate(relativeDist), ownerId, tickDiff);
                }
            }
        }
        
        private void StartParticles() => sparkParticles.Play();
        private void Destroy() => Destroy(gameObject);
    }
}
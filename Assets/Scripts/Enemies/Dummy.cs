using UnityEngine;

namespace ExoplanetStudios.ExtractionShooter
{
    public class Dummy : MonoBehaviour, IDamagable
    {
        [SerializeField] private float MaxLife;
        [SerializeField] private GameObject HitParticles;
        [SerializeField] private GameObject BreakParticles;
        private float _life;
        void Start()
        {
            _life = MaxLife;
        }
        public void OnHit(float damage, Vector3 position)
        {
            _life -= damage;
            Instantiate(HitParticles, position, Quaternion.identity);
            if (_life < 0)
            {
                _life = MaxLife;
                Instantiate(BreakParticles, transform.position, Quaternion.identity);
            }
        }
    }
}
using UnityEngine;
using Unity.Netcode;

namespace ExoplanetStudios.ExtractionShooter
{
    public class PlayerLife : NetworkBehaviour, IDamagable
    {
        [SerializeField] private float MaxLife;
        [SerializeField] private GameObject BreakParticles;
        [SerializeField] private Vector3 SpawnPoint;
        private NetworkVariable<float> _life = new NetworkVariable<float>();

        private FirstPersonController _firstPersonController; // Serveronly
        private void Start()
        {
            if (IsServer)
            {
                _life.Value = MaxLife;
                _firstPersonController = GetComponent<FirstPersonController>();
            }
            _life.OnValueChanged += OnLifeChanged;
        }
        public override void OnDestroy()
        {
            base.OnDestroy();
            _life.OnValueChanged -= OnLifeChanged;
        }
        public void OnHit(float damage)
        {
            if (IsServer)
            {
                _life.Value -= damage;
                if (_life.Value <= 0)
                {
                    _life.Value = MaxLife;
                    PlayerDeadClientRpc(transform.position);
                    _firstPersonController.SetPosition(SpawnPoint);
                }
            }
        }
        [ClientRpc]
        private void PlayerDeadClientRpc(Vector3 position)
        {
            Instantiate(BreakParticles, position, Quaternion.identity);
            if (IsOwner)
                Debug.Log("You Died");
        }
        private void OnLifeChanged(float oldValue, float newValue)
        {
            if (!IsOwner) return;

            if (oldValue > newValue)
                PlayerLifeBar.AnimateProgress(newValue / MaxLife);
            else
                PlayerLifeBar.SetProgress(newValue / MaxLife);
        }
    }
}
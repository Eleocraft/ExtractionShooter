using UnityEngine;
using Unity.Netcode;

namespace ExoplanetStudios.ExtractionShooter
{
    public class PlayerLife : NetworkBehaviour
    {
        [SerializeField] private float MaxLife;
        [SerializeField] private float SpawnProtectionTime = 2;
        [SerializeField] private GameObject BreakParticles;
        [SerializeField] private GameObject HitParticle;
        [SerializeField] private GameObject HeadshotParticle;
        private NetworkVariable<float> _life = new NetworkVariable<float>();
        private FirstPersonController _firstPersonController;
        private float _spawnProtectionTimer; // Serveronly

        private void Start()
        {
            if (IsServer)
                _life.Value = MaxLife;

            _firstPersonController = GetComponent<FirstPersonController>();
            _life.OnValueChanged += OnLifeChanged;
            NetworkManager.NetworkTickSystem.Tick += Tick;
        }
        public override void OnDestroy()
        {
            base.OnDestroy();
            _life.OnValueChanged -= OnLifeChanged;

            if (NetworkManager?.NetworkTickSystem != null)
				NetworkManager.NetworkTickSystem.Tick -= Tick;
        }
        private void Tick()
        {
            if (_spawnProtectionTimer > 0)
                _spawnProtectionTimer -= NetworkManager.LocalTime.FixedDeltaTime;
        }
        public bool OnHit(ProjectileInfo info, Vector3 point, DamageType damageType, float projectileVelocity, ulong ownerId)
        {
            if (OwnerClientId == ownerId)
                return false;

            // Particle Effects
            if (damageType == DamageType.Headshot)
                Instantiate(HeadshotParticle, point + transform.position, Quaternion.identity);
            else
                Instantiate(HitParticle, point + transform.position, Quaternion.identity);

            Damage(info.GetDamage(damageType, projectileVelocity), ownerId);
            
            return true;
        }
        public void Damage(float damage, ulong ownerId)
        {
            if (IsServer && _spawnProtectionTimer <= 0)
            { // Life calculation
                _life.Value -= damage;
                if (_life.Value <= 0)
                {
                    _spawnProtectionTimer = SpawnProtectionTime;
                    Scoreboard.AddKill(ownerId, OwnerClientId);
                    NetworkManager.ConnectedClients[ownerId].PlayerObject.GetComponent<PlayerLife>()._life.Value = MaxLife; // Heal shooting player
                    _life.Value = MaxLife; // heal this player
                    PlayerDeadClientRpc(transform.position); // Send message to all clients
                    _firstPersonController.SetPosition(SpawnPoints.GetSpawnPoint()); // Set new position
                }
            }
        }
        [ClientRpc]
        private void PlayerDeadClientRpc(Vector3 position)
        {
            if (!IsOwner)
                Instantiate(BreakParticles, position + Vector3.up, Quaternion.identity);
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
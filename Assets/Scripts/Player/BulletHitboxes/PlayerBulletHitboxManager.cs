using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

namespace ExoplanetStudios.ExtractionShooter
{
    public class PlayerBulletHitboxManager : NetworkBehaviour
    {
        [SerializeField] private PlayerBulletHitbox PlayerBulletHitboxPrefab;

        private PlayerLife _playerLife;
        private FirstPersonController _controller;
        private Dictionary<int, PlayerBulletHitboxContainer> hitboxes;
        private static List<PlayerBulletHitboxManager> managers = new();
        private void Start()
        {
            managers.Add(this);

            _playerLife = GetComponent<PlayerLife>();
            _controller = GetComponent<FirstPersonController>();

            hitboxes = new();
        }
        public override void OnDestroy()
        {
            base.OnDestroy();

            managers.Remove(this);
        }
        private void ActivateHitbox(int tickDiff)
        {
            if (!hitboxes.ContainsKey(tickDiff))
            {
                PlayerBulletHitbox newHitbox = Instantiate(PlayerBulletHitboxPrefab);
                newHitbox.Initialize(_playerLife, _controller, tickDiff, false);
                hitboxes.Add(tickDiff, new(newHitbox));
            }

            hitboxes[tickDiff].ActiveAmount++;
        }
        private void RemoveHitbox(int tickDiff)
        {
            hitboxes[tickDiff].ActiveAmount--;
        }
        public static void AddBullet(int tickDiff)
        {
            foreach (PlayerBulletHitboxManager manager in managers)
                manager.ActivateHitbox(tickDiff);
        }
        public static void RemoveBullet(int tickDiff)
        {
            foreach (PlayerBulletHitboxManager manager in managers)
                manager.RemoveHitbox(tickDiff);
        }
        private class PlayerBulletHitboxContainer
        {
            private readonly PlayerBulletHitbox _hitbox;
            private int _activeAmount;
            public int ActiveAmount
            {
                get => _activeAmount;
                set
                {
                    _activeAmount = value;
                    if (_activeAmount <= 0)
                        _hitbox.SetActive(false);
                    else
                        _hitbox.SetActive(true);
                }
            }
            public PlayerBulletHitboxContainer(PlayerBulletHitbox hitbox)
            {
                _hitbox = hitbox;
            }
        }
    }
}

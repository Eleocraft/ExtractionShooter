using Unity.Netcode;
using UnityEngine;

namespace ExoplanetStudios.ExtractionShooter
{
    public class PlayerBulletHitboxSpawner : NetworkBehaviour
    {
        [SerializeField] private PlayerBulletHitbox PlayerBulletHitboxPrefab;
        [SerializeField] private int BulletHitboxAmount;
        void Start()
        {
            PlayerLife playerLife = GetComponent<PlayerLife>();
            FirstPersonController controller = GetComponent<FirstPersonController>();
            Instantiate(PlayerBulletHitboxPrefab, transform.position, Quaternion.identity, transform).Initialize(playerLife, controller, 0);

            if (IsServer)
                for (int i = 1; i <= BulletHitboxAmount; i++)
                    Instantiate(PlayerBulletHitboxPrefab).Initialize(playerLife, controller, i);
        }
    }
}

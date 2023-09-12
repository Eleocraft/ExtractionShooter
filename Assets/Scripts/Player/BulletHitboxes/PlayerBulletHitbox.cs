using UnityEngine;
using Unity.Netcode;

namespace ExoplanetStudios.ExtractionShooter
{
    public class PlayerBulletHitbox : MonoBehaviour
    {
        [SerializeField] private float CrouchBodyYScale;
        [SerializeField] private float CrouchCamYPos;
        [SerializeField] private Transform Body;
        [SerializeField] private Transform Head;

        private float _defaultHeadYPos;
        private PlayerLife _playerLife;
        private FirstPersonController _controller;
        private int _tickDiff;
        private bool _active;
        public void Initialize(PlayerLife playerLife, FirstPersonController controller, int tickDiff, bool active)
        {
            foreach (PlayerHitRegion hitRegion in GetComponentsInChildren<PlayerHitRegion>())
                hitRegion.Initialize(this);

            _playerLife = playerLife;
            _controller = controller;
            _tickDiff = tickDiff;
            SetActive(active);

            _defaultHeadYPos = Head.localPosition.y;

            _controller.TransformStateChanged += TransformStateChanged;
        }
        private void OnDestroy()
        {
            _controller.TransformStateChanged -= TransformStateChanged;
        }
        public void OnHit(ProjectileInfo info, Vector3 point, DamageType damageType, float projectileVelocity, ulong ownerId, int tickDiff)
        {
            if (tickDiff == _tickDiff)
                _playerLife.OnHit(info, point - transform.position, damageType, projectileVelocity, ownerId);
        }
        private void TransformStateChanged(NetworkTransformState transformState)
        {
            if (!_active)
                return;
            
            if (_controller.GetState(NetworkManager.Singleton.LocalTime.Tick - _tickDiff, out NetworkTransformState state))
            {
                transform.position = state.Position;
                transform.rotation = Quaternion.Euler(0, state.LookRotation.y, 0);

                // crouch
                Body.localScale = Body.localScale.WithHeight(Mathf.Lerp(1, CrouchBodyYScale, state.CrouchAmount));
                Head.transform.localPosition = Head.transform.localPosition.WithHeight(Mathf.Lerp(_defaultHeadYPos, CrouchCamYPos, state.CrouchAmount));
            }
        }
        public void SetActive(bool active)
        {
            gameObject.SetActive(active);
            _active = active;
        }
    }
}
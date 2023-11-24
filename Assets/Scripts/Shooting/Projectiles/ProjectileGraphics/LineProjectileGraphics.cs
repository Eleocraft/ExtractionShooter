using UnityEngine;

namespace ExoplanetStudios.ExtractionShooter
{
    public class LineProjectileGraphic : ProjectileGraphic
    {
        [SerializeField] private float DecayTime = 2f;
        [SerializeField] private float FadeTime = 2f;
        [SerializeField] private float SqrCornerDistance = 2f;
        private LineRenderer _lineRenderer;
        private Vector3 _lastSavePosition;
        private Vector3 _offset;
        public override void OnInitialisation(Vector3 position, Vector3 direction, Vector3 offset)
        {
            _lineRenderer = GetComponent<LineRenderer>();
            _lineRenderer.positionCount = 1;
            AddPosition(position);
            _offset = offset;
        }
        public override void SetPositionAndDirection(Vector3 newPosition, Vector3 newDirection)
        {
            if ((_lastSavePosition - newPosition).sqrMagnitude > SqrCornerDistance)
                AddPosition(GetPosition(newPosition));
            else
                _lineRenderer.SetPosition(_lineRenderer.positionCount-1, GetPosition(newPosition));
        }
        public override void AddHit(Vector3 hitPosition, Vector3 direction) => AddPosition(hitPosition);
        private void AddPosition(Vector3 position)
        {
            _lineRenderer.positionCount++;
            _lineRenderer.SetPosition(_lineRenderer.positionCount-2, position);
            _lineRenderer.SetPosition(_lineRenderer.positionCount-1, position);
            _lastSavePosition = position;
                
        }
        private Vector3 GetPosition(Vector3 inputPos) => inputPos + _offset;
        public override void EndProjectile()
        {
            _lineRenderer.positionCount--;
            GetComponent<FadeController>().StartTimer(DecayTime, FadeTime, () => Destroy(gameObject));
        }
    }
}

using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ExoplanetStudios
{
    public class DecalGradientFadeController : FadeController
    {
        private DecalProjector _projector;
        
        [SerializeField] private Gradient BaseColorGradient;
        [SerializeField] private Gradient EmissionGradient;
        private void Awake()
        {
            _projector = GetComponentInChildren<DecalProjector>();
            _projector.material = new Material(_projector.material);
        }
        protected override void SetOpacity(float t)
        {
            _projector.material.color = BaseColorGradient.Evaluate(1 - t);
            _projector.material.SetColor("_Emission", EmissionGradient.Evaluate(1 - t));
        }
    }
}
using UnityEngine;

namespace ExoplanetStudios
{
    public class GradientFadeController : FadeController
    {
        private Renderer[] graphics;
        
        [SerializeField] private Gradient Gradient;
        private void Awake()
        {
            graphics = GetComponentsInChildren<Renderer>();
        }
        protected override void SetOpacity(float t)
        {
            for (int i = 0; i < graphics.Length; i++)
                graphics[i].material.color = Gradient.Evaluate(1 - t);
        }
    }
}
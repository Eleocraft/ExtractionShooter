using UnityEngine;

namespace ExoplanetStudios
{
    public class MeshFadeController : FadeController
    {
        private Renderer[] graphics;
        
        private float[] maxAlpha;
        private void Awake()
        {
            graphics = GetComponentsInChildren<Renderer>();
            maxAlpha = new float[graphics.Length];
            for (int i = 0; i < graphics.Length; i++)
                maxAlpha[i] = graphics[i].material.color.a;
        }
        protected override void SetOpacity(float opacity)
        {
            for (int i = 0; i < graphics.Length; i++)
                graphics[i].material.color = new Color(graphics[i].material.color.r, graphics[i].material.color.g, graphics[i].material.color.b, opacity * maxAlpha[i]);
        }
    }
}
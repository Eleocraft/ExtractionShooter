using UnityEngine;

namespace ExoplanetStudios
{
    public class AlphaMeshFadeController : FadeController
    {
        private Renderer[] graphics;
        
        private float[] maxAlpha;
        private void Awake()
        {
            graphics = GetComponentsInChildren<Renderer>();
            maxAlpha = new float[graphics.Length];
            for (int i = 0; i < graphics.Length; i++)
                maxAlpha[i] = graphics[i].material.GetFloat("_Alpha");
        }
        protected override void SetOpacity(float opacity)
        {
            for (int i = 0; i < graphics.Length; i++)
                graphics[i].material.SetFloat("_Alpha", opacity);
        }
    }
}
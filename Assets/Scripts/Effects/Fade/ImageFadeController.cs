using UnityEngine.UI;
using UnityEngine;

namespace ExoplanetStudios
{
    public class ImageFadeController : FadeController
    {
        private MaskableGraphic[] graphics;
        
        private float[] maxAlpha;
        private void Awake()
        {
            graphics = GetComponentsInChildren<MaskableGraphic>();
            maxAlpha = new float[graphics.Length];
            for (int i = 0; i < graphics.Length; i++)
                maxAlpha[i] = graphics[i].color.a;
        }
        protected override void SetOpacity(float opacity)
        {
            for (int i = 0; i < graphics.Length; i++)
                graphics[i].color = new Color(graphics[i].color.r, graphics[i].color.g, graphics[i].color.b, opacity * maxAlpha[i]);
        }
    }
}
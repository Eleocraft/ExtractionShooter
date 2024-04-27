using UnityEngine;
using UnityEngine.UI;

namespace ExoplanetStudios.ExtractionShooter
{
    [RequireComponent(typeof(Button))]
    public class ButtonLightClick : MonoBehaviour
    {
        void Start() {
            GetComponent<Button>().onClick.AddListener(() => SFXSource.PlaySoundEffect(DefaultSoundEffect.MenuClickLight));
        }
    }
}

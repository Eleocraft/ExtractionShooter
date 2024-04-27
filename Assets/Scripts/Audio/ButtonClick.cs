using UnityEngine;
using UnityEngine.UI;

namespace ExoplanetStudios.ExtractionShooter
{
    [RequireComponent(typeof(Button))]
    public class ButtonClick : MonoBehaviour
    {
        void Start() {
            GetComponent<Button>().onClick.AddListener(() => SFXSource.PlaySoundEffect(DefaultSoundEffect.MenuClick));
        }
    }
}

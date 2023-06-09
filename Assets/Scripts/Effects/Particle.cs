using UnityEngine;
using UnityEngine.VFX;

public class Particle : MonoBehaviour
{
    [SerializeField] private float Lifetime;
    void Start()
    {
        GetComponent<VisualEffect>().Play();
        Invoke("Destroy", Lifetime);
    }
    void Destroy() => Destroy(gameObject);
}

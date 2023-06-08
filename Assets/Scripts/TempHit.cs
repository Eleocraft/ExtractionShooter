
using UnityEngine;

public class TempHit : MonoBehaviour
{
    void Start()
    {
        Invoke("Destroy", 1);
    }
    private void Destroy() => Destroy(gameObject);
}

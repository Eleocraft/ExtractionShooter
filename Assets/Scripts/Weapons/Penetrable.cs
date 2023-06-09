using UnityEngine;

public class Penetrable : MonoBehaviour
{
    [Range(0, 1)] public float Resistance;
    public virtual void Penetrate() { }
}

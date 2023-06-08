using UnityEngine;
using Unity.Netcode;

public class SetInvisible : NetworkBehaviour
{
    void Start()
    {
        if (IsOwner)
            GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
    }
}

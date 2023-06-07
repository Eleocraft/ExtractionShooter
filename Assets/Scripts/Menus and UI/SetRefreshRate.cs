using UnityEngine;

public class SetRefreshRate : MonoBehaviour
{
    [SerializeField] private int _targetFrameRate = 144;
    void Start()
    {
        Application.targetFrameRate = _targetFrameRate;
    }
}

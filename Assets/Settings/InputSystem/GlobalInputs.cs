using UnityEngine;

[CreateAssetMenu(fileName = "New Global Inputs", menuName = "CustomObjects/Inputs")]
public class GlobalInputs : ScriptableObject
{
    private InputMaster _controls;
    public InputMaster Controls
    {
        get
        {
            if (_controls == null)
            {
                _controls = new();
                _controls.Enable();
            }
            
            return _controls;
        }
    }
    public void Reset()
    {
        _controls = null;
    }
}

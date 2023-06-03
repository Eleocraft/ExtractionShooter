using UnityEngine;

[CreateAssetMenu(fileName = "Custom/GlobalInputs")]
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
                //Cursor.lockState = CursorLockMode.Locked;
            }
            
            return _controls;
        }
    }
}

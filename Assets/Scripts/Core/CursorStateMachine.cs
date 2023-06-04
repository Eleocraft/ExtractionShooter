using UnityEngine;

public class CursorStateMachine : MonoBehaviour
{
    public static InstanceLocker locker = new();
    public static bool Locked => !locker.Locked;
    private static InputMaster controls;
    [SerializeField]
	private GlobalInputs GI;
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        controls = GI.Controls;
    }
    void OnDestroy()
    {
        locker = new();
    }
    public static bool AlreadyLocked(Object lockObj) => locker.AlreadyLocked(lockObj);
    public static void ChangeCursorState(bool locking, Object lockObj)
    {
        if (!locking)
            locker.AddLock(lockObj);
        else if (locker.LockedByObj(lockObj))
            locker.RemoveLock(lockObj);

        if (Locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            controls.Mouse.Enable();
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            controls.Mouse.Disable();
        }
    }
}

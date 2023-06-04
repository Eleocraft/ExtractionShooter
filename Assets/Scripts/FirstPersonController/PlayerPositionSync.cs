using UnityEngine;
using Unity.Netcode;

public class PlayerPositionSync : NetworkBehaviour
{
	private const float _positionAsyncDist = 0.05f;
    
	// Sync Network Variables
	private NetworkVariable<Vector3> _position = new NetworkVariable<Vector3>();
	private NetworkVariable<Vector3> _clientPosition = new NetworkVariable<Vector3>(writePerm: NetworkVariableWritePermission.Owner);
    public override void OnNetworkSpawn()
	{
        if (IsServer)
            _clientPosition.OnValueChanged += SyncClientPosition;
        else
		    _position.OnValueChanged += SyncPosition;
	}
	public override void OnDestroy()
	{
		base.OnDestroy();
        if (IsServer)
        _clientPosition.OnValueChanged -= SyncClientPosition;
        else
		    _position.OnValueChanged -= SyncPosition;
    }
    void Update()
    {
        if (IsServer)
			_position.Value = transform.position;
        if (IsOwner)
            _clientPosition.Value = transform.position;

    }
    private void SyncPosition(Vector3 oldPosition, Vector3 position)
	{
		if (IsOwner)
		{
			if (position.sqrMagnitude - transform.position.sqrMagnitude > _positionAsyncDist)
				transform.position = position;
			return;	
		}
		// Interpolation here
		transform.position = position;
	}
    private void SyncClientPosition(Vector3 oldPosition, Vector3 position)
    {
        if (position.sqrMagnitude - transform.position.sqrMagnitude <= _positionAsyncDist)
			transform.position = position;
		return;	
    }
}

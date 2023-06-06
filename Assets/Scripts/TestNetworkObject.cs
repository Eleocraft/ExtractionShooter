using Unity.Netcode;
using UnityEngine;

namespace ExoplanetStudios
{
    public class TestNetworkObject : NetworkBehaviour
    {
        [SerializeField] GlobalInputs GI;
        [SerializeField] float Speed = 2;
        [SerializeField] GameObject CameraSocket;
        float _lastTick;
        float _startTime;
        Vector2 _startPos;
        float _lastDeltaTime;
        void Start()
        {
            NetworkManager.NetworkTickSystem.Tick += Tick;
            if (IsOwner)
            {
                GameObject.FindGameObjectWithTag("PlayerCam").GetComponent<Cinemachine.CinemachineVirtualCamera>().Follow = CameraSocket.transform;
            }
        }
        void Tick()
        {
            Vector2 move = Vector2.up;
            float deltaTime = Time.time - _lastTick;
            if (IsOwner)
            {
                MoveServerRpc(move, deltaTime);
                if (!IsServer)
                    Move(move, deltaTime);
            }
            //Debug.Log(deltaTime);
            _lastTick = Time.time;
        }
        void Update()
        {
            if (IsOwner && (transform.position.XZ() - _startPos).sqrMagnitude > 1000)
            {
                float deltaTime = Time.time - _startTime;
                Debug.Log(Mathf.Abs(deltaTime - _lastDeltaTime));
                _lastDeltaTime = deltaTime;
                _startPos = transform.position.XZ();
                _startTime = Time.time;
            }
        }
        [ServerRpc]
        void MoveServerRpc(Vector2 movement, float deltaTime)
        {
            Move(movement, deltaTime);
        }
        void Move(Vector2 movement, float deltaTime)
        {
            transform.Translate((movement * Speed * NetworkManager.NetworkTickSystem.LocalTime.FixedDeltaTime).AddHeight(0));
        }
    }
}

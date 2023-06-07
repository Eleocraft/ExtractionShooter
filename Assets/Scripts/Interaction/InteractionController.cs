using UnityEngine;
using TMPro;

namespace ExoplanetStudios
{
    public class InteractionController : MonoSingleton<InteractionController>
    {
        private InputMaster controls;
        private InteractionData currentInteraction;
        private InteractionData activeInteraction;
        [SerializeField] private GlobalInputs GI;
        [Header("--Raycast")]
        [Range(0.1f, 3)] [SerializeField] private float HitRange;
        [SerializeField] private Transform CameraTransform;
        [SerializeField] private LayerMask InteractableLayers;
        [Header("--Interaction")]
        [SerializeField] private TMP_Text informationField;
        
        void Start()
        {
            controls = GI.Controls;
            controls.Player.Interaction.performed += Interact;
            controls.Player.Interaction.started += StartInteraction;
            controls.Player.Interaction.canceled += StopInteraction;
        }
        void OnDestroy()
        {
            controls.Player.Interaction.performed -= Interact;
            controls.Player.Interaction.started -= StartInteraction;
            controls.Player.Interaction.canceled -= StopInteraction;
        }
        void FixedUpdate()
        {
            // Raycast to check if object is hit
            if (Physics.Raycast(CameraTransform.position, CameraTransform.TransformDirection(Vector3.forward), out RaycastHit hitData, HitRange, InteractableLayers))
            {
                // Process the information
                if (hitData.collider.gameObject.TryGetComponent(out IInteractable interactable))
                {
                    // the object can be interacted with directly
                    currentInteraction = new InteractionData(interactable, hitData);
                    // Display Information
                    if (interactable.InteractionInfo != informationField.text)
                        informationField.text = interactable.InteractionInfo;
                    return;
                }
            }
            if (informationField.text != "")
                informationField.text = "";
            currentInteraction = null;
        }
        void Interact(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
        {
            if (currentInteraction != null)
                currentInteraction.interactable.Interact();
        }
        void StartInteraction(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
        {
            if (currentInteraction != null)
            {
                currentInteraction.interactable.StartInteraction();
                activeInteraction = currentInteraction;
            }
        }
        void StopInteraction(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
        {
            if (activeInteraction != null)
            {
                activeInteraction.interactable.StopInteraction();
                activeInteraction = null;
            }
        }
        // Get the interaction data
        public static InteractionData GetInteraction()
        {
            return Instance.currentInteraction;
        }
        // Version of the GetInteraction for If-statements
        public static bool TryGetInteraction(out InteractionData interactionData)
        {
            interactionData = Instance.currentInteraction;
            if (Instance.currentInteraction != null)
                return true;
            return false;
        }
    }
    public class InteractionData
    {
        public IInteractable interactable { get; private set; }
        public RaycastHit hitData { get; private set; }
        public GameObject interactableObject => hitData.collider.gameObject;

        public InteractionData(IInteractable interactable, RaycastHit hitData)
        {
            this.interactable = interactable;
            this.hitData = hitData;
        }
    }
    public interface IInteractable
    {
        void Interact() { }
        void StartInteraction() { }
        void StopInteraction() { }
        string InteractionInfo { get; }
    }
    public class InteractionTrigger : MonoBehaviour, IInteractable
    {
        private IInteractable interactable;
        public string InteractionInfo => interactable?.InteractionInfo;
        public void Initialize(IInteractable interactable)
        {
            this.interactable = interactable;
        }
        public void Interact() => interactable?.Interact();
        public void StartInteraction() => interactable?.StartInteraction();
        public void StopInteraction() => interactable?.StopInteraction();
    }
}
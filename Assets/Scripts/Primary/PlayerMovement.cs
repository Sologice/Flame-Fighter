using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private Camera cam;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 50f;
    [SerializeField] private Vector2 moveInput;

    [Header("Rotation Settings")]
    [SerializeField] private Vector3 rot;
    [SerializeField] private float rotXMin = -45;
    [SerializeField] private float rotXMax = 15;

    private void Start()
    {
        if (cam == null)
            cam = GetComponentInChildren<Camera>();
    }

    private void FixedUpdate()
    {
        MovePlayer();
    }

    private void OnEnable()
    {
        InputManager.Instance.OnChange += OnChange;
    }

    private void OnDisable()
    {
        InputManager.Instance.OnChange -= OnChange;
    }

    private void OnChange()
    {
        InputManager.Instance.ToggleFixedInputs();
    }

    private void MovePlayer()
    {
        moveInput = moveSpeed * Time.fixedDeltaTime * InputManager.Instance.MoveInput;

        rot.x = Mathf.Clamp(rot.x - moveInput.y, rotXMin, rotXMax);
        rot.y += moveInput.x;
        transform.localRotation = Quaternion.Euler(rot);

        // Debug.Log($"Moving by {(moveInput, 0f)}");
    }
}
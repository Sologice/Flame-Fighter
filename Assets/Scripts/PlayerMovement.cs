using UnityEngine;

[RequireComponent(typeof(Camera))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private Camera cam;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private Vector2 moveInput;

    private void Start()
    {
        if (cam == null)
            cam = GetComponent<Camera>();
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

        transform.Rotate(moveInput.x, moveInput.y, 0f);

        Debug.Log($"Moving by {(moveInput, 0f)}");
    }
}
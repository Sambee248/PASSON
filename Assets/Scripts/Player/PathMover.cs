using System;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public sealed class PathMover : MonoBehaviour
{
    [SerializeField] private float speed = 3f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float killHeight = -5f;
    [SerializeField] private Transform cameraTransform;

    private CharacterController characterController;
    private Vector2 input;
    private float verticalVelocity;

    public event Action Fell;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        characterController.minMoveDistance = 0f;
        // The player is driven exclusively by PlayerPathController. This component
        // remains on older prefabs for shadow replay compatibility.
        if (CompareTag("Player"))
        {
            enabled = false;
        }
    }

    private void Update()
    {
        var referenceTransform = cameraTransform != null
            ? cameraTransform
            : Camera.main != null ? Camera.main.transform : transform;

        var forward = Vector3.ProjectOnPlane(referenceTransform.forward, Vector3.up).normalized;
        var right = Vector3.ProjectOnPlane(referenceTransform.right, Vector3.up).normalized;
        var direction = forward * input.y + right * input.x;

        if (characterController.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        verticalVelocity += gravity * Time.deltaTime;
        var horizontalVelocity = Vector3.ClampMagnitude(direction, 1f) * speed;
        characterController.Move((horizontalVelocity + Vector3.up * verticalVelocity) * Time.deltaTime);

        if (transform.position.y < killHeight)
        {
            Fell?.Invoke();
            if (CompareTag("Player"))
            {
                FindObjectOfType<CheckpointService>()?.NotifyPlayerFailure();
            }
        }
    }

    public void SetInput(Vector2 moveInput)
    {
        input = Vector2.ClampMagnitude(moveInput, 1f);
    }
}

using System;
using UnityEngine;

public sealed class PlayerCommandSource : MonoBehaviour
{
    [SerializeField] private PlayerPathController playerPathController;

    private int nextSequence;
    private Vector2 lastMoveInput;

    public event Action<PlayerCommand> CommandIssued;

    private void Awake()
    {
        if (playerPathController == null)
        {
            playerPathController = GetComponent<PlayerPathController>();
        }
    }

    private void Update()
    {
        var inputLocked = playerPathController != null && playerPathController.IsMoving;
        var moveInput = inputLocked ? Vector2.zero : ReadMoveInput();
        if (moveInput != lastMoveInput)
        {
            PublishMove(moveInput);
        }

        if (!inputLocked && Input.GetKeyDown(KeyCode.E))
        {
            PublishInteractForTest();
        }
    }

    public void PublishMoveForTest(Vector2 moveInput)
    {
        PublishMove(moveInput);
    }

    public void PublishInteractForTest()
    {
        if (playerPathController != null && playerPathController.IsMoving) return;
        CommandIssued?.Invoke(PlayerCommand.Interact(++nextSequence));
    }

    private void PublishMove(Vector2 moveInput)
    {
        lastMoveInput = moveInput;
        CommandIssued?.Invoke(PlayerCommand.Move(++nextSequence, moveInput));
    }

    private static Vector2 ReadMoveInput()
    {
        var horizontal = (Input.GetKey(KeyCode.D) ? 1f : 0f) - (Input.GetKey(KeyCode.A) ? 1f : 0f);
        var vertical = (Input.GetKey(KeyCode.W) ? 1f : 0f) - (Input.GetKey(KeyCode.S) ? 1f : 0f);
        return Vector2.ClampMagnitude(new Vector2(horizontal, vertical), 1f);
    }
}

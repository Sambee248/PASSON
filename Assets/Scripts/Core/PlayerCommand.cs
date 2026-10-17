using UnityEngine;

public enum PlayerCommandType
{
    Move,
    Interact
}

public enum ActorKind
{
    Player,
    Shadow
}

public readonly struct PlayerCommand
{
    public int Sequence { get; }
    public PlayerCommandType Type { get; }
    public Vector2 Direction { get; }

    private PlayerCommand(int sequence, PlayerCommandType type, Vector2 move)
    {
        Sequence = sequence;
        Type = type;
        Direction = move;
    }

    public static PlayerCommand Move(int sequence, Vector2 move)
    {
        return new PlayerCommand(sequence, PlayerCommandType.Move, move);
    }

    public static PlayerCommand Interact(int sequence)
    {
        return new PlayerCommand(sequence, PlayerCommandType.Interact, Vector2.zero);
    }
}

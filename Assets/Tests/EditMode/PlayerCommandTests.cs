using NUnit.Framework;

public sealed class PlayerCommandTests
{
    [Test]
    public void Move_command_retains_sequence_and_direction()
    {
        var command = PlayerCommand.Move(12, new UnityEngine.Vector2(0f, 1f));

        Assert.That(command.Sequence, Is.EqualTo(12));
        Assert.That(command.Type, Is.EqualTo(PlayerCommandType.Move));
        Assert.That(command.Direction, Is.EqualTo(new UnityEngine.Vector2(0f, 1f)));
    }
}

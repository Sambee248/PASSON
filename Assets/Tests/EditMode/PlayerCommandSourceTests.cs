using NUnit.Framework;
using UnityEngine;

public sealed class PlayerCommandSourceTests
{
    [Test]
    public void Non_zero_input_issues_an_increasing_move_command()
    {
        var source = new GameObject().AddComponent<PlayerCommandSource>();
        PlayerCommand received = default;
        source.CommandIssued += command => received = command;

        source.PublishMoveForTest(new Vector2(1f, 0f));

        Assert.That(received.Sequence, Is.EqualTo(1));
        Assert.That(received.Type, Is.EqualTo(PlayerCommandType.Move));

        Object.DestroyImmediate(source.gameObject);
    }
}

using Godot;
using System;

public class Movable : Node2D
{
    [Export]
    public WordObject Object;

    [Export]
    public WordRelation Relation;

    [Export]
    public WordProperty Property;

    private float movementTimer = 0;
    private float movementMultiplier = 0;
    private Vector2 movementStep = new Vector2();
    private Vector2 targetPosition = new Vector2();

    public bool IsPushable { get; protected set; } = false;
    public bool IsControllable { get; protected set; } = false;
    public bool IsStop { get; protected set; } = false;

    public override void _EnterTree()
    {
        LevelController.Instance.RegisterMovable(this);
    }

    public override void _ExitTree() 
    {
        LevelController.Instance.UnregisterMovable(this);
    }

    public override void _Process(float delta)
    {
        if (movementTimer > 0)
        {
            Position += movementStep * delta * movementMultiplier;
            movementTimer -= delta;

            if (movementTimer <= 0)
            {
                Position = targetPosition;
                targetPosition = Vector2.Zero;
            }
        }
    }

    /// <summary>
    /// Moves the specified movement.
    /// </summary>
    /// <param name="movement">The movement.</param>
    /// <param name="time">The time.</param>
    /// <returns>True if movement is possible, otherwise false.</returns>
    public bool Move(Vector2 movement, float time)
    {
        if (targetPosition != Vector2.Zero) return false;

        if (!IsControllable && !IsPushable /*&& !IsMove*/ && IsStop) return false;

        if (!IsControllable && !IsStop && !IsPushable) return true;

        Vector2 newPosition = Position + movement;
        Movable match;

        // check if position is reachable
        if (!LevelController.Instance.CanReachPosition(newPosition, out match)) return false;

        // try to plan move for Movable at newPosition if there is any
        if (match != null && !match.Move(movement, time)) return false;

        targetPosition = newPosition;
        movementStep = movement;
        movementTimer = time;
        movementMultiplier = 1 / time;

        // check all movables in movement direction to see if movement is possible, if not, don't move         
        // in other words, try to call Move on adjacent movable, see what the fuck happens

        return true;
    }
}
using Godot;
using System;

public class Thing : Movable
{
    private bool isWinCondition = false;

    public virtual void IsProperty(WordProperty property, bool state)
    {
        if (state)
            GD.Print(Object.ToString().ToLower(), " IS ", property.ToString().ToLower(), " ", state);

        switch (property)
        {
            case WordProperty.PUSH:
                IsPushable = state;
                break;
            case WordProperty.STOP:
                IsStop = state;
                break;
            case WordProperty.YOU:
                IsControllable = state;
                if(IsControllable)
                {
                    AddToGroup(LevelController.G_CONTROLLABLE);
                    ZIndex = 1;
                }
                else
                {
                    RemoveFromGroup(LevelController.G_CONTROLLABLE);
                    ZIndex = 0;
                }
                break;
            case WordProperty.WIN:
                isWinCondition = state;
                break;
            default:
                break;
        }
    }

    public void IsThing(string thing)
    {
        if (thing == WordObject.NONE.ToString().ToLower()) {
            return;
        }

        var scene = (PackedScene)ResourceLoader.Load("res://movables/" + thing + ".tscn");
        Node2D newThing = (Node2D)scene.Instance();
        newThing.Position = Position;
        LevelController.Instance.AddChild(newThing);
        Free();
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        if (!isWinCondition) return;

        var group = GetTree().GetNodesInGroup(LevelController.G_CONTROLLABLE);
        for (int i = 0; i < group.Count; i++) 
        {
            if (((Movable)group[i]).Position == Position) 
            {
                GD.Print("Win");
            }
        }
    }
}

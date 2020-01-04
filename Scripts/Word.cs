using Godot;
using System;
using System.Collections.Generic;

public class Word : Movable
{
    [Export]
    public WordType Type = WordType.Object;
    
    public override void _EnterTree()
    {
        base._EnterTree();
        AddToGroup(LevelController.G_WORD);
    }

    public override void _Ready()
    {
        IsPushable = true;
    }
}

public enum WordType
{
    Object,
    Relation,
    Property
}

public enum WordObject 
{
    NONE,
    BABA,
    ROCK,
    FLAG,
    WALL
}

public enum WordRelation 
{
    NONE,
    IS,
    AND
}

public enum WordProperty
{
    NONE,
    STOP,
    PUSH,
    YOU,
    WIN
}

public struct Rule
{
    public List<WordObject> Objects;
    public List<WordProperty> Properties;
    public List<WordObject> TargetObjects;

    public Rule(List<WordObject> objects, List<WordProperty> properties, List<WordObject> targetObjects)
    {
        Objects = objects;
        Properties = properties;
        TargetObjects = targetObjects;
    }
}

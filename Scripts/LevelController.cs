using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;

public class LevelController : Node2D
{
    // groups
    public const string G_CONTROLLABLE = "controllable";
    public const string G_WORD = "word";

    private int width = 33;
    private int height = 18;
    private float gridBlockSize = 26;
    private float tickTime = 0.2f;

    private List<Rule> rules = new List<Rule>();
    private Array<Movable> movables = new Array<Movable>();
    private float refreshTimer = 0.00001f;
    private bool refreshWords = false;

    public static LevelController Instance;

    public override void _EnterTree()
    {
        Instance = this;
    }

    public override void _Process(float delta)
    {
        // refresh words timer
        if (refreshTimer > 0)
        {
            refreshTimer -= delta;

            if (refreshTimer <= 0)
            {
                RefreshWords();
                refreshWords = false;
                refreshTimer = 0;
            }
        }

        // controls
        int horizontal = 0;
        int vertical = 0;
        horizontal += Input.IsActionPressed("ui_right") ? 1 : 0;
        horizontal -= Input.IsActionPressed("ui_left") ? 1 : 0;
        vertical -= Input.IsActionPressed("ui_up") ? 1 : 0;
        vertical += Input.IsActionPressed("ui_down") ? 1 : 0;

        var movement = new Vector2(vertical == 0 ? horizontal : 0, vertical) * gridBlockSize;
        bool isMoving = false;

        if (movement != Vector2.Zero)
        {
            var list = GetTree().GetNodesInGroup(G_CONTROLLABLE);
            for (int i = 0; i < list.Count; i++)
            {
                Movable m = (Movable)list[i];
                isMoving = m.Move(movement, tickTime);
            }

            if (isMoving)
            {
                Tick();
            }
        }
        else if (Input.IsActionJustReleased("wait"))
        {
            Tick();
        }
    }

    private void Tick()
    {
        refreshTimer = tickTime;
    }

    private void RefreshWords()
    {
        var group = GetTree().GetNodesInGroup(G_WORD);

        SetRulesApplied(false);
        rules.Clear();
        if (group.Count == 0) return;

        // populate sorted list
        List<Word> wordList = new List<Word>();
        for (int i = 0; i < group.Count; i++)
        {
            wordList.Add((Word)group[i]);
        }

        // sort for reading in up -> down direction
        wordList.Sort((w1, w2) =>
        {
            return (w1.Position.x < w2.Position.x) ? -1 :
                    ((w1.Position.x == w2.Position.x && w1.Position.y < w2.Position.y) ? -1 : 1);
        });

        List<Word> pendingRule = new List<Word>();
        for (int i = 0; i < wordList.Count; i++)
        {
            if (pendingRule.Count != 0 && pendingRule[pendingRule.Count - 1].Position != wordList[i].Position + Vector2.Up * gridBlockSize)
            {
                MakeAllRuleSubsets(pendingRule);
                pendingRule.Clear();
            }
                
            pendingRule.Add(wordList[i]);
        }
        MakeAllRuleSubsets(pendingRule);

        // sort for reading in left -> right direction
        wordList.Sort((w1, w2) =>
        {
            return (w1.Position.y < w2.Position.y) ? -1 :
                    ((w1.Position.y == w2.Position.y && w1.Position.x < w2.Position.x) ? -1 : 1);
        });

        pendingRule.Clear();
        for (int i = 0; i < wordList.Count; i++)
        {
            if (pendingRule.Count != 0 && pendingRule[pendingRule.Count - 1].Position != wordList[i].Position - Vector2.Right * gridBlockSize)
            {
                MakeAllRuleSubsets(pendingRule);
                pendingRule.Clear();
            }

            pendingRule.Add(wordList[i]);
        }
        MakeAllRuleSubsets(pendingRule);

        SetRulesApplied(true);
        refreshWords = false;
    }

    private void SetRulesApplied(bool apply)
    {
        foreach (var r in rules)
        {
            foreach (var o in r.Objects)
            {
                foreach (var m in movables)
                {
                    if (m is Thing && m.Object == o)
                    {
                        Thing t = (Thing)m;
                        foreach (var p in r.Properties)
                        {
                            t.IsProperty(p, apply);
                        }

                        foreach (var to in r.TargetObjects)
                        {
                            t.IsThing(to.ToString().ToLower());
                        }
                    }
                }
            }
        }
    }

    private void MakeAllRuleSubsets(List<Word> words)
    {
        for (int i = words.Count; i >= 3; i--)
        {
            if (MakeRule(words.GetRange(0, i)))
            {
                //return;
            }
        }

        for (int i = 1; i < words.Count; i++)
        {
            if (MakeRule(words.GetRange(i, words.Count - i)))
            {
                //return;
            }
        }
    }

    private bool MakeRule(List<Word> words)
    {
        // TODO aggregation
        List<WordObject> objects = new List<WordObject>();
        List<WordProperty> properties = new List<WordProperty>();
        List<WordObject> targetObjects = new List<WordObject>();

        bool waitingForRelation = false;
        bool foundIs = false;
        for (int i = 0; i < words.Count; i++)
        {
            if (!waitingForRelation && words[i].Type == WordType.Object)
            {
                if (!foundIs)
                {
                    objects.Add(words[i].Object);
                    waitingForRelation = true;
                }
                else
                {
                    if (words[i].Property != WordProperty.NONE) {
                        properties.Add(words[i].Property);
                    }
                    else if (words[i].Object != WordObject.NONE) {
                        targetObjects.Add(words[i].Object);
                    }
                    rules.Add(new Rule(objects, properties, targetObjects));
                    return true;
                }
            }
            else if (!waitingForRelation && words[i].Type == WordType.Property)
            {
                if (foundIs)
                {
                    properties.Add(words[i].Property);
                    rules.Add(new Rule(objects, properties, targetObjects));
                    return true;
                }
            }
            else if (waitingForRelation && words[i].Type == WordType.Relation)
            {
                if (words[i].Relation == WordRelation.IS)
                {
                    waitingForRelation = false;
                    foundIs = true;
                }
            }
            else return false; // broken rule
        }

        return false;
    }

    public void RegisterMovable(Movable movable)
    {
        movables.Add(movable);
    }

    public void UnregisterMovable(Movable movable)
    {
        movables.Remove(movable);
    }

    public bool CanReachPosition(Vector2 position, out Movable match)
    {
        match = null;

        Vector2 adjustedPosition = position - new Vector2(gridBlockSize / 2, gridBlockSize / 2);

        if(adjustedPosition.x < 0 || adjustedPosition.x >= gridBlockSize * width ||
        adjustedPosition.y < 0 || adjustedPosition.y >= gridBlockSize * height) return false;

        foreach (var m in movables)
        {
            if (m.Position == position)
            {
                if (m.IsStop && !m.IsPushable)
                {
                    match = null;
                    return false;
                }
                else if (match == null)
                {
                    match = m.IsPushable ? m : null;
                }
            }
        }

        return true;
    }
}

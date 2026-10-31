using System.Collections.Generic;
using UnityEngine;

// A scene's dialogue, read from a plain text file. The format is explained at the top of
// Assets/Dialogue/Opening.txt.
public class ConversationScript
{
    public const string EndLabel = "end";

    public class Reply
    {
        public string Text;
        public string Target;

        // Written in [square brackets]: something the player does rather than says.
        public bool IsAction => Text.StartsWith("[");
    }

    public class Step
    {
        public string Speaker;
        public string Text;
        public string Jump;
        public List<Reply> Replies;
    }

    readonly List<Step> steps = new List<Step>();
    readonly Dictionary<string, int> labels = new Dictionary<string, int>();

    public int Count => steps.Count;
    public Step this[int index] => steps[index];

    public ConversationScript(TextAsset source)
    {
        Step choice = null;
        foreach (string rawLine in source.text.Split('\n'))
        {
            string line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith("#")) continue;

            if (line.StartsWith("="))
            {
                labels[line.Substring(1).Trim()] = steps.Count;
                choice = null;
            }
            else if (line.StartsWith("->"))
            {
                steps.Add(new Step { Jump = line.Substring(2).Trim() });
                choice = null;
            }
            else if (line.StartsWith(">"))
            {
                if (choice == null)
                {
                    choice = new Step { Replies = new List<Reply>() };
                    steps.Add(choice);
                }
                choice.Replies.Add(ParseReply(line.Substring(1)));
            }
            else
            {
                int colon = line.IndexOf(':');
                if (colon < 0) Debug.LogError($"{source.name}: can't read \"{line}\". Spoken lines need NAME: text.");
                else steps.Add(new Step { Speaker = line.Substring(0, colon).Trim(), Text = line.Substring(colon + 1).Trim() });
                choice = null;
            }
        }
        CheckJumps(source.name);
    }

    // Where a label starts, or -1 for the end of the conversation.
    public int Find(string label)
    {
        return labels.TryGetValue(label, out int index) ? index : -1;
    }

    static Reply ParseReply(string text)
    {
        int arrow = text.IndexOf("->", System.StringComparison.Ordinal);
        if (arrow < 0) return new Reply { Text = text.Trim() };
        return new Reply { Text = text.Substring(0, arrow).Trim(), Target = text.Substring(arrow + 2).Trim() };
    }

    // A mistyped label would silently end the conversation, so report it as soon as the file loads.
    void CheckJumps(string fileName)
    {
        void Check(string target)
        {
            if (target != null && target != EndLabel && !labels.ContainsKey(target))
                Debug.LogError($"{fileName}: nothing is labelled \"{target}\".");
        }

        foreach (Step step in steps)
        {
            Check(step.Jump);
            if (step.Replies == null) continue;
            foreach (Reply reply in step.Replies) Check(reply.Target);
        }
    }
}

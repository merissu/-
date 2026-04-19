using System;
using System.Collections.Generic;
using Verse;

namespace merissu
{
    public enum DialogueMood { Ordinary, Excitement, Think, Touching }

    public class DialogueOption
    {
        public string Text;
        public Action OnSelect;
        public int NextStage;
    }

    public class DialogueLine
    {
        public Pawn Speaker;
        public string Text;
        public DialogueMood Mood;
        public bool IsBoth = false;
        public List<DialogueOption> Options = null;

        public Action OnSelect = null;
        public int NextStage = -1;
    }
}
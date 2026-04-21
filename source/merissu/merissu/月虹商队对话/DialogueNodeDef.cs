using System.Collections.Generic;
using Verse;
using RimWorld;

namespace merissu
{
    public enum DialogueSpeaker { Buyer, Seller }
    public enum SkillCompareType { None, Combat, Social, Intellectual }

    public class DialogueNodeDef : Def
    {
        public int stage;
        public DialogueSpeaker speaker = DialogueSpeaker.Seller;
        public string text;
        public DialogueMood mood = DialogueMood.Ordinary;
        public int requireSocial = 0; 
        public int defaultNextStage = -1; 
        public DialogueEventDef onLineComplete;
        public List<string> texts = new List<string>();
        public List<DialogueOptionDef> options = new List<DialogueOptionDef>();
        public string DisplayText
        {
            get
            {
                if (texts != null && texts.Count > 0)
                {
                    return texts.RandomElement(); 
                }
                return text ?? defName;
            }
        }
    }

    public class DialogueOptionDef
    {
        public string text;
        public int nextStage = -1;
        public List<int> randomSuccessStages;
        public List<int> randomFailStages; 
        public DialogueEventDef onSelect;
        public List<string> texts = new List<string>(); 
        public SkillCompareType skillCheck = SkillCompareType.None;
        public int successStage = -1;
        public DialogueEventDef onSuccess; 
        public int failStage = -1;
        public DialogueEventDef onFail;    
    }

    public class DialogueEventDef
    {
        public int goodwillChange = 0;
        public string messageText;
        public MessageTypeDef messageType;
        public string customAction; 
    }
}
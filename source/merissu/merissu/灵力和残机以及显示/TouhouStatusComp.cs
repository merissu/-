using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;

namespace merissu
{
    [StaticConstructorOnStartup]
    public class Gizmo_TouhouStatus : Gizmo
    {
        private Pawn pawn;

        private static readonly Texture2D Tex_UpA = ContentFinder<Texture2D>.Get("Other/UpA");
        private static readonly Texture2D Tex_UpB = ContentFinder<Texture2D>.Get("Other/UpB");
        private static readonly Texture2D Tex_SpellA = ContentFinder<Texture2D>.Get("Other/SpellA");
        private static readonly Texture2D Tex_SpellB = ContentFinder<Texture2D>.Get("Other/SpellB");
        private static readonly Texture2D Tex_Back = ContentFinder<Texture2D>.Get("Other/Touhou_Back");

        private static readonly Texture2D Tex_LabelUp = ContentFinder<Texture2D>.Get("Other/Label_Up"); 
        private static readonly Texture2D Tex_LabelSpell = ContentFinder<Texture2D>.Get("Other/Label_Spell"); 

        public Gizmo_TouhouStatus(Pawn pawn)
        {
            this.pawn = pawn;
            this.Order = -120f;
        }

        public override float GetWidth(float maxWidth) => 250f;

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            Rect baseRect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);

            GUI.DrawTexture(baseRect, Tex_Back);

            int upLevel = Mathf.FloorToInt(pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("up"))?.Severity ?? 0f);
            int powerLevel = Mathf.FloorToInt(pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("FullPower"))?.Severity ?? 0f);

            float labelWidth = 55f;    
            float labelHeight = 20f;   
            float iconSize = 16f;      
            float spacing = 1.5f;      

            float contentStartX = baseRect.x + 8f; 
            float iconsStartX = contentStartX + labelWidth + 5f;  

            float upperY = baseRect.y + 16f; 
            float lowerY = baseRect.y + 44f; 

            Rect labelUpRect = new Rect(contentStartX, upperY - 2f, labelWidth, labelHeight);
            GUI.DrawTexture(labelUpRect, Tex_LabelUp);

            Rect labelSpellRect = new Rect(contentStartX, lowerY - 2f, labelWidth, labelHeight);
            GUI.DrawTexture(labelSpellRect, Tex_LabelSpell);

            for (int i = 0; i < 10; i++)
            {
                Rect upIconRect = new Rect(iconsStartX + i * (iconSize + spacing), upperY, iconSize, iconSize);
                GUI.DrawTexture(upIconRect, (i < upLevel) ? Tex_UpB : Tex_UpA);

                Rect spellIconRect = new Rect(iconsStartX + i * (iconSize + spacing), lowerY, iconSize, iconSize);
                GUI.DrawTexture(spellIconRect, (i < powerLevel) ? Tex_SpellB : Tex_SpellA);
            }

            return new GizmoResult(GizmoState.Clear);
        }
    }

    public class HediffComp_TouhouStatus : HediffComp
    {
        public override IEnumerable<Gizmo> CompGetGizmos()
        {
            bool isUp = this.Def.defName == "up";
            bool hasUp = Pawn.health.hediffSet.HasHediff(HediffDef.Named("up"));
            if (isUp || !hasUp)
            {
                yield return new Gizmo_TouhouStatus(Pawn);
            }
        }
    }

    public class HediffCompProperties_TouhouStatus : HediffCompProperties
    {
        public HediffCompProperties_TouhouStatus()
        {
            this.compClass = typeof(HediffComp_TouhouStatus);
        }
    }
}
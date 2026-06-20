using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace merissu
{
    public class CompAbilityEffect_Sunflowerraise : CompAbilityEffect
    {
        public static readonly Color DustColor = new Color(0.55f, 0.55f, 0.55f, 4f);

        public new CompProperties_AbilityWallraise Props => (CompProperties_AbilityWallraise)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Map map = parent.pawn.Map;

            ThingDef sunflowerDef = ThingDef.Named("YuukaSunflower");

            if (sunflowerDef == null)
            {
                Log.Error("[merissu] 未能找到 defName 为 YuukaSunflower 的向日葵定义。");
                return;
            }

            foreach (IntVec3 cell in AffectedCells(target, map))
            {
                Thing thing = GenSpawn.Spawn(sunflowerDef, cell, map);

                if (parent.pawn.Faction != null)
                {
                    thing.SetFaction(parent.pawn.Faction);
                }

                FleckMaker.ThrowDustPuffThick(cell.ToVector3Shifted(), map, Rand.Range(1.5f, 3f), DustColor);
            }

            CompAbilityEffect_Teleport.SendSkipUsedSignal(target, parent.pawn);
        }

        public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
        {
            return Valid(target, throwMessages: true);
        }

        public override void DrawEffectPreview(LocalTargetInfo target)
        {
            GenDraw.DrawFieldEdges(AffectedCells(target, parent.pawn.Map).ToList(), Valid(target) ? Color.white : Color.red);
        }

        private IEnumerable<IntVec3> AffectedCells(LocalTargetInfo target, Map map)
        {
            if (Props.pattern == null)
            {
                yield return target.Cell;
                yield break;
            }

            foreach (IntVec2 offset in Props.pattern)
            {
                IntVec3 targetCell = target.Cell + new IntVec3(offset.x, 0, offset.z);
                if (targetCell.InBounds(map))
                {
                    yield return targetCell;
                }
            }
        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            Map map = parent.pawn.Map;

            foreach (IntVec3 cell in AffectedCells(target, map))
            {
                if (cell.Filled(map))
                {
                    if (throwMessages) FailMessage("AbilityOccupiedCells", target);
                    return false;
                }

                if (!cell.Standable(map))
                {
                    if (throwMessages) FailMessage("AbilityUnwalkable", target);
                    return false;
                }

                List<Thing> thingList = cell.GetThingList(map);
                for (int i = 0; i < thingList.Count; i++)
                {
                    if (!thingList[i].def.destroyable)
                    {
                        if (throwMessages) FailMessage("AbilityNotEnoughFreeSpace", target);
                        return false;
                    }
                }
            }
            return true;
        }

        private void FailMessage(string translationKey, LocalTargetInfo target)
        {
            string message = "CannotUseAbility".Translate(parent.def.label) + ": " + translationKey.Translate();
            Messages.Message(message, target.ToTargetInfo(parent.pawn.Map), MessageTypeDefOf.RejectInput, historical: false);
        }
    }
}
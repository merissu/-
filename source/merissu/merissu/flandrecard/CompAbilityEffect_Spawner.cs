using RimWorld;
using Verse;
using System.Collections.Generic;

namespace merissu
{
    public class CompAbilityEffect_Spawner : CompAbilityEffect_WithDuration
    {
        public new CompProperties_AbilityGiveHediff Props => (CompProperties_AbilityGiveHediff)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            if (!Props.ignoreSelf || target.Pawn != parent.pawn)
            {
                if (!Props.onlyApplyToSelf && Props.applyToTarget && target.Pawn != null)
                {
                    ApplyInner(target.Pawn);
                }
                if (Props.applyToSelf || Props.onlyApplyToSelf)
                {
                    ApplyInner(parent.pawn);
                }
            }
        }

        protected void ApplyInner(Pawn target)
        {
            if (target == null || target.Map == null) return;

            GameComponent_PawnDuplicator duplicator = Current.Game.GetComponent<GameComponent_PawnDuplicator>();

            if (duplicator == null)
            {
                Log.Error("未找到 GameComponent_PawnDuplicator，无法生成分身。");
                return;
            }

            IntVec3 spawnPos = parent.pawn.Position;

            for (int i = 0; i < 3; i++)
            {
                Pawn clone = duplicator.Duplicate(target);
                if (clone == null) continue;

                GenSpawn.Spawn(clone, spawnPos, target.Map);

                clone.Rotation = parent.pawn.Rotation;

                if (target.apparel != null)
                {
                    foreach (Apparel wornItem in target.apparel.UnlockedApparel)
                    {
                        Apparel newApparel = (Apparel)ThingMaker.MakeThing(wornItem.def, wornItem.Stuff);
                        clone.apparel.Wear(newApparel, dropReplacedApparel: false, locked: true);
                    }
                }

                ThingDef weaponDef = ThingDef.Named("LaevatainF");
                if (weaponDef != null)
                {
                    ThingWithComps weapon = (ThingWithComps)ThingMaker.MakeThing(weaponDef);
                    clone.equipment.AddEquipment(weapon);
                }

                HediffDef hediffDef = HediffDef.Named("FourOfAKind");
                if (hediffDef != null)
                {
                    clone.health.AddHediff(hediffDef);
                }
            }
        }
    }
}
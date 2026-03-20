using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace merissu
{
    public class CompProperties_Yukaridisappearance : CompProperties_AbilityEffect
    {
        public CompProperties_Yukaridisappearance()
        {
            compClass = typeof(CompAbilityEffect_Yukaridisappearance);
        }
    }

    public class CompAbilityEffect_Yukaridisappearance : CompAbilityEffect
    {
        private static readonly SoundDef GapKillSound = SoundDef.Named("gapkill");

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            Pawn caster = parent.pawn;
            if (caster == null || caster.Map == null)
                return;

            Map map = caster.Map;

            float radius = parent.def.GetStatValueAbstract(StatDefOf.Ability_EffectRadius);
            IntVec3 center = target.Cell;

            List<Pawn> victims = new List<Pawn>();

            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, radius, true))
            {
                if (!cell.InBounds(map))
                    continue;

                foreach (Thing thing in cell.GetThingList(map))
                {
                    if (thing is Pawn p)
                    {
                        if (p == caster)
                            continue;

                        if (p.Dead || p.Destroyed || !p.Spawned)
                            continue;

                        if (p.Faction == null || !p.HostileTo(caster))
                            continue;

                        victims.Add(p);
                    }
                }
            }

            victims = victims.Distinct().ToList();

            foreach (Pawn victim in victims)
            {
                DoDisappear(victim, map);
            }
        }

        private void DoDisappear(Pawn targetPawn, Map map)
        {
            IntVec3 pos = targetPawn.Position;

            GapKillSound?.PlayOneShot(new TargetInfo(pos, map));

            FleckMaker.Static(pos, map, FleckDefOf.PsycastSkipFlashEntry);
            FleckMaker.Static(pos, map, FleckDefOf.PsycastSkipInnerExit);
            FleckMaker.ThrowDustPuff(pos, map, 2f);

            Effecter effecter = RimWorld.EffecterDefOf.Skip_Entry.Spawn();
            effecter.Trigger(new TargetInfo(pos, map), new TargetInfo(pos, map));
            effecter.Cleanup();

            targetPawn.Destroy();
            Find.WorldPawns.RemovePawn(targetPawn);
        }
    }
}

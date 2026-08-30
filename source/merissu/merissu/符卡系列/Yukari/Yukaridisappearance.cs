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
                    Pawn pawn = thing as Pawn;

                    if (pawn == null)
                        continue;


                    if (pawn == caster)
                        continue;


                    if (pawn.Dead || pawn.Destroyed || !pawn.Spawned)
                        continue;


                    if (!pawn.HostileTo(caster))
                        continue;


                    victims.Add(pawn);
                }
            }


            foreach (Pawn victim in victims.Distinct())
            {
                DoDisappear(victim, map);
            }
        }



        private void DoDisappear(Pawn pawn, Map map)
        {
            IntVec3 pos = pawn.Position;


            GapKillSound?.PlayOneShot(
                new TargetInfo(pos, map)
            );


            FleckMaker.ThrowDustPuff(
                pos,
                map,
                2f
            );


            pawn.TryGetComp<CompCanBeDormant>()?.WakeUp();



            Thing_GapKiller killer =
                (Thing_GapKiller)ThingMaker.MakeThing(
                    ThingDef.Named("GapKiller")
                );


            killer.isPawn = true;



            GenSpawn.Spawn(
                killer,
                pos,
                map
            );



            pawn.DeSpawn();


            killer.innerContainer.TryAdd(
                pawn
            );


            killer.CacheTexture();
        }
    }
}
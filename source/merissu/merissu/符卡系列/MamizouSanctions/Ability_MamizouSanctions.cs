using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace merissu
{
    public static class MamizouAPI
    {
        public static Dictionary<ThingDef, PawnKindDef> CustomRaceToAnimal = new Dictionary<ThingDef, PawnKindDef>();
    }
    [StaticConstructorOnStartup]
    public static class MamizouCompatibility
    {
        static MamizouCompatibility()
        {
            RegisterAnimalRace("Ratkin", "Rat");
            RegisterAnimalRace("Ratkin_Su", "Rat");

            RegisterAnimalRace("Milira_Race", "Chicken");
            RegisterAnimalRace("Milian_Race", "Chicken");

            RegisterAnimalRace("Axolotl", "Iguana");

            RegisterAnimalRace("Anty", "Larva");
            RegisterAnimalRace("Kiiro_Race", "Cat");
            RegisterAnimalRace("Wolfein_Race", "Husky");
            RegisterAnimalRace("Yuran_Race", "Snowhare");
            RegisterAnimalRace("Yuran_Race_Miko", "Snowhare");
            RegisterAnimalRace("Yuran_Race_Miko_BlackSnake", "Snowhare");
            RegisterAnimalRace("Alien_Destrier", "Horse");
            RegisterAnimalRace("Alien_Epona", "Horse");
            RegisterAnimalRace("Kurin_Race", "Fox_Fennec");
            RegisterAnimalRace("Alien_Miho", "Fox_Fennec");

        }
        private static void RegisterAnimalRace(
            string raceDefName,
            string animalPawnKindDefName)
        {
            ThingDef race =
                DefDatabase<ThingDef>.GetNamedSilentFail(raceDefName);

            if (race == null)
            {
                return;
            }

            PawnKindDef animal =
                DefDatabase<PawnKindDef>.GetNamedSilentFail(animalPawnKindDefName);

            if (animal == null)
            {
                Log.Warning(
                    "[Mamizou] Animal PawnKindDef not found: "
                    + animalPawnKindDefName);

                return;
            }

            MamizouAPI.CustomRaceToAnimal[race] = animal;

            Log.Message(
                "[Mamizou] Registered: "
                + raceDefName
                + " -> "
                + animalPawnKindDefName);
        }
    }
    public class Ability_MamizouSanctions : Ability
    {
        public Ability_MamizouSanctions() : base() { }
        public Ability_MamizouSanctions(Pawn pawn, AbilityDef def) : base(pawn, def) { }

        public override bool GizmoDisabled(out string reason)
        {
            if (base.GizmoDisabled(out reason)) return true;

            if (pawn != null && pawn.health != null && pawn.health.hediffSet != null)
            {
                Hediff powerHediff = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("FullPower"));
                if (powerHediff == null || powerHediff.Severity < 1f)
                {
                    reason = "符卡不足";
                    return true;
                }
            }

            reason = null;
            return false;
        }

        public override bool Activate(LocalTargetInfo target, LocalTargetInfo dest)
        {
            if (!target.IsValid || pawn == null || pawn.Map == null)
                return false;

            Hediff powerHediff = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("FullPower"));
            if (powerHediff == null || powerHediff.Severity < 1f)
            {
                Messages.Message("符卡不足", pawn, MessageTypeDefOf.RejectInput, false);
                return false;
            }

            powerHediff.Severity -= 1f;

            ThingDef projDef = DefDatabase<ThingDef>.GetNamed("MamizouSmokeProjectile");
            MamizouSmokeProjectile proj = (MamizouSmokeProjectile)ThingMaker.MakeThing(projDef);
            GenSpawn.Spawn(proj, pawn.Position, pawn.Map);
            proj.Launch(pawn, target.Cell);

            return true;
        }
    }
}
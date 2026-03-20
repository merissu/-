using RimWorld;
using Verse;
using UnityEngine;

namespace merissu
{
    public class Ability_StarlightTyphoon : Ability
    {
        public Ability_StarlightTyphoon() : base() { }

        public Ability_StarlightTyphoon(Pawn pawn) : base(pawn) { }
        public Ability_StarlightTyphoon(Pawn pawn, AbilityDef def) : base(pawn, def) { }

        public override bool Activate(LocalTargetInfo target, LocalTargetInfo dest)
        {
            Pawn caster = pawn;
            Map map = caster.Map;
            if (map == null) return false;

            Hediff cardStatus = caster.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("MarisaCardDeclared"));
            if (cardStatus != null)
            {
                caster.health.RemoveHediff(cardStatus);
            }

            HediffDef myStatus = HediffDef.Named("StarlightTyphoon"); 
            if (myStatus != null && !caster.health.hediffSet.HasHediff(myStatus))
            {
                caster.health.AddHediff(myStatus);
            }


            ThingDef[] orbDefs =
            {
                DefDatabase<ThingDef>.GetNamed("StarlightOrb_Red"),
                DefDatabase<ThingDef>.GetNamed("StarlightOrb_Blue"),
                DefDatabase<ThingDef>.GetNamed("StarlightOrb_Green"),
                DefDatabase<ThingDef>.GetNamed("StarlightOrb_Yellow"),
                DefDatabase<ThingDef>.GetNamed("StarlightOrb_Purple")
            };

            Material[] laserMats =
            {
                MaterialPool.MatFrom("Other/StarlightLaser_Red", ShaderDatabase.Transparent),
                MaterialPool.MatFrom("Other/StarlightLaser_Blue", ShaderDatabase.Transparent),
                MaterialPool.MatFrom("Other/StarlightLaser_Green", ShaderDatabase.Transparent),
                MaterialPool.MatFrom("Other/StarlightLaser_Yellow", ShaderDatabase.Transparent),
                MaterialPool.MatFrom("Other/StarlightLaser_Purple", ShaderDatabase.Transparent)
            };

            for (int i = 0; i < 5; i++)
            {
                Thing_StarlightOrb orb = ThingMaker.MakeThing(orbDefs[i]) as Thing_StarlightOrb;
                if (orb == null) continue;

                orb.Init(caster, i * 72f, laserMats[i], +1);
                GenSpawn.Spawn(orb, caster.Position, map);
            }

            for (int i = 0; i < 5; i++)
            {
                Thing_StarlightOrb orb = ThingMaker.MakeThing(orbDefs[i]) as Thing_StarlightOrb;
                if (orb == null) continue;

                orb.Init(caster, i * 72f + 36f, laserMats[i], -1);
                GenSpawn.Spawn(orb, caster.Position, map);
            }

            return true;
        }
    }
}
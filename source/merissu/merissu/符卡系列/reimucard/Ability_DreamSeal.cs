using RimWorld;
using Verse;

namespace merissu
{
    public class Ability_DreamSeal : Ability
    {
        public Ability_DreamSeal() : base() { }

        public Ability_DreamSeal(Pawn pawn, AbilityDef def) : base(pawn, def)
        {
        }

        private static readonly string[] OrbDefNames =
        {
            "DreamSealOrb_Red",
            "DreamSealOrb_Blue",
            "DreamSealOrb_Green"
        };

        public override bool Activate(LocalTargetInfo target, LocalTargetInfo dest)
        {
            Pawn caster = pawn;
            if (caster == null || target.Thing == null) return false;
            HediffDef hDef = HediffDef.Named("ReimuCardDeclared");
            if (hDef != null)
            {
                Hediff firstHediffOfDef = caster.health.hediffSet.GetFirstHediffOfDef(hDef);
                if (firstHediffOfDef != null)
                {
                    caster.health.RemoveHediff(firstHediffOfDef);
                }
            }

            HediffDef hDefSeal = HediffDef.Named("DreamSeal");
            if (hDefSeal != null)
            {
                caster.health.AddHediff(hDefSeal, null, null);
            }

            Map map = caster.Map;
            int orbCount = 8; 

            System.Collections.Generic.List<string> orbList = new System.Collections.Generic.List<string>();

            for (int j = 0; j < 2; j++)
            {
                orbList.Add("DreamSealOrb_Red");
                orbList.Add("DreamSealOrb_Blue");
                orbList.Add("DreamSealOrb_Green");
            }

            for (int j = 0; j < 2; j++)
            {
                orbList.Add(OrbDefNames.RandomElement());
            }

            orbList.Shuffle();

            for (int i = 0; i < orbCount; i++)
            {
                string defName = orbList[i];
                ThingDef orbDef = ThingDef.Named(defName);

                DreamSealOrb orb = (DreamSealOrb)GenSpawn.Spawn(
                    orbDef,
                    caster.Position,
                    map);

                orb.caster = caster;
                orb.target = target.Thing;

                orb.angleOffset = i * (360f / orbCount);
                orb.fireDelayTicks = i * 15;
            }

            return base.Activate(target, dest);
        }
    }
}

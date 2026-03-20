using System.Collections.Generic;
using Verse;
using RimWorld;
using UnityEngine;

namespace merissu
{
    public class CompProperties_HorrorMonitor : CompProperties
    {
        public CompProperties_HorrorMonitor() { this.compClass = typeof(CompHorrorMonitor); }
    }

    public class CompHorrorMonitor : ThingComp
    {
        private static readonly string MonitorHediffDefName = "Hediff_InanimateDream";

        public override void CompTick()
        {
            base.CompTick();
            if (parent.IsHashIntervalTick(60))
            {
                CheckHediffAndRevert();
            }
        }

        private void CheckHediffAndRevert()
        {
            Pawn pawn = (parent.ParentHolder as Pawn_EquipmentTracker)?.pawn;

            if (pawn == null || pawn.Dead || !pawn.health.hediffSet.HasHediff(HediffDef.Named(MonitorHediffDefName)))
            {
                RevertToNormal(pawn);
            }
        }

        private void RevertToNormal(Pawn pawn)
        {
            if (pawn != null)
            {
                HorrorUtility.ReplaceWeapon(pawn, "YuukaUmbrella");
                Messages.Message("魔炮形态持续时间结束。", MessageTypeDefOf.NeutralEvent);
            }
            else
            {
                Map map = parent.Map;
                IntVec3 pos = parent.Position;
                if (map != null)
                {
                    ThingDef normalDef = ThingDef.Named("YuukaUmbrella");
                    Thing newW = ThingMaker.MakeThing(normalDef, parent.Stuff);
                    GenSpawn.Spawn(newW, pos, map);
                    parent.Destroy();
                }
            }
        }
    }

    public class Ability_InanimateDream : Ability
    {
        private static readonly HediffDef FullPowerDef = HediffDef.Named("FullPower");
        private static readonly HediffDef FormHediffDef = HediffDef.Named("Hediff_InanimateDream");

        public Ability_InanimateDream() : base() { }
        public Ability_InanimateDream(Pawn pawn, AbilityDef def) : base(pawn, def) { }

        public override AcceptanceReport CanCast
        {
            get
            {
                Hediff fp = pawn.health.hediffSet.GetFirstHediffOfDef(FullPowerDef);
                if (fp == null || fp.Severity < 5f) return "灵力不足（需要5层）";

                if (pawn.equipment?.Primary?.def.defName != "YuukaUmbrella") return "必须装备收起状态的阳伞";

                return base.CanCast;
            }
        }

        public override bool Activate(LocalTargetInfo target, LocalTargetInfo dest)
        {
            Hediff fp = pawn.health.hediffSet.GetFirstHediffOfDef(FullPowerDef);
            if (fp != null && fp.Severity >= 5f)
            {
                fp.Severity -= 5f;

                pawn.health.AddHediff(FormHediffDef);

                HorrorUtility.ReplaceWeapon(pawn, "horrorYuukaUmbrella");

                Messages.Message("幽梦　～ Inanimate Dream", MessageTypeDefOf.PositiveEvent);
                return base.Activate(target, dest);
            }
            return false;
        }
    }

    public static class HorrorUtility
    {
        public static void ReplaceWeapon(Pawn pawn, string targetDefName)
        {
            ThingWithComps oldW = pawn.equipment.Primary;
            if (oldW == null) return;

            ThingDef newDef = ThingDef.Named(targetDefName);
            ThingWithComps newW = (ThingWithComps)ThingMaker.MakeThing(newDef, oldW.Stuff);

            newW.HitPoints = oldW.HitPoints;

            CompQuality oldQ = oldW.TryGetComp<CompQuality>();
            CompQuality newQ = newW.TryGetComp<CompQuality>();
            if (oldQ != null && newQ != null)
            {
                newQ.SetQuality(oldQ.Quality, ArtGenerationContext.Colony);
            }

            CompBiocodable oldB = oldW.TryGetComp<CompBiocodable>();
            CompBiocodable newB = newW.TryGetComp<CompBiocodable>();
            if (oldB != null && oldB.Biocoded && newB != null)
            {
                newB.CodeFor(oldB.CodedPawn);
            }

            pawn.equipment.Remove(oldW);
            if (!oldW.Destroyed) oldW.Destroy();
            pawn.equipment.AddEquipment(newW);
            pawn.Drawer.renderer.SetAllGraphicsDirty();
        }
    }
}
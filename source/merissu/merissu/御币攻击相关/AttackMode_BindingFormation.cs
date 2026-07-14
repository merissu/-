using UnityEngine;
using Verse;
using Verse.Sound;

namespace merissu
{
    public class AttackMode_BindingFormation : GoheiAttackMode
    {
        public override string ModeName => "BindingFormation";
        protected override string ProjectileDefName => "REIMU_SpinningTalisman";
        protected override string SoundDefName => "VigilanceFormation";
        public override int BurstCount => 1;
        public override int TicksBetweenShots => 0;
        public override float WarmupTime => 1f;

        public override void OnWarmupStart(Verb_GoheiRandomShoot verb, LocalTargetInfo target)
        {
            base.OnWarmupStart(verb, target);
            Pawn caster = verb.CasterPawn;
            Map map = caster.Map;
            if (map == null) return;

            SoundDef.Named(SoundDefName)?.PlayOneShot(new TargetInfo(caster.Position, map));

            ThingDef controllerDef = ThingDef.Named("BindingFormationController");
            Thing_BindingFormationController ctrl = (Thing_BindingFormationController)ThingMaker.MakeThing(controllerDef);
            ctrl.verb = verb;
            ctrl.caster = caster;
            ctrl.targetThing = target.Thing;
            ctrl.startTick = Find.TickManager.TicksGame;
            ctrl.warmupTicks = Mathf.RoundToInt(WarmupTime * 60f);
            GenSpawn.Spawn(ctrl, caster.Position, map);
        }

        public override bool OverrideCastShot(Verb_GoheiRandomShoot verb, LocalTargetInfo target)
        {
            return true;
        }
    }
}
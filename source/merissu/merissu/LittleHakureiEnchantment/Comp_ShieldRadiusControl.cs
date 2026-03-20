using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace merissu
{
    public class Comp_ShieldRadiusControl : ThingComp
    {
        public CompProperties_ShieldRadiusControl Props =>
            (CompProperties_ShieldRadiusControl)props;

        private CompProjectileInterceptor interceptor;
        private CompRefuelable refuelable;

        private bool allowGround;
        private bool allowAir;

        private bool revealStealthEnabled = false;

        private bool touhouDieEnabled = false;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);

            interceptor = parent.GetComp<CompProjectileInterceptor>();
            refuelable = parent.GetComp<CompRefuelable>();

            if (interceptor != null)
            {
                allowGround = interceptor.Props.interceptGroundProjectiles;
                allowAir = interceptor.Props.interceptAirProjectiles;
            }

            UpdateFuelConsumption();
            UpdateInterceptorState();
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref revealStealthEnabled, "revealStealthEnabled", false);
            Scribe_Values.Look(ref touhouDieEnabled, "touhouDieEnabled", false);
        }

        public override void CompTick()
        {
            base.CompTick();

            UpdateInterceptorState();

            if (revealStealthEnabled && refuelable != null && refuelable.HasFuel)
            {
                RevealStealthedEnemies();
            }

            if (touhouDieEnabled && refuelable != null && refuelable.HasFuel)
            {
                ApplyTouhouDie();
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (interceptor == null)
                yield break;

            float radius = interceptor.Props.radius;

            yield return new Command_Action
            {
                defaultLabel = "缩小结界范围",
                defaultDesc = $"当前半径：{radius:F0}\n每日灵力消耗：{radius:F0}",
                icon = ContentFinder<Texture2D>.Get("Other/onmyoBall"),
                action = () => ChangeRadius(-Props.step)
            };

            yield return new Command_Action
            {
                defaultLabel = "扩大结界范围",
                defaultDesc = $"当前半径：{radius:F0}\n每日灵力消耗：{radius:F0}",
                icon = ContentFinder<Texture2D>.Get("Other/onmyoBall"),
                action = () => ChangeRadius(+Props.step)
            };

            yield return new Command_Toggle
            {
                defaultLabel = "显形结界",
                defaultDesc = "使结界范围内所有正在隐身的敌对生物现身\n每日额外消耗：10 灵力",
                icon = ContentFinder<Texture2D>.Get("Other/onmyoBall"),
                isActive = () => revealStealthEnabled,
                toggleAction = () =>
                {
                    revealStealthEnabled = !revealStealthEnabled;
                    UpdateFuelConsumption();
                }
            };

            yield return new Command_Toggle
            {
                defaultLabel = "必灭结界",
                defaultDesc = "使结界范围内所有敌对单位获得幻想入\n每日额外消耗：10 灵力",
                icon = ContentFinder<Texture2D>.Get("Other/onmyoBall"),
                isActive = () => touhouDieEnabled,
                toggleAction = () =>
                {
                    touhouDieEnabled = !touhouDieEnabled;
                    UpdateFuelConsumption();
                }
            };
        }

        private void ChangeRadius(float delta)
        {
            if (interceptor == null)
                return;

            float current = interceptor.Props.radius;
            float newRadius = Mathf.Clamp(
                current + delta,
                Props.minRadius,
                Props.maxRadius
            );

            if (Mathf.Approximately(newRadius, current))
                return;

            interceptor.Props.radius = newRadius;

            UpdateFuelConsumption();
            UpdateInterceptorState();
        }

        private void UpdateFuelConsumption()
        {
            if (refuelable == null || interceptor == null)
                return;

            float consumption = interceptor.Props.radius;

            if (revealStealthEnabled)
                consumption += 10f;

            if (touhouDieEnabled)
                consumption += 10f;

            refuelable.Props.fuelConsumptionRate = consumption;
        }

        private void UpdateInterceptorState()
        {
            if (interceptor == null || refuelable == null)
                return;

            if (refuelable.HasFuel)
            {
                interceptor.Props.interceptGroundProjectiles = allowGround;
                interceptor.Props.interceptAirProjectiles = allowAir;
            }
            else
            {
                interceptor.Props.interceptGroundProjectiles = false;
                interceptor.Props.interceptAirProjectiles = false;
            }
        }

        private void RevealStealthedEnemies()
        {
            if (parent.Map == null)
                return;

            float radius = interceptor.Props.radius;
            IntVec3 center = parent.Position;

            foreach (Pawn pawn in parent.Map.mapPawns.AllPawnsSpawned)
            {
                if (pawn.Dead || pawn.Faction == null)
                    continue;

                if (!pawn.HostileTo(parent.Faction))
                    continue;

                if (!pawn.Position.InHorDistOf(center, radius))
                    continue;

                pawn.health.hediffSet.hediffs.RemoveAll(h =>
                    h.def.defName.Contains("Invisible") ||
                    h.def.defName.Contains("Invisibility") ||
                    h.def.defName.Contains("Stealth")
                );
            }
        }

        private void ApplyTouhouDie()
        {
            if (parent.Map == null)
                return;

            float radius = interceptor.Props.radius;
            IntVec3 center = parent.Position;

            HediffDef dieDef = DefDatabase<HediffDef>.GetNamedSilentFail("touhoudie");
            if (dieDef == null)
                return;

            foreach (Pawn pawn in parent.Map.mapPawns.AllPawnsSpawned)
            {
                if (pawn.Dead || pawn.Faction == null)
                    continue;

                if (!pawn.HostileTo(parent.Faction))
                    continue;

                if (!pawn.Position.InHorDistOf(center, radius))
                    continue;

                if (!pawn.health.hediffSet.HasHediff(dieDef))
                {
                    pawn.health.AddHediff(dieDef);
                }
            }
        }
    }
}

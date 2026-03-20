using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace merissu
{
    public class fourfoldbarrier : Ability
    {
        private const int MaxTargets = 20;
        private const int PullRange = 9999;
        private const int TeleportRadius = 20;
        private const int BarrierDuration = 180;

        private const float PowerCost = 1.0f;
        private static readonly string PowerHediffName = "FullPower";
        public fourfoldbarrier() : base() { }

        public fourfoldbarrier(Pawn pawn, AbilityDef def) : base(pawn, def) { }

        public override AcceptanceReport CanCast
        {
            get
            {
                AcceptanceReport baseReport = base.CanCast;
                if (!baseReport.Accepted) return baseReport;

                Hediff hp = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named(PowerHediffName));
                if (hp == null || hp.Severity < PowerCost)
                {
                    return "灵力不足"; 
                }

                return AcceptanceReport.WasAccepted;
            }
        }

        public override bool Activate(LocalTargetInfo target, LocalTargetInfo dest)
        {
            if (pawn.Map == null)
                return false;

            Hediff hp = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named(PowerHediffName));
            if (hp != null)
            {
                hp.Severity -= PowerCost;
            }
            else
            {
                return false;
            }

            Map map = pawn.Map;

            SoundDef.Named("fourfoldbarrier")?.PlayOneShot(new TargetInfo(pawn.Position, map));

            SpawnBarrierVisual(map);

            map.GetComponent<MapComponent_FourfoldBarrier>().StartBarrier(pawn, BarrierDuration);

            List<Pawn> enemies = map.mapPawns.AllPawnsSpawned
                .Where(p => p != pawn && !p.Dead && p.HostileTo(pawn))
                .OrderBy(p => p.Position.DistanceTo(pawn.Position))
                .Take(MaxTargets)
                .ToList();

            foreach (Pawn enemy in enemies)
            {
                if (TryFindTeleportCellNearCaster(enemy, map, out IntVec3 cell))
                    DoTeleport(enemy, cell);
            }

            return true;
        }
        private void SpawnBarrierVisual(Map map)
        {
            Thing barrier = ThingMaker.MakeThing(
                ThingDef.Named("FourfoldBarrierVisual"));

            GenSpawn.Spawn(barrier, pawn.Position, map);

            if (barrier is Thing_FourfoldBarrierVisual v)
            {
                v.Init(pawn, BarrierDuration);
            }
        }

        private bool TryFindTeleportCellNearCaster(Pawn enemy, Map map, out IntVec3 result)
        {
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(
                         pawn.Position,
                         TeleportRadius,
                         true))
            {
                if (!cell.InBounds(map))
                    continue;

                if (!cell.Standable(map))
                    continue;

                if (cell.GetFirstPawn(map) != null)
                    continue;

                result = cell;
                return true;
            }

            result = IntVec3.Invalid;
            return false;
        }

        private void DoTeleport(Pawn targetPawn, IntVec3 destination)
        {
            Map map = pawn.Map;

            this.AddEffecterToMaintain(
                RimWorld.EffecterDefOf.Skip_Entry.Spawn(targetPawn, map),
                targetPawn.Position,
                60);

            this.AddEffecterToMaintain(
                RimWorld.EffecterDefOf.Skip_Exit.Spawn(destination, map),
                destination,
                60);

            targetPawn.Position = destination;

            targetPawn.stances.stunner.StunFor(
                60,
                pawn,
                addBattleLog: false,
                showMote: false);

            targetPawn.Notify_Teleported();
        }
    }

    public class MapComponent_FourfoldBarrier : MapComponent
    {
        private Pawn caster;
        private int ticksLeft;
        private int damageTicker;

        private const int DamageInterval = 10;
        private const int DamageAmount = 40;
        private const float DamageRadius = 20f;

        public MapComponent_FourfoldBarrier(Map map) : base(map)
        {
        }

        public void StartBarrier(Pawn pawn, int duration)
        {
            caster = pawn;
            ticksLeft = duration;
            damageTicker = 0;

            HediffDef def = HediffDef.Named("fourfoldbarrier");
            if (def != null && caster.health != null)
            {
                Hediff hediff = HediffMaker.MakeHediff(def, caster);
                caster.health.AddHediff(hediff);
            }
        }

        public override void MapComponentTick()
        {
            if (caster == null || caster.DestroyedOrNull())
                return;

            if (ticksLeft <= 0)
            {
                caster = null;
                return;
            }

            ticksLeft--;
            damageTicker++;

            caster.pather?.StopDead();

            if (damageTicker >= DamageInterval)
            {
                damageTicker = 0;
                DoAOEDamage();
            }
        }

        private void DoAOEDamage()
        {
            List<Pawn> targets = map.mapPawns.AllPawnsSpawned.ToList();

            foreach (Pawn p in targets)
            {
                if (p == caster) continue;
                if (p.Dead) continue;
                if (!p.HostileTo(caster)) continue;

                if (p.Position.DistanceTo(caster.Position) <= DamageRadius)
                {
                    DamageInfo dinfo = new DamageInfo(
                        DamageDefOf.Cut,
                        DamageAmount,
                        0,
                        -1,
                        caster);

                    p.TakeDamage(dinfo);
                }
            }
        }
    }
    [StaticConstructorOnStartup]
    public class Thing_FourfoldBarrierVisual : Thing
    {
        private Pawn caster;
        private int age;
        private int duration;

        private float rotation;

        private const float RotateSpeed = 8f;      
        private const float DrawScale = 40f;       

        private const float OuterInitialScaleOffset = 8f;
        private const int OuterSpawnInterval = 6;
        private const int OuterLife = 40;
        private const float OuterExpandSpeed = 0.8f;

        private int outerSpawnTicker;

        private static readonly MaterialPropertyBlock propBlock =
            new MaterialPropertyBlock();

        private Material mainMat;
        private Material outerMat;

        private List<OuterRing> outerRings = new List<OuterRing>();

        private class OuterRing
        {
            public float rotation;
            public float scale;
            public int age;
        }

        public void Init(Pawn pawn, int durationTicks)
        {
            caster = pawn;
            duration = durationTicks;
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);

            mainMat = MaterialPool.MatFrom(
                "Other/spellBulletDa001",
                ShaderDatabase.MoteGlow);

            outerMat = MaterialPool.MatFrom(
                "Other/spellBulletDb001",
                ShaderDatabase.MoteGlow);
        }

        protected override void Tick()
        {
            base.Tick();

            if (caster == null || caster.DestroyedOrNull())
            {
                Destroy();
                return;
            }

            age++;
            rotation += RotateSpeed;

            if (age < duration)
            {
                outerSpawnTicker++;
                if (outerSpawnTicker >= OuterSpawnInterval)
                {
                    outerSpawnTicker = 0;

                    outerRings.Add(new OuterRing
                    {
                        rotation = rotation,   
                        scale = DrawScale + OuterInitialScaleOffset,
                        age = 0
                    });
                }
            }

            for (int i = outerRings.Count - 1; i >= 0; i--)
            {
                OuterRing ring = outerRings[i];

                ring.age++;
                ring.scale += OuterExpandSpeed;


                if (ring.age >= OuterLife)
                {
                    outerRings.RemoveAt(i);
                }
            }

            if (age >= duration && outerRings.Count == 0)
                Destroy();
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            if (caster == null) return;

            Vector3 pos = caster.DrawPos;
            pos.y = AltitudeLayer.MoteOverhead.AltitudeFor();

            float t = (float)age / duration;

            float mainAlpha = 1f;
            if (t > 0.7f)
            {
                mainAlpha = Mathf.Lerp(1f, 0f, (t - 0.7f) / 0.3f);
            }

            foreach (OuterRing ring in outerRings)
            {
                float outerAlpha = 1f - (float)ring.age / OuterLife;

                propBlock.SetColor(
                    ShaderPropertyIDs.Color,
                    new Color(1f, 1f, 1f, outerAlpha * 0.8f));

                Matrix4x4 outerMatrix = Matrix4x4.TRS(
                    pos,
                    Quaternion.Euler(0f, ring.rotation, 0f),
                    new Vector3(ring.scale, 1f, ring.scale));

                Graphics.DrawMesh(
                    MeshPool.plane10,
                    outerMatrix,
                    outerMat,
                    0,
                    null,
                    0,
                    propBlock);
            }

            propBlock.SetColor(
                ShaderPropertyIDs.Color,
                new Color(1f, 1f, 1f, mainAlpha));

            Matrix4x4 mainMatrix = Matrix4x4.TRS(
                pos,
                Quaternion.Euler(0f, rotation, 0f),
                new Vector3(DrawScale, 1f, DrawScale));

            Graphics.DrawMesh(
                MeshPool.plane10,
                mainMatrix,
                mainMat,
                0,
                null,
                0,
                propBlock);
        }
    }
}

using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using System.Collections.Generic;

namespace merissu
{
    public class Ability_FixedStar : Ability
    {
        public Ability_FixedStar() : base() { }

        public Ability_FixedStar(Pawn pawn, AbilityDef def) : base(pawn, def) { }

        public override AcceptanceReport CanCast
        {
            get
            {
                Hediff hp = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("FullPower"));

                if (hp == null || hp.Severity < 1f)
                {
                    return "灵力不足 (需要1层)"; 
                }

                return AcceptanceReport.WasAccepted;
            }
        }

        public override bool Activate(LocalTargetInfo target, LocalTargetInfo dest)
        {
            if (pawn == null || pawn.Map == null)
                return false;

            Hediff hp = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("FullPower"));
            if (hp == null || hp.Severity < 1f)
            {
                return false; 
            }
            hp.Severity -= 1f; 

            Thing thing = ThingMaker.MakeThing(ThingDef.Named("Thing_FixedStarVisual"));
            if (thing is Thing_FixedStarVisual visual)
            {
                visual.caster = pawn;
                visual.startTick = Find.TickManager.TicksGame;
                GenSpawn.Spawn(visual, pawn.Position, pawn.Map);

                SoundDef.Named("FixedStar").PlayOneShot(new TargetInfo(pawn.Position, pawn.Map));
            }

            return true;
        }
    }
    [StaticConstructorOnStartup]
    public class Thing_FixedStarVisual : Thing
    {
        public Pawn caster;
        public int startTick;

        private const int LifeTimeTicks = 300;

        private const float LowerBaseSize = 30f;
        private const float UpperBaseSize = 6f;

        private const int StarCount1 = 5;
        private const float StarRadius1 = 4f;
        private const float StarBaseSize1 = 6f;

        private const int StarCount2 = 5;
        private const float StarRadius2 = 9f;
        private const float StarBaseSize2 = 7f;

        private const int DamageInterval = 1; 
        private const float PawnDamage = 50f;
        private const float ThingDamage = 25f;

        private float starOrbitRot1;
        private float starOrbitRot2;
        private float starOrbitSpeed1;
        private float starOrbitSpeed2;

        private float rotLower;
        private float rotUpper;

        private static readonly Material LowerMat =
            MaterialPool.MatFrom("Other/FixedStar/bulletFd004", ShaderDatabase.MoteGlow);

        private static readonly Material UpperMat =
            MaterialPool.MatFrom("Other/FixedStar/bulletFd001", ShaderDatabase.MoteGlow);

        private static readonly Material[] StarMats =
        {
            MaterialPool.MatFrom("Other/FixedStar/bulletFd000", ShaderDatabase.MoteGlow),
            MaterialPool.MatFrom("Other/FixedStar/bulletFd002", ShaderDatabase.MoteGlow),
            MaterialPool.MatFrom("Other/FixedStar/bulletFd003", ShaderDatabase.MoteGlow),
            MaterialPool.MatFrom("Other/FixedStar/bulletFd005", ShaderDatabase.MoteGlow)
        };

        protected override void Tick()
        {
            base.Tick();

            if (caster == null || !caster.Spawned || caster.Dead)
            {
                Destroy();
                return;
            }

            int age = Find.TickManager.TicksGame - startTick;
            if (age >= LifeTimeTicks)
            {
                Destroy();
                return;
            }

            Position = caster.Position;

            rotLower -= 3f;
            rotUpper += 12f;

            const float AngularAccel = 0.1f;
            const float MaxSpeed = 8f;

            starOrbitSpeed1 = Mathf.Min(starOrbitSpeed1 + AngularAccel, MaxSpeed);
            starOrbitRot1 += starOrbitSpeed1;

            starOrbitSpeed2 = Mathf.Min(starOrbitSpeed2 + AngularAccel, MaxSpeed);
            starOrbitRot2 -= starOrbitSpeed2;

            if (Find.TickManager.TicksGame % DamageInterval == 0)
            {
                ApplyStarDamage(
                    starOrbitRot1,
                    StarRadius1,
                    StarCount1,
                    StarBaseSize1
                );

                ApplyStarDamage(
                    starOrbitRot2,
                    StarRadius2,
                    StarCount2,
                    StarBaseSize2
                );
            }
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            if (caster == null)
                return;

            int age = Find.TickManager.TicksGame - startTick;
            float progress = age / (float)LifeTimeTicks;

            float scale = 1f;
            float alpha = 1f;

            if (progress > 0.8f)
            {
                float t = (progress - 0.8f) / 0.2f;
                scale = 1f - t;
                alpha = 1f - t;
            }

            Vector3 center = caster.DrawPos;

            DrawRing(center,
                AltitudeLayer.Pawn.AltitudeFor() - 0.05f,
                rotLower,
                LowerBaseSize * scale,
                alpha,
                LowerMat);

            DrawRing(center,
                AltitudeLayer.MetaOverlays.AltitudeFor() + 0.05f,
                rotUpper,
                UpperBaseSize * scale,
                alpha,
                UpperMat);

            DrawOrbitStars(center, scale, alpha,
                starOrbitRot1,
                StarRadius1,
                StarCount1,
                StarBaseSize1);

            DrawOrbitStars(center, scale, alpha,
                starOrbitRot2,
                StarRadius2,
                StarCount2,
                StarBaseSize2);
        }

        private void DrawOrbitStars(
            Vector3 center,
            float scale,
            float alpha,
            float orbitRot,
            float radius,
            int count,
            float starSize)
        {
            float step = 360f / count;

            for (int i = 0; i < count; i++)
            {
                float angle = step * i + orbitRot;
                float rad = angle * Mathf.Deg2Rad;

                Vector3 pos = center;
                pos.x += Mathf.Cos(rad) * radius;
                pos.z += Mathf.Sin(rad) * radius;
                pos.y = AltitudeLayer.MetaOverlays.AltitudeFor() + 0.1f;

                foreach (Material mat in StarMats)
                {
                    DrawStar(pos, angle, starSize * scale, alpha, mat);
                }
            }
        }

        private void ApplyStarDamage(
            float orbitRot,
            float radius,
            int count,
            float starVisualSize)
        {
            float step = 360f / count;

            float damageRadius = Mathf.Max(0.5f, starVisualSize * 0.18f);

            for (int i = 0; i < count; i++)
            {
                float angle = step * i + orbitRot;
                float rad = angle * Mathf.Deg2Rad;

                Vector3 pos = caster.DrawPos;
                pos.x += Mathf.Cos(rad) * radius;
                pos.z += Mathf.Sin(rad) * radius;

                IntVec3 centerCell = pos.ToIntVec3();

                foreach (IntVec3 cell in GenRadial.RadialCellsAround(
                    centerCell,
                    damageRadius,
                    true))
                {
                    if (!cell.InBounds(caster.Map))
                        continue;

                    List<Thing> things = cell.GetThingList(caster.Map);
                    for (int t = things.Count - 1; t >= 0; t--)
                    {
                        Thing thing = things[t];

                        if (thing == caster)
                            continue;

                        if (thing is Pawn pawn)
                        {
                            if (!pawn.Dead && !pawn.Downed)
                            {
                                DamageInfo dinfo = new DamageInfo(
                                    DamageDefOf.Flame,
                                    PawnDamage,
                                    0.8f,
                                    -1f,
                                    caster);
                                pawn.TakeDamage(dinfo);
                            }
                        }
                        else if (thing.FlammableNow)
                        {
                            thing.Destroy();
                            GenSpawn.Spawn(ThingDefOf.Filth_Ash, cell, caster.Map);
                        }
                        else
                        {
                            DamageInfo dinfo = new DamageInfo(
                                DamageDefOf.Flame,
                                ThingDamage,
                                instigator: caster);
                            thing.TakeDamage(dinfo);
                        }
                    }
                }
            }
        }

        private void DrawStar(Vector3 pos, float rotation, float scale, float alpha, Material mat)
        {
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            block.SetColor(ShaderPropertyIDs.Color, new Color(1f, 1f, 1f, alpha));

            Matrix4x4 matrix = Matrix4x4.TRS(
                pos,
                Quaternion.Euler(0f, rotation, 0f),
                new Vector3(scale, 1f, scale));

            Graphics.DrawMesh(MeshPool.plane10, matrix, mat, 0, null, 0, block);
        }

        private void DrawRing(
            Vector3 center,
            float altitude,
            float rotation,
            float scale,
            float alpha,
            Material mat)
        {
            Vector3 pos = center;
            pos.y = altitude;

            MaterialPropertyBlock block = new MaterialPropertyBlock();
            block.SetColor(ShaderPropertyIDs.Color, new Color(1f, 1f, 1f, alpha));

            Matrix4x4 matrix = Matrix4x4.TRS(
                pos,
                Quaternion.Euler(0f, rotation, 0f),
                new Vector3(scale, 1f, scale));

            Graphics.DrawMesh(MeshPool.plane10, matrix, mat, 0, null, 0, block);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref caster, "caster");
            Scribe_Values.Look(ref startTick, "startTick");
            Scribe_Values.Look(ref rotLower, "rotLower");
            Scribe_Values.Look(ref rotUpper, "rotUpper");
            Scribe_Values.Look(ref starOrbitRot1, "starOrbitRot1");
            Scribe_Values.Look(ref starOrbitRot2, "starOrbitRot2");
            Scribe_Values.Look(ref starOrbitSpeed1, "starOrbitSpeed1");
            Scribe_Values.Look(ref starOrbitSpeed2, "starOrbitSpeed2");
        }
    }
}

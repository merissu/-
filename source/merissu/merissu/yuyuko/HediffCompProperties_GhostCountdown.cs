using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace merissu
{
    public class HediffCompProperties_GhostCountdown : HediffCompProperties
    {
        public HediffCompProperties_GhostCountdown()
        {
            compClass = typeof(HediffComp_GhostCountdown);
        }
    }

    public class HediffComp_GhostCountdown : HediffComp
    {
        private Thing_GhostOrbitController controller;
        private bool controllerSpawned;

        public override void CompPostMake()
        {
            base.CompPostMake();
            TrySpawnController();
        }

        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            TrySpawnController();
        }

        private void TrySpawnController()
        {
            if (controllerSpawned) return;

            Pawn pawn = parent.pawn;
            if (pawn == null || pawn.Map == null) return;
            SoundDef.Named("gotodie").PlayOneShot(new TargetInfo(pawn.Position, pawn.Map));
            controller = (Thing_GhostOrbitController)ThingMaker.MakeThing(
                ThingDef.Named("GhostOrbitController"));
            controller.pawn = pawn;

            GenSpawn.Spawn(controller, pawn.Position, pawn.Map);
            controllerSpawned = true;
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            Pawn pawn = parent.pawn;
            if (pawn == null) return;

            if (pawn.Dead)
            {
                pawn.health.RemoveHediff(parent);
                controller?.Destroy();
                return;
            }

            if (pawn.Map == null)
            {
                controller?.Destroy();
                return;
            }

            if (!pawn.health.hediffSet.HasHediff(parent.def))
            {
                controller?.Destroy();
            }
        }
    }

    public class Thing_GhostOrbitController : Thing
    {
        public Pawn pawn;

        private const int GhostCount = 7;
        private const int ExplodeInterval = 45;

        private int tick;
        private float baseAngle;
        private List<Mote_GhostAnimated> ghosts = new List<Mote_GhostAnimated>();

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);

            for (int i = 0; i < GhostCount; i++)
            {
                Mote_GhostAnimated g = (Mote_GhostAnimated)ThingMaker.MakeThing(
                    ThingDef.Named("Mote_GhostAnimated"));
                g.controller = this;
                g.angleOffset = i * (360f / GhostCount);
                ghosts.Add(g);
                GenSpawn.Spawn(g, pawn.Position, map);
            }
        }
        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
        }
        protected override void Tick()
        {
            if (pawn == null || pawn.Dead || pawn.Map == null)
            {
                Destroy();
                return;
            }

            tick++;
            baseAngle += 1.2f;

            if (tick % ExplodeInterval == 0 && ghosts.Count > 0)
            {
                int idx = Rand.Range(0, ghosts.Count);
                ghosts[idx].Explode();
                ghosts.RemoveAt(idx);

                if (ghosts.Count == 0)
                {
                    pawn.Kill(null);
                    Destroy();
                }
            }
        }
        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            for (int i = ghosts.Count - 1; i >= 0; i--)
            {
                if (!ghosts[i].Destroyed)
                    ghosts[i].Destroy();
            }
            ghosts.Clear();

            base.Destroy(mode);
        }

        public Vector3 GetGhostPos(float offset)
        {
            float angle = (baseAngle + offset) * Mathf.Deg2Rad;
            Vector3 center = pawn.DrawPos;
            center.y = AltitudeLayer.MoteOverhead.AltitudeFor();

            return center + new Vector3(
                Mathf.Cos(angle) * 1.8f,
                0,
                Mathf.Sin(angle) * 1.8f
            );
        }
    }
    [StaticConstructorOnStartup]
    public class Mote_GhostAnimated : Thing
    {
        public Thing_GhostOrbitController controller;
        public float angleOffset;

        private int age;
        private int frame;

        private static readonly MaterialPropertyBlock pb = new MaterialPropertyBlock();
        private const int FrameCount = 21;
        private const int FrameInterval = 3;

        protected override void Tick()
        {
            age++;
            if (age % FrameInterval == 0)
                frame = (frame + 1) % FrameCount;
        }

        public void Explode()
        {
            Vector3 pos = controller.GetGhostPos(angleOffset);

            for (int i = 0; i < 12; i++)
            {
                Mote_GhostPetal p = (Mote_GhostPetal)ThingMaker.MakeThing(
                    ThingDef.Named("Mote_GhostPetal"));
                p.Initialize(pos);
                GenSpawn.Spawn(p, pos.ToIntVec3(), Map);
            }

            Destroy();
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            if (controller == null) return;

            Vector3 pos = controller.GetGhostPos(angleOffset);
            string tex = $"Other/ghost/{473 + frame}";
            Material mat = MaterialPool.MatFrom(tex, ShaderDatabase.MoteGlow);

            Matrix4x4 matrix = Matrix4x4.TRS(
                pos,
                Quaternion.identity,
                new Vector3(1f, 1f, 2f)
            );

            Graphics.DrawMesh(MeshPool.plane10, matrix, mat, 0);
        }
    }
    [StaticConstructorOnStartup]
    public class Mote_GhostPetal : Thing
    {
        private int age;
        private Vector3 pos;
        private Vector3 vel;
        private float rot;
        private float rotVel;
        private float size;

        private static readonly MaterialPropertyBlock pb = new MaterialPropertyBlock();

        public void Initialize(Vector3 start)
        {
            pos = start;
            size = Rand.Range(0.6f, 1.4f);

            float angle = Rand.Range(0, 360f) * Mathf.Deg2Rad;
            float speed = Rand.Range(0.08f, 0.18f);

            vel = new Vector3(
                Mathf.Cos(angle) * speed,
                0,
                Mathf.Sin(angle) * speed
            );

            vel.z += Rand.Range(0.12f, 0.2f);
            rot = Rand.Range(0, 360f);
            rotVel = Rand.Range(-6f, 6f);
        }

        protected override void Tick()
        {
            age++;
            pos += vel;
            vel.z -= 0.002f;
            rot += rotVel;
            rotVel *= 0.98f;
            vel *= 0.85f;


            if (age >= 90) Destroy();
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            float alpha = 1f - age / 90f;
            pb.SetColor("_Color", new Color(1, 1, 1, alpha));

            Vector3 drawPos = pos;
            drawPos.y = AltitudeLayer.MoteOverhead.AltitudeFor();

            Matrix4x4 matrix = Matrix4x4.TRS(
                drawPos,
                Quaternion.AngleAxis(rot, Vector3.up),
                new Vector3(size, 1f, size)
            );

            Graphics.DrawMesh(
                MeshPool.plane10,
                matrix,
                MaterialPool.MatFrom("Other/spellBulletDa000", ShaderDatabase.MoteGlow),
                0,
                null,
                0,
                pb
            );
        }
    }
}

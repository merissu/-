using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;

namespace merissu
{
    public class InanimateLaser : Thing
    {
        public Vector3 direction = Vector3.forward;
        public Pawn caster;
        private int age;

        private const int TicksPerFrame = 2;
        private const int TotalFrames = 25;

        private const float BaseOffsetDistance = 8f;
        private const float HeadSize = 15f;
        private const float BeamLength = 250f;
        private const float GapFiller = 0.004f;
        private const float DamagePerHit = 50f;
        private const float FireChance = 0.5f;
        private const int DamageIntervalTicks = 4;

        private const int TotalDuration = 300;
        private const int FadeOutTicks = 60;

        private static Mesh _headMesh = null;
        public static Mesh HeadMesh
        {
            get
            {
                if (_headMesh == null)
                {
                    _headMesh = new Mesh { name = "MasterSpark_HeadMesh" };
                    _headMesh.vertices = new Vector3[] { new Vector3(-0.5f, 0, -0.5f), new Vector3(0.5f, 0, -0.5f), new Vector3(-0.5f, 0, 0.5f), new Vector3(0.5f, 0, 0.5f) };
                    _headMesh.uv = new Vector2[] { new Vector2(0.01f, 0.01f), new Vector2(0.99f, 0.01f), new Vector2(0.01f, 0.99f), new Vector2(0.99f, 0.99f) };
                    _headMesh.triangles = new int[] { 0, 2, 1, 1, 2, 3 };
                    _headMesh.RecalculateNormals();
                }
                return _headMesh;
            }
        }

        private static Mesh _lineMesh = null;
        public static Mesh LineMesh
        {
            get
            {
                if (_lineMesh == null)
                {
                    _lineMesh = new Mesh { name = "MasterSpark_LineMesh" };
                    _lineMesh.vertices = new Vector3[] { new Vector3(-0.5f, 0, -0.5f), new Vector3(0.5f, 0, -0.5f), new Vector3(-0.5f, 0, 0.5f), new Vector3(0.5f, 0, 0.5f) };
                    _lineMesh.uv = new Vector2[] { new Vector2(0.98f, 0.01f), new Vector2(0.98f, 0.01f), new Vector2(0.98f, 0.99f), new Vector2(0.98f, 0.99f) };
                    _lineMesh.triangles = new int[] { 0, 2, 1, 1, 2, 3 };
                    _lineMesh.RecalculateNormals();
                }
                return _lineMesh;
            }
        }

        protected override void Tick()
        {
            base.Tick();
            age++;

            if (caster == null || !caster.Spawned || caster.Dead || caster.Downed)
            {
                this.Destroy();
                return;
            }

            this.Position = caster.Position;
            caster.pather.StopDead();
            float shotAngle = direction.AngleFlat();
            caster.Rotation = Rot4.FromAngleFlat(shotAngle);

            if (caster.CurJob != null)
                caster.CurJob.targetA = caster.Position + (direction * 10f).ToIntVec3();

            if (!(caster.stances.curStance is Stance_MasterSpark))
            {
                LocalTargetInfo focus = caster.Position + (direction * 5f).ToIntVec3();
                caster.stances.SetStance(new Stance_MasterSpark(100, focus, null));
            }

            float currentVisualSize = GetCurrentSize();
            if (age % DamageIntervalTicks == 0 && currentVisualSize > 0.2f)
            {
                DoLaserCollisionAndDamage(currentVisualSize / 2f);
                if (Find.CurrentMap == this.Map)
                    Find.CameraDriver.shaker.DoShake(0.25f * (currentVisualSize / HeadSize));
            }

            if (age > TotalDuration) Destroy();
        }

        private float GetCurrentSize()
        {
            if (age <= TotalDuration - FadeOutTicks) return HeadSize;
            float progress = (float)(age - (TotalDuration - FadeOutTicks)) / FadeOutTicks;
            return Mathf.Lerp(HeadSize, 0f, progress);
        }

        private void DoLaserCollisionAndDamage(float radius)
        {
            Map map = this.Map;
            Vector3 normDir = direction.normalized;
            float currentDynamicOffset = BaseOffsetDistance - (HeadSize - radius * 2f) / 2f;
            Vector3 visualStartPos = caster.DrawPos + (normDir * currentDynamicOffset);
            Vector3 damageStartPos = visualStartPos - (normDir * 2f);
            Vector3 endPos = visualStartPos + (normDir * BeamLength);

            HashSet<IntVec3> affectedCells = new HashSet<IntVec3>();
            Vector3 ortho = new Vector3(-normDir.z, 0, normDir.x).normalized;
            float dist = Vector3.Distance(damageStartPos, endPos);
            int radiusInt = Mathf.Max(0, Mathf.RoundToInt(radius));

            for (float d = 0; d <= dist; d += 0.8f)
            {
                Vector3 centerPoint = damageStartPos + normDir * d;
                for (int i = -radiusInt; i <= radiusInt; i++)
                {
                    IntVec3 c = (centerPoint + ortho * i).ToIntVec3();
                    if (c.InBounds(map)) affectedCells.Add(c);
                }
            }

            foreach (IntVec3 cell in affectedCells)
            {
                if (Rand.Value < FireChance) FireUtility.TryStartFireIn(cell, map, 0.6f, this);
                GenTemperature.PushHeat(cell, map, 100f);
                List<Thing> thingList = cell.GetThingList(map);
                for (int i = thingList.Count - 1; i >= 0; i--)
                {
                    Thing t = thingList[i];
                    if (t != this && t != caster && (t is Pawn || t is Building || t.def.useHitPoints))
                    {
                        float damageAmt = DamagePerHit;
                        if (t is Building) damageAmt *= 12f;
                        else if (!(t is Pawn)) damageAmt *= 2f;
                        t.TakeDamage(new DamageInfo(DamageDefOf.Flame, damageAmt, 1.5f, -1f, this));
                    }
                }
            }
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            Vector3 actualLoc = (caster != null) ? caster.DrawPos : drawLoc;
            float currentSize = GetCurrentSize();
            if (currentSize <= 0.01f) return;

            Vector3 normDir = direction.normalized;

            float shrinkOffset = (HeadSize - currentSize) / 2f;
            Vector3 originPos = actualLoc + (normDir * (BaseOffsetDistance - shrinkOffset));
            originPos.y = AltitudeLayer.MetaOverlays.AltitudeFor() + 0.8f;

            Quaternion rotation = Quaternion.LookRotation(normDir) * Quaternion.Euler(0, -90, 0);
            Material mat = MaterialPool.MatFrom($"Other/MasterSpark/bulletEa{(age % (TicksPerFrame * TotalFrames)) / TicksPerFrame:D3}", ShaderDatabase.MoteGlow);

            Matrix4x4 headMatrix = Matrix4x4.TRS(originPos, rotation, new Vector3(currentSize, 1f, currentSize));
            Graphics.DrawMesh(HeadMesh, headMatrix, mat, 0);

            float offsetToBeamCenter = (currentSize / 2f) + (BeamLength / 2f) - GapFiller;
            Vector3 beamPos = originPos + (normDir * offsetToBeamCenter);
            beamPos.y = originPos.y;

            Matrix4x4 beamMatrix = Matrix4x4.TRS(beamPos, rotation, new Vector3(BeamLength, 1f, currentSize));
            Graphics.DrawMesh(LineMesh, beamMatrix, mat, 0);
        }
    }
}
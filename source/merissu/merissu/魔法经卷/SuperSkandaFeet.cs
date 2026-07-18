using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace merissu
{
    public class CompProperties_superSkandaFeet : CompProperties_AbilityEffect
    {
        public CompProperties_superSkandaFeet()
        {
            compClass = typeof(CompAbilityEffect_superSkandaFeet);
        }
    }

    public class CompAbilityEffect_superSkandaFeet : CompAbilityEffect
    {
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            Pawn caster = parent.pawn;
            Map map = caster.Map;
            if (map == null || !target.IsValid) return;

            IntVec3 start = caster.Position;
            IntVec3 end = target.Cell;

            Mote_SuperSkandaTrail trail = (Mote_SuperSkandaTrail)ThingMaker.MakeThing(
                ThingDef.Named("Mote_SuperSkandaTrail"));
            trail.SetStartEnd(start, end, caster);
            GenSpawn.Spawn(trail, start, map);

            caster.Position = end;
            caster.Notify_Teleported(false, false);
        }

        public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
        {
            return target.Cell.IsValid && base.CanApplyOn(target, dest);
        }
    }

    [StaticConstructorOnStartup]
    public class Mote_SuperSkandaTrail : Thing
    {
        private IntVec3 start;
        private Pawn caster;
        private int age;
        private const int FadeOutTime = 8;
        private int lifeTime;
        private int damageEndTick;

        private Mesh customMesh;

        private static Material mat;
        private static readonly MaterialPropertyBlock propBlock = new MaterialPropertyBlock();

        private HashSet<Thing> damagedThings = new HashSet<Thing>();
        private const float DamageAmount = 100f;
        private const float KnockbackDistance = 3f;

        static Mote_SuperSkandaTrail()
        {
            mat = MaterialPool.MatFrom("Other/ShinkiRecitation/assultTrailA0000", ShaderDatabase.MoteGlow);
        }

        public void SetStartEnd(IntVec3 start, IntVec3 end, Pawn caster)
        {
            this.start = start;
            this.caster = caster;
            damagedThings.Clear();

            float dist = start.DistanceTo(end);
            int tiles = Mathf.Max(1, Mathf.RoundToInt(dist));
            lifeTime = Mathf.RoundToInt(20f + (tiles - 1) * 0.5f);
            damageEndTick = lifeTime - FadeOutTime;
        }

        protected override void Tick()
        {
            age++;
            if (caster == null || caster.Destroyed || !caster.Spawned || age >= lifeTime)
            {
                Destroy();
                return;
            }

            if (age <= damageEndTick)
            {
                ApplyShockwave();
            }
        }

        private void ApplyShockwave()
        {
            Map map = caster.Map;
            if (map == null) return;

            IntVec3 center = caster.DrawPos.ToIntVec3();

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    IntVec3 cell = center + new IntVec3(dx, 0, dz);
                    if (!cell.InBounds(map)) continue;

                    List<Thing> things = cell.GetThingList(map);
                    for (int i = things.Count - 1; i >= 0; i--)
                    {
                        Thing t = things[i];
                        if (t == caster || t.Destroyed) continue;

                        Pawn victim = t as Pawn;
                        if (victim != null)
                        {
                            if (victim.Dead) continue;
                            if (victim.Faction != null && !victim.Faction.HostileTo(caster.Faction)) continue;
                            if (!damagedThings.Add(victim)) continue;

                            victim.TakeDamage(new DamageInfo(DamageDefOf.Blunt, DamageAmount, 0f, -1f, caster));

                            if (!victim.Dead && !victim.Destroyed && victim.Spawned)
                            {
                                DoKnockback(victim, map);
                            }
                        }
                        else if (t is Building)
                        {
                            if (!damagedThings.Add(t)) continue;
                            t.Destroy(DestroyMode.KillFinalize);
                        }
                        else if (t is Plant plant)
                        {
                            if (!plant.def.plant.IsTree) continue;
                            if (!damagedThings.Add(t)) continue;
                            t.TakeDamage(new DamageInfo(DamageDefOf.Blunt, DamageAmount, 0f, -1f, caster));
                        }
                        else if (t.def.useHitPoints)
                        {
                            if (!damagedThings.Add(t)) continue;
                            t.TakeDamage(new DamageInfo(DamageDefOf.Blunt, DamageAmount, 0f, -1f, caster));
                        }
                    }
                }
            }
        }
        private void DoKnockback(Pawn pawn, Map map)
        {
            Vector3 knockDir = (pawn.DrawPos - caster.DrawPos).normalized;
            if (knockDir.magnitude < 0.1f)
                knockDir = Vector3.forward;

            Vector3 destVec = pawn.DrawPos + knockDir * KnockbackDistance;
            IntVec3 destCell = destVec.ToIntVec3();
            if (!destCell.InBounds(map))
                destCell = CellFinder.RandomClosewalkCellNear(destCell, map, 1);
            if (!destCell.IsValid || !destCell.InBounds(map)) return;

            PawnFlyer flyer = PawnFlyer.MakeFlyer(
                ThingDef.Named("PawnFlyer_Stun"), pawn, destCell, null, null);
            if (flyer != null)
                GenSpawn.Spawn(flyer, destCell, map);
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            base.Destroy(mode);
            if (customMesh != null)
            {
                Object.Destroy(customMesh);
                customMesh = null;
            }
        }

        private const float ForwardOffset = 0.6f;

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            if (mat == null || caster == null) return;

            float fadeProgress = Mathf.Clamp01((lifeTime - age) / (float)FadeOutTime);
            float alpha = Mathf.Clamp01(fadeProgress);
            propBlock.SetColor("_Color", new Color(1, 1, 1, alpha));

            Vector3 s = start.ToVector3ShiftedWithAltitude(AltitudeLayer.MoteOverhead);
            Vector3 e = caster.DrawPos;
            e.y = s.y;

            Vector3 rawDiff = e - s;
            if (rawDiff.magnitude < 0.01f) return;
            Vector3 dirNorm = rawDiff.normalized;

            e += dirNorm * ForwardOffset;

            Vector3 diff = e - s;
            float length = diff.magnitude;

            Quaternion rot = Quaternion.LookRotation(dirNorm);

            UpdateCustomMesh(length);

            Vector3 scale = new Vector3(3f, 1f, 1f);
            Matrix4x4 matrix = Matrix4x4.TRS(s, rot, scale);

            Graphics.DrawMesh(customMesh, matrix, mat, 0, null, 0, propBlock);
        }

        private void UpdateCustomMesh(float length)
        {
            if (customMesh == null)
            {
                customMesh = new Mesh();
                customMesh.name = "SuperSkandaTrailMesh";
                customMesh.MarkDynamic();
            }

            float headWorldLen = 3f * (70f / 230f);
            float uSplit = 160f / 230f;

            if (length < headWorldLen)
            {
                headWorldLen = length * (70f / 230f);
            }
            float splitZ = length - headWorldLen;

            Vector3[] vertices = new Vector3[]
            {
                new Vector3(-0.5f, 0f, 0f),
                new Vector3(-0.5f, 0f, splitZ),
                new Vector3(-0.5f, 0f, length),
                new Vector3( 0.5f, 0f, length),
                new Vector3( 0.5f, 0f, splitZ),
                new Vector3( 0.5f, 0f, 0f)
            };

            if (customMesh.vertexCount == 0)
            {
                Vector2[] uv = new Vector2[]
                {
                    new Vector2(0f, 1f),
                    new Vector2(uSplit, 1f),
                    new Vector2(1f, 1f),
                    new Vector2(1f, 0f),
                    new Vector2(uSplit, 0f),
                    new Vector2(0f, 0f)
                };

                int[] triangles = new int[]
                {
                    0, 1, 4,
                    0, 4, 5,
                    1, 2, 3,
                    1, 3, 4
                };

                customMesh.vertices = vertices;
                customMesh.uv = uv;
                customMesh.triangles = triangles;
            }
            else
            {
                customMesh.vertices = vertices;
            }

            customMesh.RecalculateNormals();
            customMesh.RecalculateBounds();
        }
    }
}
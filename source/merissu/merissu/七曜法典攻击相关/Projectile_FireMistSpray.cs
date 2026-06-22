using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace merissu
{
    public class AttackMode_FireMistSpray : GrimoireAttackMode
    {
        public override string ModeName => "FireMistSpray";
        protected override string ProjectileDefName => "Projectile_FireMistSpray";
        protected override string SoundDefName => "Fireball";

        public override int BurstCount => 90;
        public override int TicksBetweenShots => 2;
        public override float WarmupTime => 1.0f;

        public override bool PlaySoundOnEveryShot => false;

        public override bool OverrideCastShot(Verb_RandomElementalShoot verb, LocalTargetInfo target)
        {
            Pawn caster = verb.CasterPawn;
            Map map = caster.Map;
            if (map == null) return false;

            if (verb.burstShotsLeft == verb.verbProps.burstShotCount)
            {
                CastSound?.PlayOneShot(new TargetInfo(caster.Position, map));
            }

            Vector3 casterPos = caster.DrawPos;
            Vector3 targetPos = target.Cell.ToVector3Shifted();
            if (target.Thing != null) targetPos = target.Thing.DrawPos;

            Vector3 dir = (targetPos - casterPos).normalized;
            Vector3 spawnPos = casterPos + dir * 1f;
            IntVec3 spawnCell = spawnPos.ToIntVec3();

            float progress = 1f - ((float)verb.burstShotsLeft / BurstCount);
            float angleOffset = Mathf.Sin(progress * Mathf.PI * 16f) * 35f;

            float baseAngle = dir.AngleFlat() - 90f;
            float finalAngle = baseAngle + angleOffset;
            Vector3 projDir = Vector3Utility.FromAngleFlat(finalAngle);

            Vector3 projTargetPos = spawnPos + projDir * 20f;
            LocalTargetInfo projTargetInfo = new LocalTargetInfo(projTargetPos.ToIntVec3());

            Projectile proj = (Projectile)GenSpawn.Spawn(ProjectileDef, spawnCell, map);

            proj.Launch(caster, spawnPos, projTargetInfo, projTargetInfo, ProjectileHitFlags.None, false, null, null);

            return true;
        }
    }

    public class Projectile_FireMistSpray : Projectile
    {
        private Vector3 startPos;
        private bool initialized;

        private const float MaxDistance = 18f; 
        private const float MaxDistanceSq = MaxDistance * MaxDistance;

        private const float DamageStartDist = 1.5f;
        private const float DamageStartDistSq = DamageStartDist * DamageStartDist;

        private int fireTickCounter;

        private static readonly MaterialPropertyBlock MPB = new MaterialPropertyBlock();

        protected override void Tick()
        {
            base.Tick();

            if (!initialized)
            {
                startPos = ExactPosition;
                initialized = true;
            }

            if (Map == null) return;

            Vector3 offset = ExactPosition - startPos;
            float distSq = offset.sqrMagnitude;

            if (distSq > DamageStartDistSq)
            {
                if (++fireTickCounter >= 4)
                {
                    fireTickCounter = 0;
                    IntVec3 center = Position;

                    foreach (IntVec3 c in GenRadial.RadialCellsAround(center, 1.5f, true))
                    {
                        if (!c.InBounds(Map)) continue;

                        FireUtility.TryStartFireIn(c, Map, 0.15f, launcher);

                        List<Thing> thingList = c.GetThingList(Map);
                        for (int i = 0; i < thingList.Count; i++)
                        {
                            Thing t = thingList[i];

                            if (t is Pawn pawn && pawn != launcher && !pawn.Dead)
                            {
                                pawn.TakeDamage(new DamageInfo(
                                    DamageDefOf.Flame,
                                    4f,
                                    0f,
                                    -1f,
                                    launcher));
                            }
                        }
                    }
                }
            }

            if (distSq >= MaxDistanceSq)
            {
                Destroy(DestroyMode.Vanish);
            }
        }

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            if (!initialized) return;

            Vector3 offset = ExactPosition - startPos;

            float progress = Mathf.Clamp01(offset.sqrMagnitude / MaxDistanceSq);

            float alpha = 1f - (progress * progress * progress);

            Material mat = Graphic.MatSingle;
            MPB.Clear();

            MPB.SetColor(ShaderPropertyIDs.Color, new Color(1f, 0.6f * alpha, 0.3f * alpha, alpha));

            Vector3 scale = new Vector3(this.def.graphicData.drawSize.x, 1f, this.def.graphicData.drawSize.y);

            Graphics.DrawMesh(
                MeshPool.plane10,
                Matrix4x4.TRS(drawLoc, ExactRotation, scale),
                mat,
                0,
                null,
                0,
                MPB
            );
        }
    }
}
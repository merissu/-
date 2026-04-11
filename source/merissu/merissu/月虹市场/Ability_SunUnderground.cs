using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using System.Collections.Generic;
using System.Linq;

namespace merissu
{
    public class Ability_SunUnderground : Ability
    {
        public Ability_SunUnderground() : base() { }
        public Ability_SunUnderground(Pawn pawn, AbilityDef def) : base(pawn, def) { }

        private static readonly HediffDef InvincibleHediffDef = HediffDef.Named("Suninvincible");
        private static readonly ThingDef AnimationThingDef = ThingDef.Named("SunUndergroundAnimation");

        public override bool Activate(LocalTargetInfo target, LocalTargetInfo dest)
        {
            bool result = base.Activate(target, dest);
            if (pawn == null || pawn.Dead || pawn.Map == null) return false;

            Hediff invincibleHediff = HediffMaker.MakeHediff(InvincibleHediffDef, pawn);
            pawn.health.AddHediff(invincibleHediff);

            var sunAnim = (Thing_SunUndergroundAnimation)ThingMaker.MakeThing(AnimationThingDef);
            sunAnim.Init(pawn, invincibleHediff);
            GenSpawn.Spawn(sunAnim, pawn.Position, pawn.Map);

            return result;
        }
    }
    [StaticConstructorOnStartup]
    public class Thing_SunUndergroundAnimation : Thing
    {
        public const float ExpandDuration = 5.0f;
        public const float FadeDuration = 1.0f;
        private const float StartSize = 5f;
        private const float EndSize = 120f;
        private const float JitterAmount = 1.2f;
        private const float JitterSpeed = 60f;

        private const int DestroyIntervalTicks = 4; 
        private const float BurnDamageAmount = 50f;

        private static readonly string SunTexPath = "Other/FixedStar/bulletFd000";

        private Pawn caster;
        private Hediff associatedHediff;
        private float ageSeconds = 0f;
        private int ageTicks = 0;
        private Material sunMat;

        private static Mesh _quadMesh;
        private static Mesh QuadMesh
        {
            get
            {
                if (_quadMesh == null)
                {
                    _quadMesh = new Mesh { name = "SunUnderground_CenteredQuad" };
                    _quadMesh.vertices = new Vector3[]
                    {
                        new Vector3(-0.5f, 0, -0.5f), new Vector3( 0.5f, 0, -0.5f),
                        new Vector3(-0.5f, 0,  0.5f), new Vector3( 0.5f, 0,  0.5f)
                    };
                    _quadMesh.uv = new Vector2[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };
                    _quadMesh.triangles = new int[] { 0, 2, 1, 1, 2, 3 };
                    _quadMesh.RecalculateNormals();
                }
                return _quadMesh;
            }
        }

        public void Init(Pawn pawn, Hediff hediff)
        {
            this.caster = pawn;
            this.associatedHediff = hediff;
            this.sunMat = MaterialPool.MatFrom(SunTexPath, ShaderDatabase.MoteGlow);
        }

        protected override void Tick()
        {
            base.Tick();
            ageTicks++;
            ageSeconds += 1f / 60f;

            if (caster == null || caster.Destroyed || caster.Map == null)
            {
                Destroy();
                return;
            }

            this.Position = caster.Position;
            float currentRadius = CalculateCurrentDiameter() / 2f;

            if (ageTicks % DestroyIntervalTicks == 0)
            {
                AnnihilateEverything(currentRadius);
            }

            if (ageSeconds >= (ExpandDuration + FadeDuration))
            {
                Destroy();
            }
        }

        private float CalculateCurrentDiameter()
        {
            float lerpPercent = Mathf.Clamp01(ageSeconds / ExpandDuration);
            float baseSize = Mathf.Lerp(StartSize, EndSize, lerpPercent);
            float jitter = Mathf.Sin(ageSeconds * JitterSpeed) * JitterAmount;
            return baseSize + jitter;
        }

        private void AnnihilateEverything(float radius)
        {
            Map map = caster.Map;
            if (map == null) return;

            IEnumerable<IntVec3> cells = GenRadial.RadialCellsAround(caster.Position, radius, true);

            foreach (IntVec3 cell in cells)
            {
                if (!cell.InBounds(map)) continue;

                List<Thing> thingList = cell.GetThingList(map);
                for (int i = thingList.Count - 1; i >= 0; i--)
                {
                    Thing t = thingList[i];

                    if (t == caster || t == this) continue;

                    if (t is Pawn p)
                    {
                        if (!p.Dead && p.Faction != caster.Faction)
                        {
                            p.TakeDamage(new DamageInfo(DamageDefOf.Flame, BurnDamageAmount, 999, -1, caster));
                            p.TryAttachFire(0.5f, caster);
                        }
                        continue;
                    }

                    if (t is Plant)
                    {
                        if (Rand.Value < 0.1f)
                        {
                            FleckMaker.ThrowSmoke(t.DrawPos, map, 1.2f);
                        }
                        t.Kill();
                        continue;
                    }

                    if (t.GetStatValue(StatDefOf.Flammability) <= 0) continue;

                    if (t.def.category == ThingCategory.Item)
                    {
                        if (Rand.Value < 0.2f) FleckMaker.ThrowSmoke(t.DrawPos, map, 1.5f);
                        FleckMaker.ThrowSmoke(cell.ToVector3Shifted(), map, 1.5f);
                        t.TakeDamage(new DamageInfo(DamageDefOf.Flame, 9999f, 999, -1, caster));
                    }
                    else if (t.def.useHitPoints)
                    {
                        t.TakeDamage(new DamageInfo(DamageDefOf.Flame, BurnDamageAmount, 999, -1, caster));

                        if (t.Destroyed && Rand.Value < 0.3f)
                        {
                            FleckMaker.ThrowMicroSparks(t.DrawPos, map);
                        }
                    }
                }
            }
        }
        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            if (caster == null || sunMat == null) return;

            float size = CalculateCurrentDiameter();
            float alpha = 1f;
            if (ageSeconds > ExpandDuration)
            {
                alpha = 1f - Mathf.Clamp01((ageSeconds - ExpandDuration) / FadeDuration);
            }

            sunMat.color = new Color(1f, 1f, 1f, alpha);
            Vector3 pos = caster.DrawPos;
            pos.y = AltitudeLayer.MetaOverlays.AltitudeFor();

            Matrix4x4 matrix = Matrix4x4.TRS(pos, Quaternion.identity, new Vector3(size, 1f, size));
            Graphics.DrawMesh(QuadMesh, matrix, sunMat, 0);
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            if (caster != null && !caster.Dead && associatedHediff != null)
            {
                caster.health.RemoveHediff(associatedHediff);
            }
            base.Destroy(mode);
        }
    }
}
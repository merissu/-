using RimWorld;
using UnityEngine;
using Verse;

namespace merissu
{
    public class HediffCompProperties_InfraredMoonEffect : HediffCompProperties
    {
        public HediffCompProperties_InfraredMoonEffect() => compClass = typeof(HediffComp_InfraredMoonEffect);
    }

    [StaticConstructorOnStartup]
    public class Thing_InfraredMoonEffect : Thing
    {
        public Pawn caster;
        private int age;
        private const int LifeTicks = 30;
        private const float StartSize = 7f;
        private const float EndSize = 10f;
        private Material mat;
        private static readonly MaterialPropertyBlock propBlock = new MaterialPropertyBlock();

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            mat = MaterialPool.MatFrom("Projectiles/bulletAb000", ShaderDatabase.Mote);
        }

        protected override void Tick()
        {
            base.Tick();
            age++;

            if (caster == null || caster.Dead || !caster.Spawned)
            {
                Destroy();
                return;
            }

            if (age >= LifeTicks)
            {
                Destroy();
                return;
            }

            Position = caster.Position;
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            if (caster == null) return;

            float t = age / (float)LifeTicks;
            t = Mathf.Clamp01(t);

            float size = Mathf.Lerp(StartSize, EndSize, t);
            float alpha = 1f - t;
            Color color = Color.white;
            color.a = alpha;

            propBlock.SetColor("_Color", color);
            Vector3 pos = caster.DrawPos;
            pos.y = AltitudeLayer.MoteOverhead.AltitudeFor();

            Matrix4x4 matrix = Matrix4x4.TRS(pos, Quaternion.identity, new Vector3(size, 1f, size));
            Graphics.DrawMesh(MeshPool.plane10, matrix, mat, 0, null, 0, propBlock);
        }
    }

    public class HediffComp_InfraredMoonEffect : HediffComp
    {
        private int ticksUntilNextSpawn = 0;

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            Pawn pawn = parent.pawn;

            if (pawn == null || pawn.Map == null || pawn.Dead)
                return;

            ticksUntilNextSpawn--;

            if (ticksUntilNextSpawn <= 0)
            {
                SpawnEffect(pawn);
                ticksUntilNextSpawn = 20;
            }
        }

        private void SpawnEffect(Pawn pawn)
        {
            Thing thing = ThingMaker.MakeThing(ThingDef.Named("InfraredMoonEffect"));
            Thing_InfraredMoonEffect effect = thing as Thing_InfraredMoonEffect;
            if (effect != null)
            {
                effect.caster = pawn;
                GenSpawn.Spawn(effect, pawn.Position, pawn.Map);
            }
        }
    }
}
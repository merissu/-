using System.Collections.Generic;
using RimWorld;
using Verse;
using UnityEngine;
using HarmonyLib;

namespace merissu
{
    public class HediffCompProperties_JellyfishPrincess : HediffCompProperties
    {
        public ThingDef bubbleMoteDef;
        public int maxHits = 100;
        public float extinguishRadius = 2.0f;

        public HediffCompProperties_JellyfishPrincess()
        {
            this.compClass = typeof(HediffComp_JellyfishPrincess);
        }
    }

    public class HediffComp_JellyfishPrincess : HediffComp
    {
        public HediffCompProperties_JellyfishPrincess Props => (HediffCompProperties_JellyfishPrincess)props;

        private int hitsTaken = 0;
        private Mote_JellyfishBubble bubbleMote;

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            if (Pawn == null || !Pawn.Spawned || Pawn.Dead) return;

            if (bubbleMote == null || bubbleMote.Destroyed)
            {
                SpawnBubbleMote();
            }
            else
            {
                bubbleMote.Maintain();
            }

            if (Pawn.IsHashIntervalTick(60))
            {
                ExtinguishFires();
            }
        }

        private void SpawnBubbleMote()
        {
            if (Props.bubbleMoteDef != null && Pawn.Map != null)
            {
                bubbleMote = (Mote_JellyfishBubble)ThingMaker.MakeThing(Props.bubbleMoteDef);
                bubbleMote.exactPosition = Pawn.DrawPos;
                bubbleMote.Attach(Pawn);
                GenSpawn.Spawn(bubbleMote, Pawn.Position, Pawn.Map);
            }
        }

        public void Notify_Attacked()
        {
            hitsTaken++;

            if (bubbleMote != null && !bubbleMote.Destroyed)
            {
                bubbleMote.Notify_Hit();
            }

            if (hitsTaken >= Props.maxHits)
            {
                Pawn.health.RemoveHediff(this.parent);
            }
        }

        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();

            if (bubbleMote != null && !bubbleMote.Destroyed)
            {
                bubbleMote.Destroy();
            }

            SpawnPopEffect();
        }

        private void SpawnPopEffect()
        {
            Map map = Pawn?.Map;
            if (map != null)
            {
                Vector3 center = Pawn.DrawPos;
                for (int i = 0; i < 8; i++)
                {
                    Thing mote = ThingMaker.MakeThing(ThingDef.Named("Mote_BulletDestroy"));
                    float angle = i * 45f;
                    Vector3 offset = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), 0, Mathf.Sin(angle * Mathf.Deg2Rad)) * 1.5f;

                    if (mote is Mote_BulletDestroyFade thrownMote)
                    {
                        thrownMote.exactPosition = center + offset;
                        GenSpawn.Spawn(thrownMote, Pawn.Position, map);
                    }
                }
            }
        }

        private void ExtinguishFires()
        {
            Map map = Pawn.Map;
            IntVec3 pos = Pawn.Position;

            var selfFire = Pawn.GetAttachment(ThingDefOf.Fire);
            if (selfFire != null) selfFire.Destroy();

            int cellCount = GenRadial.NumCellsInRadius(Props.extinguishRadius);
            for (int i = 0; i < cellCount; i++)
            {
                IntVec3 targetCell = pos + GenRadial.RadialPattern[i];
                if (!targetCell.InBounds(map)) continue;

                List<Thing> things = targetCell.GetThingList(map);
                for (int j = things.Count - 1; j >= 0; j--)
                {
                    if (things[j] is Fire fire)
                    {
                        fire.TakeDamage(new DamageInfo(DamageDefOf.Extinguish, 100f));
                    }
                }
            }
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref hitsTaken, "hitsTaken", 0);
            Scribe_References.Look(ref bubbleMote, "bubbleMote");
        }
    }
    public class Mote_JellyfishBubble : MoteAttached
    {
        private int lastHitTick = -999;

        public void Notify_Hit()
        {
            lastHitTick = Find.TickManager.TicksGame;
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            if (!this.Spawned) return;

            int ticks = Find.TickManager.TicksGame;
            float time = (ticks + this.thingIDNumber) * 0.1f;

            float stretchX = 1f + Mathf.Sin(time) * 0.08f;
            float stretchZ = 1f + Mathf.Cos(time) * 0.08f;

            int ticksSinceHit = ticks - lastHitTick;
            if (ticksSinceHit < 20)
            {
                float dampFactor = 1f - (ticksSinceHit / 20f);
                float hitEffect = Mathf.Sin(ticksSinceHit * 1.5f) * dampFactor;

                stretchX += hitEffect * 0.2f;
                stretchZ -= hitEffect * 0.2f;
            }

            Vector2 baseSize = this.def.graphicData.drawSize;
            Vector3 exactScale = new Vector3(baseSize.x * stretchX, 1f, baseSize.y * stretchZ);

            Matrix4x4 matrix = default;
            matrix.SetTRS(drawLoc, Quaternion.AngleAxis(this.exactRotation, Vector3.up), exactScale);

            Graphics.DrawMesh(MeshPool.plane10, matrix, this.Graphic.MatSingle, 0);
        }
    }
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.PreApplyDamage))]
    public static class Patch_Pawn_PreApplyDamage_Jellyfish
    {
        public static bool Prefix(Pawn __instance, ref DamageInfo dinfo, out bool absorbed)
        {
            absorbed = false;

            if (__instance == null || dinfo.Def == DamageDefOf.Extinguish || dinfo.Def == DamageDefOf.SurgicalCut)
                return true;

            var hediff = __instance.health?.hediffSet?.GetFirstHediffOfDef(HediffDef.Named("JellyfishPrincess"));
            if (hediff != null)
            {
                var comp = hediff.TryGetComp<HediffComp_JellyfishPrincess>();
                if (comp != null)
                {
                    comp.Notify_Attacked();

                    absorbed = true;
                    return false;
                }
            }
            return true;
        }
    }
}
using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace merissu
{
    public class AttackMode_CondensedBubble : GrimoireAttackMode
    {
        public override string ModeName => "CondensedBubble";
        protected override string ProjectileDefName => "Projectile_CondensedBubble_Large";
        protected override string SoundDefName => "JellyfishPrincess";
        public override int BurstCount => 1;
        public override int TicksBetweenShots => 1;
        public override float WarmupTime => 1.5f;
    }

    public class Projectile_CondensedBubble : Projectile
    {
        private int tickCounter = 0;
        private Pawn caster;

        public int Generation
        {
            get
            {
                if (this.def.defName.Contains("Small")) return 2;
                if (this.def.defName.Contains("Medium")) return 1;
                return 0;
            }
        }

        public float ExplodeRadius => Generation == 0 ? 4f : (Generation == 1 ? 2f : 1f);
        public float InterceptRadius => Generation == 0 ? 2.5f : (Generation == 1 ? 1.5f : 0.75f);
        public float ProximityRadius => ExplodeRadius * 0.8f;
        private static readonly System.Type ProjectileCEType =
AccessTools.TypeByName("CombatExtended.ProjectileCE");

        protected override void Tick()
        {
            base.Tick();
            if (this.Destroyed || this.Map == null) return;
            tickCounter++;

            InterceptProjectiles();

            if (tickCounter % 3 == 0) ExtinguishFiresUnderneath();

            if (tickCounter % 5 == 0 && CheckProximityExplode())
            {
                Explode();
            }
        }

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Explode();
        }

        private void ExtinguishFiresUnderneath()
        {
            IntVec3 pos = this.Position;
            if (!pos.InBounds(this.Map)) return;

            int cellCount = GenRadial.NumCellsInRadius(Generation == 0 ? 1.9f : (Generation == 1 ? 1f : 0.5f));
            for (int i = 0; i < cellCount; i++)
            {
                IntVec3 targetCell = pos + GenRadial.RadialPattern[i];
                if (!targetCell.InBounds(this.Map)) continue;

                List<Thing> things = targetCell.GetThingList(this.Map);
                for (int j = things.Count - 1; j >= 0; j--)
                {
                    if (things[j] is Fire fire)
                    {
                        fire.TakeDamage(new DamageInfo(DamageDefOf.Extinguish, 100f));
                    }
                }
            }
        }

        private void InterceptProjectiles()
        {
            CellRect rect =
                CellRect.CenteredOn(
                    this.Position,
                    Mathf.CeilToInt(InterceptRadius));

            foreach (IntVec3 cell in rect)
            {
                if (!cell.InBounds(this.Map))
                    continue;

                List<Thing> things = cell.GetThingList(this.Map);

                for (int i = things.Count - 1; i >= 0; i--)
                {
                    Thing t = things[i];

                    if (t == this)
                        continue;

                    Projectile vanillaProj = t as Projectile;

                    if (vanillaProj != null)
                    {
                        if (vanillaProj.launcher == this.launcher)
                            continue;

                        if (vanillaProj.def.defName.Contains("CondensedBubble"))
                            continue;

                        if (vanillaProj.Position.DistanceTo(this.Position)
                            <= InterceptRadius)
                        {
                            FleckMaker.ThrowMicroSparks(
                                vanillaProj.DrawPos,
                                this.Map);

                            vanillaProj.Destroy(
                                DestroyMode.Vanish);
                        }

                        continue;
                    }


                    if (ProjectileCEType != null &&
                        ProjectileCEType.IsAssignableFrom(
                            t.GetType()))
                    {
                        Thing launcher = null;

                        try
                        {
                            launcher =
                                Traverse.Create(t)
                                    .Field("launcher")
                                    .GetValue<Thing>();
                        }
                        catch
                        {
                        }

                        if (launcher == null)
                        {
                            try
                            {
                                launcher =
                                    Traverse.Create(t)
                                        .Property("Launcher")
                                        .GetValue<Thing>();
                            }
                            catch
                            {
                            }
                        }

                        if (launcher == this.launcher)
                            continue;

                        if (t.Position.DistanceTo(this.Position)
                            > InterceptRadius)
                            continue;

                        FleckMaker.ThrowMicroSparks(
                            t.DrawPos,
                            this.Map);

                        t.Destroy(
                            DestroyMode.Vanish);
                    }
                }
            }
        }
        private bool CheckProximityExplode()
        {
            foreach (IntVec3 c in GenRadial.RadialCellsAround(this.Position, ProximityRadius, true))
            {
                if (!c.InBounds(this.Map)) continue;
                List<Thing> things = c.GetThingList(this.Map);
                for (int i = 0; i < things.Count; i++)
                {
                    if (things[i] is Pawn pawn && pawn.HostileTo(this.launcher != null ? this.launcher.Faction : null))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private void Explode()
        {
            if (this.Destroyed) return;
            Map map = this.Map;
            IntVec3 pos = this.Position;

            SoundDef.Named("CondensedBubble")?.PlayOneShot(new TargetInfo(pos, map));
            SpawnMist(pos, map);

            float radius = 2f;
            int damAmount = 20;
            float armorPen = damAmount * 0.015f;
            Faction casterFaction = this.launcher?.Faction;

            foreach (IntVec3 cell in GenRadial.RadialCellsAround(pos, radius, true))
            {
                if (!cell.InBounds(map)) continue;

                List<Thing> things = cell.GetThingList(map);
                for (int i = things.Count - 1; i >= 0; i--)
                {
                    if (things[i] is Pawn pawn && pawn.HostileTo(casterFaction))
                    {
                        float dist = pawn.Position.DistanceTo(pos);
                        float falloff = 1f - dist / radius;
                        int finalDamage = Mathf.Max(1, (int)(damAmount * falloff));

                        pawn.TakeDamage(new DamageInfo(
                            DamageDefOf.Bomb,
                            finalDamage,
                            armorPen,
                            angle: -1f,
                            instigator: this.launcher
                        ));
                    }
                }
            }

            if (Generation < 2)
            {
                string nextDefName = Generation == 0 ? "Projectile_CondensedBubble_Medium" : "Projectile_CondensedBubble_Small";
                ThingDef nextDef = ThingDef.Named(nextDefName);
                for (int i = 0; i < 6; i++)
                {
                    if (ThingMaker.MakeThing(nextDef) is Projectile nextBubble)
                    {
                        GenSpawn.Spawn(nextBubble, pos, map);
                        IntVec3 randomTarget = pos + new IntVec3(Rand.Range(-3, 4), 0, Rand.Range(-3, 4));
                        nextBubble.Launch(this.launcher, pos.ToVector3(), new LocalTargetInfo(randomTarget), randomTarget, ProjectileHitFlags.All);
                    }
                }
            }

            this.Destroy(DestroyMode.Vanish);
        }

        private void SpawnMist(IntVec3 pos, Map map)
        {
            ThingDef mistDef = ThingDef.Named("Mote_WaterMistScattering");
            if (mistDef == null) return;

            for (int i = 0; i < 8; i++)
            {
                Thing mist = ThingMaker.MakeThing(mistDef);
                if (mist is MoteThrown thrown)
                {
                    float angle = i * 45f + Rand.Range(-15f, 15f);
                    thrown.exactPosition = pos.ToVector3Shifted() + new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), 0, Mathf.Sin(angle * Mathf.Deg2Rad)) * 0.5f;
                    thrown.SetVelocity(angle, Rand.Range(1.5f, 3.5f));
                    GenSpawn.Spawn(thrown, pos, map);
                }
            }
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            if (!this.Spawned) return;

            int ticks = Find.TickManager.TicksGame;
            float time = (ticks + this.thingIDNumber) * 0.1f;

            float stretchX = 1f + Mathf.Sin(time) * 0.08f;
            float stretchZ = 1f + Mathf.Cos(time) * 0.08f;

            Vector2 baseSize = this.def.graphicData.drawSize;
            Vector3 exactScale = new Vector3(baseSize.x * stretchX, 1f, baseSize.y * stretchZ);

            Matrix4x4 matrix = default;
            matrix.SetTRS(drawLoc, this.ExactRotation, exactScale);

            Graphics.DrawMesh(MeshPool.plane10, matrix, this.Graphic.MatSingle, 0);
        }
    }
}
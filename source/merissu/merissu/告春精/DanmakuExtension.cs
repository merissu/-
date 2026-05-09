using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace merissu
{
    public class DanmakuExtension : DefModExtension
    {
        public ThingDef projectileA;
        public ThingDef projectileB;
        public ThingDef projectileC;          
        public ThingDef projectileD;          
        public ThingDef moverDef;
        public ThingDef ultimateEmitterDef;   

        public int bulletCount = 96;
        public float radius = 3.5f;
        public int expandTicks = 30;
        public int hoverTicks = 60;

        public string normalBodyTexPath = "Pawn/Lily/Lily";
        public string shootingBodyTexPath = "Pawn/Lily/LilyB";
    }
    public class CompLilyDanmakuTracker : ThingComp
    {
        public int shotCount = 0;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref shotCount, "shotCount", 0);
        }
    }

    public class CompProperties_LilyDanmakuTracker : CompProperties
    {
        public CompProperties_LilyDanmakuTracker()
        {
            this.compClass = typeof(CompLilyDanmakuTracker);
        }
    }

    [HarmonyPatch(typeof(Pawn_StanceTracker), nameof(Pawn_StanceTracker.SetStance))]
    public static class Patch_Pawn_StanceTracker_SetStance_LilyGraphicDirty
    {
        private static bool IsLilyWarmup(Stance stance)
        {
            return stance is Stance_Warmup w && w.verb is Verb_LilyDanmaku;
        }

        public static void Prefix(Pawn_StanceTracker __instance, out bool __state)
        {
            __state = IsLilyWarmup(__instance?.curStance);
        }

        public static void Postfix(Pawn_StanceTracker __instance, Stance newStance, bool __state)
        {
            Pawn pawn = __instance?.pawn;
            if (pawn == null) return;

            var ext = pawn.def?.GetModExtension<DanmakuExtension>();
            if (ext == null) return;

            bool nowWarmup = IsLilyWarmup(newStance);
            if (nowWarmup != __state)
            {
                pawn.Drawer?.renderer?.SetAllGraphicsDirty();
            }
        }
    }

    [HarmonyPatch(typeof(PawnRenderNode_AnimalPart), nameof(PawnRenderNode_AnimalPart.GraphicFor))]
    public static class Patch_PawnRenderNode_AnimalPart_GraphicFor
    {
        public static void Postfix(Pawn pawn, ref Graphic __result)
        {
            if (pawn == null || __result == null || pawn.def == null) return;

            var ext = pawn.def.GetModExtension<DanmakuExtension>();
            if (ext == null || ext.shootingBodyTexPath.NullOrEmpty()) return;

            bool isShootingWarmup = pawn.stances?.curStance is Stance_Warmup warmup && warmup.verb is Verb_LilyDanmaku;
            if (!isShootingWarmup) return;

            __result = GraphicDatabase.Get<Graphic_Multi>(
                ext.shootingBodyTexPath,
                __result.Shader,
                __result.drawSize,
                __result.Color,
                __result.ColorTwo
            );
        }
    }

    public class Verb_LilyDanmaku : Verb_Shoot
    {
        protected override bool TryCastShot()
        {
            if (currentTarget.HasThing && currentTarget.Thing.Map != caster.Map) return false;

            var ext = caster.def.GetModExtension<DanmakuExtension>();
            if (ext == null) return false;

            var tracker = caster.TryGetComp<CompLilyDanmakuTracker>();
            if (tracker != null)
            {
                if (tracker.shotCount >= 3)
                {
                    FireUltimate(ext);
                    tracker.shotCount = 0;
                    return true; 
                }
                else
                {
                    tracker.shotCount++;
                }
            }

            Vector3 startPos = caster.DrawPos;
            int bulletsPerRing = ext.bulletCount;
            int ringCount = 3;

            for (int ring = 0; ring < ringCount; ring++)
            {
                float currentRadius = ext.radius + (ring * 1.2f);
                int extraHover = ring * 10;

                for (int i = 0; i < bulletsPerRing; i++)
                {
                    DanmakuMover mover = (DanmakuMover)ThingMaker.MakeThing(ext.moverDef);
                    mover.launcher = caster;
                    mover.startPos = startPos;

                    int patternIndex = i % 6;
                    mover.projectileDef = (patternIndex < 3) ? ext.projectileA : ext.projectileB;

                    float angle = (360f / bulletsPerRing) * i;
                    if (ring == 1) angle += (360f / bulletsPerRing) / 2f;

                    Vector3 offset = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), 0, Mathf.Sin(angle * Mathf.Deg2Rad));
                    mover.hoverPos = startPos + offset * currentRadius;

                    Vector3 dirOutward = offset.normalized;
                    Vector3 dirTangentLeft = new Vector3(-dirOutward.z, 0, dirOutward.x);
                    Vector3 dirTangentRight = new Vector3(dirOutward.z, 0, -dirOutward.x);

                    if (patternIndex < 3)
                    {
                        if (patternIndex == 0) mover.shootDir = dirTangentLeft;
                        else if (patternIndex == 1)
                        {
                            mover.isTargeted = true;
                            mover.target = currentTarget;
                        }
                        else mover.shootDir = dirTangentRight;
                    }
                    else
                    {
                        mover.shootDir = dirTangentLeft;
                    }

                    mover.expandTicks = ext.expandTicks;
                    mover.hoverTicks = ext.hoverTicks + extraHover;

                    GenSpawn.Spawn(mover, startPos.ToIntVec3(), caster.Map);
                }
            }
            return true;
        }

        private void FireUltimate(DanmakuExtension ext)
        {
            if (ext.ultimateEmitterDef != null && ext.projectileC != null)
            {
                UltimateDanmakuEmitter emitter = (UltimateDanmakuEmitter)ThingMaker.MakeThing(ext.ultimateEmitterDef);
                emitter.caster = caster;
                emitter.projectileDefC = ext.projectileC; 
                emitter.projectileDefD = ext.projectileD; 
                GenSpawn.Spawn(emitter, caster.Position, caster.Map);
            }
        }
    }

    public class UltimateDanmakuEmitter : Thing
    {
        public Thing caster;
        public ThingDef projectileDefC; 
        public ThingDef projectileDefD; 

        private int age = 0;
        private bool initialized = false;
        private float baseAngle = 0f;

        public int duration = 300;
        public int fireInterval = 2;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref caster, "caster");
            Scribe_Defs.Look(ref projectileDefC, "projectileDefC");
            Scribe_Defs.Look(ref projectileDefD, "projectileDefD");
            Scribe_Values.Look(ref age, "age");
            Scribe_Values.Look(ref baseAngle, "baseAngle");
            Scribe_Values.Look(ref initialized, "initialized");
        }

        protected override void Tick()
        {
            bool isDead = caster is Pawn pawn && pawn.Dead;
            if (caster == null || caster.Destroyed || !caster.Spawned || isDead)
            {
                this.Destroy();
                return;
            }

            if (!initialized)
            {
                baseAngle = caster.Rotation.AsAngle;
                initialized = true;
            }

            this.Position = caster.Position;

            age++;
            if (age > duration)
            {
                this.Destroy();
                return;
            }

            if (age % fireInterval == 0)
            {
                FireDoubleCrossFanBullets();
            }
        }

        private void FireDoubleCrossFanBullets()
        {
            if (this.Map == null) return;

            float progress = (float)age / duration;
            Vector3 spawnPos = caster.DrawPos;

            float spreadInterval = 12.5f; 

            float fanHalfWidth = 2 * spreadInterval;

            float maxCenterOffset = 90f - fanHalfWidth;

            float sweepOffset = Mathf.Cos(progress * Mathf.PI * 2f) * maxCenterOffset;

            for (int i = -2; i <= 2; i++)
            {
                float fanSpread = i * spreadInterval;

                float angleA = baseAngle + sweepOffset + fanSpread;
                SpawnProjectile(projectileDefC, angleA, spawnPos);

                if (projectileDefD != null)
                {
                    float angleB = baseAngle - sweepOffset + fanSpread;
                    SpawnProjectile(projectileDefD, angleB, spawnPos);
                }
            }
        }
        private void SpawnProjectile(ThingDef bulletDef, float targetAngle, Vector3 spawnPos)
        {
            if (bulletDef == null) return;
            float mathAngle = 90f - targetAngle;
            float finalRad = mathAngle * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(finalRad), 0, Mathf.Sin(finalRad));

            IntVec3 targetCell = (spawnPos + dir * 50f).ToIntVec3();

            Projectile proj = (Projectile)GenSpawn.Spawn(bulletDef, this.Position, this.Map);
            proj.Launch(caster, spawnPos, targetCell, targetCell, ProjectileHitFlags.All);
        }
    }
    public class DanmakuMover : Thing
    {
        public ThingDef projectileDef;
        public Thing launcher;
        public Vector3 startPos;
        public Vector3 hoverPos;
        public LocalTargetInfo target;
        public Vector3 shootDir;
        public bool isTargeted;

        public int expandTicks;
        public int hoverTicks;
        private int age = 0;

        private float OutwardRotation => (hoverPos - startPos).AngleFlat();

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref projectileDef, "projectileDef");
            Scribe_References.Look(ref launcher, "launcher");
            Scribe_Values.Look(ref startPos, "startPos");
            Scribe_Values.Look(ref hoverPos, "hoverPos");
            Scribe_TargetInfo.Look(ref target, "target");
            Scribe_Values.Look(ref shootDir, "shootDir");
            Scribe_Values.Look(ref isTargeted, "isTargeted");
            Scribe_Values.Look(ref age, "age");
            Scribe_Values.Look(ref expandTicks, "expandTicks");
            Scribe_Values.Look(ref hoverTicks, "hoverTicks");
        }

        protected override void Tick()
        {
            age++;
            this.Position = DrawPos.ToIntVec3();

            if (age >= expandTicks + hoverTicks)
            {
                FireRealProjectile();
                this.Destroy();
            }
        }

        public override Vector3 DrawPos
        {
            get
            {
                if (age < expandTicks)
                {
                    return Vector3.Lerp(startPos, hoverPos, (float)age / expandTicks);
                }
                return hoverPos;
            }
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            if (projectileDef != null && projectileDef.graphicData != null)
            {
                float rot;
                if (age < expandTicks + hoverTicks - 1)
                {
                    rot = OutwardRotation;
                }
                else
                {
                    rot = isTargeted && target.IsValid
                        ? (target.Cell.ToVector3Shifted() - drawLoc).AngleFlat()
                        : shootDir.AngleFlat();
                }

                projectileDef.graphicData.Graphic.Draw(drawLoc, Rot4.North, this, rot);
            }
        }

        private void FireRealProjectile()
        {
            if (this.Map == null) return;

            Projectile proj = (Projectile)GenSpawn.Spawn(projectileDef, this.Position, this.Map);
            LocalTargetInfo launchTarget = target;

            if (!isTargeted || !target.IsValid)
            {
                IntVec3 destCell = (DrawPos + shootDir * 40f).ToIntVec3();
                launchTarget = new LocalTargetInfo(destCell);
            }

            proj.Launch(launcher, DrawPos, launchTarget, launchTarget, ProjectileHitFlags.All);
        }
    }
}
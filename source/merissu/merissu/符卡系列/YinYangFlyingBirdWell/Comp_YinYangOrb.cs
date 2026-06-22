using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace merissu
{
    public class Comp_YinYangOrb : ThingComp
    {
        private IntVec3 targetCell;
        private Pawn caster;

        private Vector3 visualPos;
        private Vector3 moveDir;
        private float rotationAngle;

        private bool arrived;
        private int lingerLeft;

        private enum OrbState { Spawning, Alive, Fading, Dead }
        private OrbState state = OrbState.Spawning;
        private int stateTick = 0;

        private const int SpawnDuration = 20;
        private const int FadeOutDuration = 10;    

        public float CurrentScale { get; private set; } = 0f;
        public float CurrentAlpha { get; private set; } = 1f;

        private float orbDrawSize;

        private Mote_OrbEffectA effectA;
        private int effectBTimer = 0;
        private const int EffectBSpawnInterval = 6;

        public CompProperties_YinYangOrb Props => (CompProperties_YinYangOrb)props;
        public Vector3 VisualPos => visualPos;
        public float RotationAngle => rotationAngle;

        public void Init(IntVec3 target, Pawn caster)
        {
            this.targetCell = target;
            this.caster = caster;

            visualPos = parent.Position.ToVector3Shifted();
            moveDir = (target.ToVector3Shifted() - visualPos).normalized;
            rotationAngle = Rand.Range(0f, 360f);

            orbDrawSize = parent.def.graphicData.drawSize.x; // 8

            if (parent.Map != null)
            {
                effectA = (Mote_OrbEffectA)ThingMaker.MakeThing(ThingDef.Named("Mote_YinYangOrbEffectA"));
                GenSpawn.Spawn(effectA, parent.Position, parent.Map);
                effectA.AttachToOrb(this, orbDrawSize);
            }

            state = OrbState.Spawning;
            stateTick = 0;
            CurrentScale = 0f;
            CurrentAlpha = 1f;
        }

        public override void CompTick()
        {
            base.CompTick();

            if (parent.Map == null)
                return;

            stateTick++;

            rotationAngle += Props.rotateSpeed;
            if (rotationAngle > 360f) rotationAngle -= 360f;

            switch (state)
            {
                case OrbState.Spawning:
                    CurrentScale = Mathf.Clamp01((float)stateTick / SpawnDuration);
                    if (stateTick >= SpawnDuration)
                    {
                        CurrentScale = 1f;
                        state = OrbState.Alive;
                        stateTick = 0;
                    }
                    break;

                case OrbState.Alive:
                    CurrentScale = 1f;
                    CurrentAlpha = 1f;

                    if (!arrived)
                    {
                        visualPos += moveDir * Props.moveSpeed;
                        IntVec3 newCell = visualPos.ToIntVec3();
                        if (newCell != parent.Position && newCell.InBounds(parent.Map))
                            parent.Position = newCell;

                        if (parent.Position == targetCell)
                        {
                            arrived = true;
                            lingerLeft = Props.lingerTicks;
                        }
                    }
                    else
                    {
                        lingerLeft--;
                        if (lingerLeft <= 0)
                        {
                            state = OrbState.Fading;
                            stateTick = 0;
                            effectA?.StartFadeOut(FadeOutDuration);
                        }
                    }

                    DamageAndStunInRadius();

                    effectBTimer++;
                    if (effectBTimer >= EffectBSpawnInterval)
                    {
                        effectBTimer = 0;
                        SpawnEffectB();
                    }
                    break;

                case OrbState.Fading:
                    CurrentAlpha = 1f - Mathf.Clamp01((float)stateTick / FadeOutDuration);
                    if (stateTick >= FadeOutDuration)
                    {
                        state = OrbState.Dead;
                        ExplodeAndDestroy();
                    }
                    break;
            }

            if (effectA != null && !effectA.Destroyed)
            {
                effectA.UpdatePositionRotation(visualPos, rotationAngle, CurrentScale, CurrentAlpha);
            }
        }

        private void DamageAndStunInRadius()
        {
            Map map = parent.Map;
            if (map == null) return;

            var things = map.listerThings.AllThings
                .Where(t =>
                    t != parent &&
                    !t.Destroyed &&
                    t.Position.DistanceTo(parent.Position) <= Props.radius
                ).ToList();

            foreach (Thing t in things)
            {
                if (t == caster) continue;

                if (t is Pawn pawn)
                {
                    pawn.TakeDamage(new DamageInfo(DamageDefOf.Blunt, Props.damagePerTick, instigator: caster));

                    if (!pawn.Dead && pawn.stances?.stunner != null)
                    {
                        pawn.stances.stunner.StunFor(60, caster, false);
                    }
                }
                else if (t.def.category == ThingCategory.Building || t.def.category == ThingCategory.Item)
                {
                    t.TakeDamage(new DamageInfo(DamageDefOf.Blunt, Props.damagePerTick));
                }
            }
        }

        private void SpawnEffectB()
        {
            Map map = parent.Map;
            if (map == null) return;

            Mote_OrbEffectB moteB = (Mote_OrbEffectB)ThingMaker.MakeThing(ThingDef.Named("Mote_YinYangOrbEffectB"));
            GenSpawn.Spawn(moteB, visualPos.ToIntVec3(), map);
            moteB.Init(visualPos, rotationAngle, CurrentScale, orbDrawSize);
        }

        private void ExplodeAndDestroy()
        {
            parent.Destroy();
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);
            if (effectA != null && !effectA.Destroyed)
                effectA.Destroy();
        }
    }

    public class Mote_OrbEffectA : Thing
    {
        private Comp_YinYangOrb orbComp;
        private Vector3 position;
        private float rotation;
        private float scale = 1f;
        private float alpha = 1f;
        private float baseSize; 

        private bool fadingOut = false;
        private int fadeTick = 0;
        private int fadeDuration = 20;
        private float startScale = 1f;

        private static readonly Material MaterialA = MaterialPool.MatFrom(
            "Projectiles/YinYangFlyingBirdWell/OrbMoteA",
            ShaderDatabase.MoteGlow
        );

        public void AttachToOrb(Comp_YinYangOrb comp, float orbDrawSize)
        {
            orbComp = comp;
            baseSize = orbDrawSize;
        }

        public void UpdatePositionRotation(Vector3 pos, float rot, float currentOrbScale, float orbAlpha)
        {
            position = pos;
            rotation = rot;
            if (!fadingOut)
            {
                scale = currentOrbScale;
                alpha = orbAlpha;
            }
        }

        public void StartFadeOut(int duration)
        {
            fadingOut = true;
            fadeDuration = duration;
            fadeTick = 0;
            startScale = scale;
        }

        protected override void Tick()
        {
            if (fadingOut)
            {
                fadeTick++;
                float progress = Mathf.Clamp01((float)fadeTick / fadeDuration);
                scale = Mathf.Lerp(startScale, startScale * 2f, progress);
                alpha = 1f - progress;

                if (fadeTick >= fadeDuration)
                {
                    this.Destroy();
                }
            }
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            if (alpha <= 0f || scale <= 0f) return;

            Vector3 pos = position;
            pos.y = Altitudes.AltitudeFor(AltitudeLayer.MoteOverhead) - 0.1f;

            Material mat = FadedMaterialPool.FadedVersionOf(MaterialA, alpha);
            Quaternion rot = Quaternion.AngleAxis(rotation, Vector3.up);
            Vector3 finalScale = new Vector3(baseSize * scale, 1f, baseSize * scale);

            Matrix4x4 matrix = Matrix4x4.TRS(pos, rot, finalScale);
            Graphics.DrawMesh(MeshPool.plane10, matrix, mat, 0);
        }
    }
    public class Mote_OrbEffectB : Thing
    {
        private Vector3 position;
        private float rotation;
        private float targetScale;
        private float baseSize; 

        private int age = 0;
        private const int MaxAge = 20;
        private const float GrowEndProgress = 0.4f;

        private static readonly Material MaterialB = MaterialPool.MatFrom(
            "Projectiles/YinYangFlyingBirdWell/OrbMoteB",
            ShaderDatabase.MoteGlow
        );

        public void Init(Vector3 pos, float rot, float currentOrbScale, float orbDrawSize)
        {
            position = pos;
            rotation = rot;
            targetScale = currentOrbScale;
            baseSize = orbDrawSize;
        }

        protected override void Tick()
        {
            age++;
            if (age >= MaxAge)
                this.Destroy();
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            float progress = (float)age / MaxAge;
            float scale, alpha;

            if (progress <= GrowEndProgress)
            {
                float growProgress = progress / GrowEndProgress;
                scale = Mathf.Lerp(0f, targetScale, growProgress);
                alpha = 1f;
            }
            else
            {
                float fadeProgress = (progress - GrowEndProgress) / (1f - GrowEndProgress);
                scale = targetScale;
                alpha = 1f - fadeProgress;
            }

            Vector3 pos = position;
            pos.y = Altitudes.AltitudeFor(AltitudeLayer.MoteLow);

            Material mat = FadedMaterialPool.FadedVersionOf(MaterialB, alpha);
            Quaternion rot = Quaternion.AngleAxis(rotation, Vector3.up);
            Vector3 finalScale = new Vector3(baseSize * scale, 1f, baseSize * scale);

            Matrix4x4 matrix = Matrix4x4.TRS(pos, rot, finalScale);
            Graphics.DrawMesh(MeshPool.plane10, matrix, mat, 0);
        }
    }
}
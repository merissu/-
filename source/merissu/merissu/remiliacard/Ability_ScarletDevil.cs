using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using System.Collections.Generic;
using System.Linq;

namespace merissu
{
    public class Ability_ScarletDevil : Ability
    {
        public Ability_ScarletDevil() : base() { }

        public Ability_ScarletDevil(Pawn pawn, AbilityDef def)
            : base(pawn, def) { }

        public override AcceptanceReport CanCast
        {
            get
            {
                Hediff hp = pawn.health.hediffSet
                    .GetFirstHediffOfDef(HediffDef.Named("FullPower"));

                if (hp == null || hp.Severity < 1f)
                    return "灵力不足 (需要1层)";

                return AcceptanceReport.WasAccepted;
            }
        }

        public override bool Activate(LocalTargetInfo target, LocalTargetInfo dest)
        {
            Hediff hp = pawn.health.hediffSet
                .GetFirstHediffOfDef(HediffDef.Named("FullPower"));

            if (hp == null || hp.Severity < 1f)
                return false;

            hp.Severity -= 1f;

            bool result = base.Activate(target, dest);

            var anim = (Thing_ScarletDevilAnimation)
                ThingMaker.MakeThing(
                    ThingDef.Named("ScarletDevilAnimation"));

            anim.Init(pawn);

            GenSpawn.Spawn(anim, pawn.Position, pawn.Map);

            SoundDef.Named("ScarletDevil")
                .PlayOneShot(new TargetInfo(pawn.Position, pawn.Map));

            return result;
        }
    }

    public class Thing_ScarletDevilAnimation : Thing
    {
        private Pawn caster;
        private int age;

        private const int TotalFrames = 32;
        private const int TicksPerFrame = 2;
        private const int TotalDuration = 140;

        private const int LoopStart = 7;
        private const int LoopEnd = 18;

        private const int HaloFrames = 30;
        private const int HaloTicksPerFrame = 6;
        private const int HaloFadeTicks = 40;
        private const int HaloTotalDuration = TotalDuration + HaloFadeTicks;

        private const float HeadSize = 40f;
        private const float BeamLength = 250f;
        private const float HaloSize = 35f;
        private const float Height = 1f;

        private const int DamageInterval = 3;
        private const float DamageAmount = 20f;

        private static Mesh _quadMesh;
        private static Mesh QuadMesh
        {
            get
            {
                if (_quadMesh == null)
                {
                    _quadMesh = new Mesh { name = "ScarletDevil_QuadMesh" };

                    _quadMesh.vertices = new Vector3[]
                    {
                        new Vector3(-0.5f,0,-0.5f),
                        new Vector3(0.5f,0,-0.5f),
                        new Vector3(-0.5f,0,0.5f),
                        new Vector3(0.5f,0,0.5f)
                    };

                    _quadMesh.uv = new Vector2[]
                    {
                        new Vector2(0.01f,0.01f),
                        new Vector2(0.99f,0.01f),
                        new Vector2(0.01f,0.99f),
                        new Vector2(0.99f,0.99f)
                    };

                    _quadMesh.triangles = new int[] { 0, 2, 1, 1, 2, 3 };
                    _quadMesh.RecalculateNormals();
                }
                return _quadMesh;
            }
        }

        private static Mesh _beamMesh;
        private static Mesh BeamMesh
        {
            get
            {
                if (_beamMesh == null)
                {
                    _beamMesh = new Mesh { name = "ScarletDevil_BeamMesh" };

                    _beamMesh.vertices = new Vector3[]
                    {
                        new Vector3(-0.5f,0,0f),
                        new Vector3(0.5f,0,0f),
                        new Vector3(-0.5f,0,1f),
                        new Vector3(0.5f,0,1f)
                    };

                    _beamMesh.uv = new Vector2[]
                    {
                        new Vector2(0.01f,0.995f),
                        new Vector2(0.99f,0.995f),
                        new Vector2(0.01f,0.995f),
                        new Vector2(0.99f,0.995f)
                    };

                    _beamMesh.triangles = new int[] { 0, 2, 1, 1, 2, 3 };
                    _beamMesh.RecalculateNormals();
                }
                return _beamMesh;
            }
        }

        public void Init(Pawn pawn)
        {
            caster = pawn;
        }

        protected override void Tick()
        {
            base.Tick();
            age++;

            if (caster == null || caster.Destroyed)
            {
                Destroy();
                return;
            }

            Position = caster.Position;

            if (age % DamageInterval == 0)
            {
                DoScarletBurnDamage();
            }

            if (age >= HaloTotalDuration)
                Destroy();
        }

        private void DoScarletBurnDamage()
        {
            Map map = caster.Map;
            if (map == null) return;

            IntVec3 center = caster.Position;

            List<Pawn> targets = map.mapPawns.AllPawnsSpawned.ToList();

            foreach (Pawn target in targets)
            {
                if (target == caster) continue;
                if (target.Dead) continue;
                if (target.Faction == caster.Faction) continue;

                bool inCircle = target.Position.DistanceTo(center) <= 20f;

                bool inVerticalBeam =
                    Mathf.Abs(target.Position.x - center.x) <= 10 &&
                    target.Position.z >= center.z;

                if (inCircle || inVerticalBeam)
                {
                    DamageInfo dinfo = new DamageInfo(
                        DamageDefOf.Flame,
                        DamageAmount,
                        0f,
                        -1f,
                        caster);

                    target.TakeDamage(dinfo);
                }
            }
        }


        private int GetMainFrame()
        {
            int rawFrame = age / TicksPerFrame;

            int introLength = LoopStart;
            int loopLength = LoopEnd - LoopStart + 1;
            int outroLength = TotalFrames - LoopEnd - 1;
            int totalFramesPlayed = TotalDuration / TicksPerFrame;

            if (rawFrame < introLength)
                return rawFrame;

            if (rawFrame < totalFramesPlayed - outroLength)
            {
                int loopIndex = (rawFrame - introLength) % loopLength;
                return LoopStart + loopIndex;
            }

            int outroFrame = rawFrame - (totalFramesPlayed - outroLength);
            return LoopEnd + 1 + Mathf.Clamp(outroFrame, 0, outroLength - 1);
        }

        private int GetHaloFrame()
        {
            return (age / HaloTicksPerFrame) % HaloFrames;
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            if (caster == null) return;

            Vector3 basePos = caster.DrawPos;

            int haloFrame = GetHaloFrame();
            string haloPath = $"Other/ScarletDevilAnimation/spellBulletAb{haloFrame:D3}";
            Material haloMat = MaterialPool.MatFrom(haloPath, ShaderDatabase.MoteGlow);

            float alpha = 1f;

            if (age > TotalDuration)
            {
                float progress = (age - TotalDuration) / (float)HaloFadeTicks;
                alpha = 1f - progress;
            }

            haloMat.color = new Color(1f, 1f, 1f, alpha);

            Vector3 haloPos = basePos;
            haloPos.y = AltitudeLayer.MetaOverlays.AltitudeFor();

            Matrix4x4 haloMatrix = Matrix4x4.TRS(
                haloPos,
                Quaternion.identity,
                new Vector3(HaloSize, 1f, HaloSize));

            Graphics.DrawMesh(QuadMesh, haloMatrix, haloMat, 0);

            if (age < TotalDuration)
            {
                int frame = GetMainFrame();
                string texPath = $"Other/ScarletDevil/spellBulletAa{frame:D3}";
                Material mat = MaterialPool.MatFrom(texPath, ShaderDatabase.MoteGlow);

                Vector3 mainPos = basePos;
                mainPos.y = AltitudeLayer.MoteOverhead.AltitudeFor();

                Matrix4x4 headMatrix = Matrix4x4.TRS(
                    mainPos + new Vector3(0, 0, Height),
                    Quaternion.identity,
                    new Vector3(HeadSize, 1f, HeadSize));

                Graphics.DrawMesh(QuadMesh, headMatrix, mat, 0);

                Vector3 beamPos = mainPos + new Vector3(0, 0, Height + HeadSize / 2f);

                Matrix4x4 beamMatrix = Matrix4x4.TRS(
                    beamPos,
                    Quaternion.identity,
                    new Vector3(HeadSize, 1f, BeamLength));

                Graphics.DrawMesh(BeamMesh, beamMatrix, mat, 0);
            }
        }
    }
}
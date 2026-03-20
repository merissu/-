using RimWorld;
using Verse;
using UnityEngine;
using System.Collections.Generic;

namespace merissu
{
    public class Thing_FlowingArray : Thing
    {
        private Pawn caster;
        private int age;

        private const int Duration = 300;      
        private const int SquareRadius = 10;   

        private const float FlowSpeed = 0.01f;      
        private const float Alpha = 0.8f;           
        private const float GlowAlphaMax = 1f;      
        private const float GlowAnimSpeed = 0.02f;  

        private float uvOffset = 0f;               
        private Material flowMat;
        private Material glowMat;                   

        public void Init(Pawn pawn)
        {
            caster = pawn;

            flowMat = MaterialPool.MatFrom("Projectiles/FlowingArray", ShaderDatabase.MoteGlow);
            flowMat.color = new Color(1f, 1f, 1f, Alpha);

            glowMat = MaterialPool.MatFrom("Projectiles/FlowingArray2", ShaderDatabase.MoteGlow);
            glowMat.color = new Color(1f, 1f, 0f, GlowAlphaMax);
        }

        protected override void Tick()
        {
            base.Tick();
            age++;

            uvOffset += FlowSpeed;
            if (uvOffset > 1f) uvOffset -= 1f;

            AffectEnemies();

            BlockProjectiles();

            if (caster == null || caster.Dead || age > Duration)
            {
                EndArray();
            }
        }

        private void EndArray()
        {
            Hediff h = caster.health.hediffSet.GetFirstHediffOfDef(
                HediffDef.Named("EightDragonCasterLock"));
            if (h != null) caster.health.RemoveHediff(h);

            Destroy();
        }

        private void AffectEnemies()
        {
            if (caster == null || caster.Map == null) return;

            IntVec3 center = caster.Position;

            for (int x = -SquareRadius; x < SquareRadius; x++)
            {
                for (int z = -SquareRadius; z < SquareRadius; z++)
                {
                    IntVec3 c = center + new IntVec3(x, 0, z);
                    if (!c.InBounds(Map)) continue;

                    List<Thing> list = c.GetThingList(Map);
                    for (int i = list.Count - 1; i >= 0; i--)
                    {
                        if (list[i] is Pawn p &&
                            !p.Dead &&
                            p.Faction != caster.Faction)
                        {
                            p.TakeDamage(new DamageInfo(DamageDefOf.Flame, 3f));

                            if (!p.health.hediffSet.HasHediff(HediffDef.Named("EightDragonBurnLock")))
                                p.health.AddHediff(HediffDef.Named("EightDragonBurnLock"));
                        }
                    }
                }
            }
        }

        private void BlockProjectiles()
        {
            if (caster == null || Map == null) return;

            IntVec3 center = caster.Position;

            for (int x = -SquareRadius; x < SquareRadius; x++)
            {
                for (int z = -SquareRadius; z < SquareRadius; z++)
                {
                    IntVec3 c = center + new IntVec3(x, 0, z);
                    if (!c.InBounds(Map)) continue;

                    List<Thing> list = c.GetThingList(Map);
                    for (int i = list.Count - 1; i >= 0; i--)
                    {
                        if (list[i] is Projectile proj)
                        {
                            proj.Destroy();
                        }
                    }
                }
            }
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            base.DrawAt(drawLoc, flip);

            Vector3 pos = drawLoc + new Vector3(0f, 0.01f, 0f);
            Vector3 scale = new Vector3(SquareRadius * 2f, 1f, SquareRadius * 2f);

            flowMat.mainTextureOffset = new Vector2(0f, -uvOffset);
            Matrix4x4 matrix = Matrix4x4.TRS(pos, Quaternion.identity, scale);
            Graphics.DrawMesh(MeshPool.plane10, matrix, flowMat, 0);

            float alphaAnim = Mathf.PingPong(age * GlowAnimSpeed, GlowAlphaMax);
            glowMat.color = new Color(1f, 1f, 0f, alphaAnim);

            float glowScaleFactor = 1.1f;
            Matrix4x4 glowMatrix = Matrix4x4.TRS(pos, Quaternion.identity, scale * glowScaleFactor);
            Graphics.DrawMesh(MeshPool.plane10, glowMatrix, glowMat, 0);
        }
    }
}

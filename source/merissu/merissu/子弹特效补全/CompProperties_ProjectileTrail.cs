using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace merissu
{
    public class CompProperties_ProjectileTrail : CompProperties
    {
        public string texPath;
        public int trailLifespan = 15;      
        public int spawnInterval = 1;       
        public Vector2 drawSize = new Vector2(0.8f, 0.8f);

        public CompProperties_ProjectileTrail()
        {
            this.compClass = typeof(Comp_ProjectileTrail);
        }
    }

    public struct TrailPoint
    {
        public Vector3 position;
        public float rotation;
        public int age;
    }

    public class Comp_ProjectileTrail : ThingComp
    {
        private List<TrailPoint> trailPoints = new List<TrailPoint>();
        private Material trailMat;
        private static MaterialPropertyBlock propBlock = new MaterialPropertyBlock();

        public CompProperties_ProjectileTrail Props => (CompProperties_ProjectileTrail)this.props;

        public override void CompTick()
        {
            base.CompTick();

            for (int i = trailPoints.Count - 1; i >= 0; i--)
            {
                TrailPoint p = trailPoints[i];
                p.age++;

                if (p.age >= Props.trailLifespan)
                {
                    trailPoints.RemoveAt(i);
                }
                else
                {
                    trailPoints[i] = p; 
                }
            }

            if (Find.TickManager.TicksGame % Props.spawnInterval == 0)
            {
                if (parent is Projectile proj)
                {
                    trailPoints.Add(new TrailPoint
                    {
                        position = proj.DrawPos,
                        rotation = proj.ExactRotation.eulerAngles.y,
                        age = 0
                    });
                }
            }
        }

        public override void PostDraw()
        {
            base.PostDraw();

            if (trailPoints.Count == 0) return;

            if (trailMat == null)
            {
                trailMat = MaterialPool.MatFrom(Props.texPath, ShaderDatabase.Transparent);
            }

            Vector3 size = new Vector3(Props.drawSize.x, 1f, Props.drawSize.y);

            float baseAlt = AltitudeLayer.Projectile.AltitudeFor() - 0.05f;

            for (int i = 0; i < trailPoints.Count; i++)
            {
                TrailPoint p = trailPoints[i];

                float alpha = Mathf.Lerp(1f, 0f, (float)p.age / Props.trailLifespan);
                propBlock.SetColor(ShaderPropertyIDs.Color, new Color(1f, 1f, 1f, alpha));

                Vector3 drawPos = p.position;
                drawPos.y = baseAlt - (p.age * 0.001f);

                Matrix4x4 matrix = Matrix4x4.TRS(drawPos, Quaternion.Euler(0, p.rotation, 0), size);

                Graphics.DrawMesh(
                    MeshPool.plane10,
                    matrix,
                    trailMat,
                    0,
                    null,
                    0,
                    propBlock
                );
            }
        }
    }
}
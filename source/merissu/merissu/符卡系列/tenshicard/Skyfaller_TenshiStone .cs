using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace merissu
{
    public class Skyfaller_TenshiStone : Skyfaller
    {
        protected override void Impact()
        {
            if (Map != null)
            {
                Vector3 impactPos = this.DrawPos;
                int debrisCount = Rand.RangeInclusive(24, 36);  

                string[] defNames = new string[]
                {
                    "Mote_RockDebris_A",
                    "Mote_RockDebris_B",
                    "Mote_RockDebris_C",
                    "Mote_RockDebris_D"
                };

                for (int i = 0; i < debrisCount; i++)
                {
                    string chosenDef = defNames[Rand.Range(0, defNames.Length)];
                    ThingDef debrisDef = ThingDef.Named(chosenDef);
                    if (debrisDef == null) continue;

                    Mote_RockDebris debris = (Mote_RockDebris)ThingMaker.MakeThing(debrisDef);
                    GenSpawn.Spawn(debris, impactPos.ToIntVec3(), Map);
                    debris.Initialize(impactPos);
                }
            }

            base.Impact(); 
        }
    }

    [StaticConstructorOnStartup]
    public class Mote_RockDebris : Thing
    {
        private Vector3 pos;
        private Vector3 vel;
        private float rot;
        private float rotSpeed;
        private float size;
        private int age;
        private int maxAge;
        private Material mat;
        private static readonly MaterialPropertyBlock propBlock = new MaterialPropertyBlock();

        public void Initialize(Vector3 origin)
        {
            pos = origin;
            pos.y = AltitudeLayer.MoteOverhead.AltitudeFor() + Rand.Range(2f, 5f);

            float angle = Rand.Range(0f, 360f) * Mathf.Deg2Rad;
            float speed = Rand.Range(0.22f, 0.42f);   
            vel = new Vector3(Mathf.Cos(angle) * speed, Rand.Range(0.18f, 0.4f), Mathf.Sin(angle) * speed);

            rot = Rand.Range(0f, 360f);
            rotSpeed = Rand.Range(-25f, 25f);         

            size = GetSizeByDef();

            maxAge = Rand.RangeInclusive(70, 100);
            age = 0;
        }

        private float GetSizeByDef()
        {
            if (def == null) return 1.5f;
            switch (def.defName)
            {
                case "Mote_RockDebris_A": return Rand.Range(2.5f, 3.8f);   
                case "Mote_RockDebris_D": return Rand.Range(0.6f, 1.0f);   
                default: return Rand.Range(1.2f, 2.0f);   
            }
        }

        protected override void Tick()
        {
            if (age >= maxAge)
            {
                Destroy();
                return;
            }

            age++;
            pos += vel;
            vel.y -= 0.0025f;       
            rot += rotSpeed;
            rotSpeed *= 0.98f;      
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            if (mat == null && def?.graphicData != null)
            {
                mat = MaterialPool.MatFrom(def.graphicData.texPath, ShaderDatabase.Mote);
            }
            if (mat == null) return;

            float alpha = 1f - (float)age / maxAge;
            if (alpha <= 0f) return;

            propBlock.SetColor("_Color", new Color(1f, 1f, 1f, alpha));

            Vector3 renderPos = pos;
            Matrix4x4 matrix = Matrix4x4.TRS(
                renderPos,
                Quaternion.AngleAxis(rot, Vector3.up),
                new Vector3(size, 1f, size)
            );

            Graphics.DrawMesh(MeshPool.plane10, matrix, mat, 0, null, 0, propBlock);
        }
    }
}
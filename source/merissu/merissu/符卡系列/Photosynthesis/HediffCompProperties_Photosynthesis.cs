using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;

namespace merissu
{
    public class HediffCompProperties_Photosynthesis : HediffCompProperties
    {
        public ThingDef moteDef;
        public float radius = 1.9f;
        public float healAmount = 1.0f;
        public float spiritualPowerGain = 0.05f;
        public int tickInterval = 60;

        public HediffCompProperties_Photosynthesis()
        {
            this.compClass = typeof(HediffComp_Photosynthesis);
        }
    }

    public class HediffComp_Photosynthesis : HediffComp
    {
        public HediffCompProperties_Photosynthesis Props => (HediffCompProperties_Photosynthesis)props;
        private Mote_PhotosynthesisPillar pillarMote;

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            if (Pawn == null || !Pawn.Spawned || Pawn.Dead) return;

            if (pillarMote == null || pillarMote.Destroyed)
            {
                SpawnPillarMote();
            }
            else
            {
                pillarMote.Maintain();
            }

            if (Pawn.IsHashIntervalTick(Props.tickInterval))
            {
                ApplyAreaEffect();
            }
        }

        private void SpawnPillarMote()
        {
            if (Props.moteDef != null && Pawn.Map != null)
            {
                pillarMote = (Mote_PhotosynthesisPillar)ThingMaker.MakeThing(Props.moteDef);
                pillarMote.target = Pawn;
                GenSpawn.Spawn(pillarMote, Pawn.Position, Pawn.Map);
            }
        }

        private void ApplyAreaEffect()
        {
            Map map = Pawn.Map;
            Vector3 center = pillarMote != null ? pillarMote.exactPosition : Pawn.DrawPos;
            IntVec3 cellCenter = center.ToIntVec3();

            int cellCount = GenRadial.NumCellsInRadius(Props.radius);
            for (int i = 0; i < cellCount; i++)
            {
                IntVec3 targetCell = cellCenter + GenRadial.RadialPattern[i];
                if (!targetCell.InBounds(map)) continue;

                List<Thing> things = targetCell.GetThingList(map);
                for (int j = 0; j < things.Count; j++)
                {
                    if (things[j] is Pawn p && !p.Dead)
                    {
                        if (p.Faction == Faction.OfPlayer || (p.Faction != null && !p.Faction.HostileTo(Faction.OfPlayer)))
                        {
                            ApplyHealingAndBuff(p);
                        }
                    }
                }
            }
        }

        private void ApplyHealingAndBuff(Pawn p)
        {
            var injury = p.health.hediffSet.hediffs
                .OfType<Hediff_Injury>()
                .FirstOrDefault(h => h.CanHealNaturally() && h.Severity > 0);

            if (injury != null) injury.Heal(Props.healAmount);

            HediffDef spDef = DefDatabase<HediffDef>.GetNamedSilentFail("spiritualpower");
            if (spDef != null)
            {
                Hediff spHediff = p.health.hediffSet.GetFirstHediffOfDef(spDef);
                if (spHediff == null)
                {
                    spHediff = p.health.AddHediff(spDef);
                    spHediff.Severity = Props.spiritualPowerGain;
                }
                else
                {
                    spHediff.Severity += Props.spiritualPowerGain;
                }
            }
        }

        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();
            if (pillarMote != null && !pillarMote.Destroyed)
            {
                pillarMote.StartFadeOut();
            }
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_References.Look(ref pillarMote, "pillarMote");
        }
    }

    public class Mote_PhotosynthesisPillar : Mote
    {
        public Pawn target;
        private Vector3 currentPos;
        private float followSpeed = 0.05f;

        private bool isEnding = false;
        private int endingTick = -1;

        private Mesh customMesh;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if (!respawningAfterLoad)
            {
                if (target != null) currentPos = target.DrawPos;
                else currentPos = this.Position.ToVector3Shifted();
            }
        }

        public void StartFadeOut()
        {
            if (!isEnding)
            {
                isEnding = true;
                endingTick = Find.TickManager.TicksGame;
            }
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            base.Destroy(mode);
            if (customMesh != null)
            {
                UnityEngine.Object.Destroy(customMesh);
                customMesh = null;
            }
        }

        protected override void Tick()
        {
            base.Tick();
            if (target != null && target.Spawned)
            {
                currentPos = Vector3.Lerp(currentPos, target.DrawPos, followSpeed);
            }
            this.exactPosition = currentPos;
        }

        private void UpdateMesh(float width, float bottomHeight, float totalHeight)
        {
            if (customMesh == null)
            {
                customMesh = new Mesh();
                customMesh.name = "PhotosynthesisPillarMesh";
                customMesh.MarkDynamic(); 

                Vector2[] uvs = new Vector2[6];
                float vBottom = 0.015f;
                float vMid = 0.25f; 
                float vTop = 1.0f;  

                uvs[0] = new Vector2(0, vBottom);
                uvs[1] = new Vector2(1, vBottom);
                uvs[2] = new Vector2(0, vMid);
                uvs[3] = new Vector2(1, vMid);
                uvs[4] = new Vector2(0, vTop);
                uvs[5] = new Vector2(1, vTop);

                int[] triangles = new int[] {
                    0, 2, 1, 1, 2, 3, 
                    2, 4, 3, 3, 4, 5  
                };

                customMesh.vertices = new Vector3[6];
                customMesh.uv = uvs;
                customMesh.triangles = triangles;
            }

            Vector3[] verts = customMesh.vertices;
            verts[0] = new Vector3(-width / 2f, 0, 0);
            verts[1] = new Vector3(width / 2f, 0, 0);
            verts[2] = new Vector3(-width / 2f, 0, bottomHeight);
            verts[3] = new Vector3(width / 2f, 0, bottomHeight);
            verts[4] = new Vector3(-width / 2f, 0, totalHeight); 
            verts[5] = new Vector3(width / 2f, 0, totalHeight); 

            customMesh.vertices = verts;
            customMesh.RecalculateBounds();
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            if (!this.Spawned) return;

            float currentAlpha = this.Alpha;

            if (isEnding)
            {
                float ticksSinceEnd = Find.TickManager.TicksGame - endingTick;
                float fadeDurationTicks = this.def.mote.fadeOutTime * 60f;
                float endFade = 1f - (ticksSinceEnd / fadeDurationTicks);

                if (endFade <= 0f)
                {
                    this.Destroy(); 
                    return;
                }
                currentAlpha = Mathf.Min(currentAlpha, endFade);
            }

            if (currentAlpha <= 0f) return;

            float width = 3f;
            float naturalHeight = width * 3.75f; 
            float bottomHeight = naturalHeight * 0.25f; 

            Vector3 adjustedPos = currentPos;
            adjustedPos.y = AltitudeLayer.MetaOverlays.AltitudeFor() + 0.05f;
            adjustedPos.z -= 1f;

            float pawnBottomZ = adjustedPos.z;
            float cameraMaxZ = Find.CameraDriver.CurrentViewRect.maxZ + 5f; 
            float requiredTotalHeight = cameraMaxZ - pawnBottomZ;

            float totalHeight = Mathf.Max(naturalHeight, requiredTotalHeight);

            UpdateMesh(width, bottomHeight, totalHeight);

            Matrix4x4 matrix = default;
            matrix.SetTRS(adjustedPos, Quaternion.identity, Vector3.one);

            Material fadedMat = FadedMaterialPool.FadedVersionOf(this.Graphic.MatSingle, currentAlpha);
            Graphics.DrawMesh(customMesh, matrix, fadedMat, 0);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref target, "target");
            Scribe_Values.Look(ref currentPos, "currentPos");
            Scribe_Values.Look(ref isEnding, "isEnding");
            Scribe_Values.Look(ref endingTick, "endingTick");
        }
    }
}
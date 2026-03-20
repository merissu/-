using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;
using Verse.Sound;

namespace merissu
{
    public class Ability_Trainbomb : Ability
    {
        public Ability_Trainbomb() : base() { }

        public Ability_Trainbomb(Pawn pawn, AbilityDef def) : base(pawn, def) { }

        public override AcceptanceReport CanCast
        {
            get
            {
                Hediff hp = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("FullPower"));

                if (hp == null || hp.Severity < 1f)
                {
                    return "灵力不足";
                }

                return AcceptanceReport.WasAccepted;
            }
        }

        public override bool Activate(LocalTargetInfo target, LocalTargetInfo dest)
        {
            if (pawn == null || pawn.Map == null)
                return false;

            Hediff hp = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("FullPower"));
            if (hp != null)
            {
                hp.Severity -= 1f; 
            }

            SoundDef.Named("Train").PlayOneShot(new TargetInfo(pawn.Position, pawn.Map));

            Vector3 dir = (target.Cell - pawn.Position).ToVector3();
            if (dir.sqrMagnitude < 0.01f)
                dir = pawn.Rotation.FacingCell.ToVector3();

            dir.Normalize();

            IntVec3 spawnCell = pawn.Position + dir.ToIntVec3();
            Thing portalThing = ThingMaker.MakeThing(ThingDef.Named("TrainGapPortal"));

            if (portalThing is Thing_TrainGapPortal portal)
            {
                portal.direction = dir;
                portal.caster = pawn;
                GenSpawn.Spawn(portal, spawnCell, pawn.Map);
            }

            return true;
        }
    }
    public class Thing_TrainGapPortal : Thing
    {
        public Pawn caster;
        public Vector3 direction = Vector3.forward;

        private int age;
        private int trainsSpawnedCount = 0;
        private int nextTrainTick = -1;
        private bool isClosing = false;
        private int closeStartTick = -1;

        private Sustainer sustainer;
        private List<Thing_MovingTrain> activeTrains = new List<Thing_MovingTrain>();

        private const int MaxTrains = 6;
        private const int ExpandDuration = 15;
        private const int ShrinkDuration = 10;
        private const int TicksPerFrame = 4;
        private const int TotalFrames = 16;
        private const float FinalWidth = 5f;
        private const float Height = 10f;
        private const int TicksBetweenTrains = 28;

        private static Mesh _mesh;
        public static Mesh PortalMesh
        {
            get
            {
                if (_mesh == null)
                {
                    _mesh = new Mesh { name = "TrainGapPortal_Mesh" };
                    _mesh.vertices = new Vector3[] {
                        new Vector3(-0.5f,0,-0.5f), new Vector3(0.5f,0,-0.5f),
                        new Vector3(-0.5f,0,0.5f), new Vector3(0.5f,0,0.5f)
                    };
                    _mesh.uv = new Vector2[] {
                        new Vector2(0.01f,0.01f), new Vector2(0.99f,0.01f),
                        new Vector2(0.01f,0.99f), new Vector2(0.99f,0.99f)
                    };
                    _mesh.triangles = new int[] { 0, 2, 1, 1, 2, 3 };
                    _mesh.RecalculateNormals();
                }
                return _mesh;
            }
        }

        protected override void Tick()
        {
            base.Tick();
            if (!this.Spawned) return;
            age++;

            if (!isClosing && age >= ExpandDuration && trainsSpawnedCount < MaxTrains)
            {
                if (nextTrainTick == -1 || age >= nextTrainTick)
                {
                    SpawnTrain();
                    trainsSpawnedCount++;
                    nextTrainTick = age + TicksBetweenTrains;
                }
            }

            activeTrains.RemoveAll(t => t == null || t.Destroyed);

            if (activeTrains.Count > 0)
            {
                if (sustainer == null || sustainer.Ended)
                {
                    SoundDef trainMovingSound = SoundDef.Named("TrainMoving");
                    if (trainMovingSound != null)
                    {
                        sustainer = trainMovingSound.TrySpawnSustainer(SoundInfo.InMap(activeTrains[0], MaintenanceType.PerTick));
                    }
                }
                else
                {
                    sustainer.Maintain();

                }
            }
            else if (trainsSpawnedCount >= MaxTrains)
            {
                if (sustainer != null && !sustainer.Ended)
                {
                    sustainer.End();
                    sustainer = null;
                }
            }

            if (!isClosing && trainsSpawnedCount >= MaxTrains)
            {
                if (age > nextTrainTick + 5)
                {
                    isClosing = true;
                    closeStartTick = age;
                }
            }

            if (isClosing && age > closeStartTick + ShrinkDuration)
            {
                if (activeTrains.Count == 0)
                {
                    Destroy();
                }
            }
        }

        private void SpawnTrain()
        {
            Thing train = ThingMaker.MakeThing(ThingDef.Named("TrainMoving"));
            if (train is Thing_MovingTrain movingTrain)
            {
                movingTrain.direction = this.direction;
                movingTrain.caster = this.caster;
                GenSpawn.Spawn(movingTrain, this.Position, this.Map);
                activeTrains.Add(movingTrain); 
            }
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            if (sustainer != null && !sustainer.Ended)
            {
                sustainer.End();
            }
            base.Destroy(mode);
        }

        private float GetCurrentWidth()
        {
            if (isClosing)
            {
                float progress = (float)(age - closeStartTick) / ShrinkDuration;
                return Mathf.Lerp(FinalWidth, 0f, progress);
            }
            if (age < ExpandDuration)
            {
                return Mathf.Lerp(0f, FinalWidth, (float)age / ExpandDuration);
            }
            return FinalWidth;
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            if (!this.Spawned) return;
            float width = GetCurrentWidth();
            if (width <= 0.001f) return;

            Vector3 pos = drawLoc;
            pos.y = AltitudeLayer.MoteOverhead.AltitudeFor() - 0.1f;
            Quaternion rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(0, -90, 0);

            int frame = (age % (TicksPerFrame * TotalFrames)) / TicksPerFrame;
            Material mat = MaterialPool.MatFrom($"Other/gap/bulletEa{frame:D3}", ShaderDatabase.MoteGlow);

            Matrix4x4 matrix = Matrix4x4.TRS(pos, rotation, new Vector3(width, 1f, Height));
            Graphics.DrawMesh(PortalMesh, matrix, mat, 0);
        }
    }

    public class Thing_MovingTrain : Thing
    {
        public Vector3 direction;
        public Pawn caster;
        public float distanceTravelled = 0f; 
        private int age;
        private int toughObjectsDestroyed = 0;

        private const float Speed = 0.6f;
        private const float TrainWidth = 7.5f;
        private const float TrainLength = 19.99f;
        private const int MaxAge = 500;
        private const int MaxToughObjects = 25;

        private Mesh _dynamicMesh;
        private Mesh DynamicMesh => _dynamicMesh ?? (_dynamicMesh = new Mesh { name = "Train_Mesh" });

        protected override void Tick()
        {
            if (!this.Spawned) return;
            base.Tick();
            age++;
            distanceTravelled += Speed;
            if (age % 2 == 0) DoCollision();

            if (!this.Spawned) return;

            if (age > MaxAge || (!this.Position.InBounds(this.Map) && distanceTravelled > TrainLength))
            {
                this.Destroy();
            }
        }

        private void DoCollision()
        {
            Map currentMap = this.Map;
            if (currentMap == null || !this.Spawned) return;

            Vector3 headPos = this.Position.ToVector3Shifted() + direction * distanceTravelled;
            for (float offset = 0; offset < TrainLength; offset += 3f)
            {
                if (offset > distanceTravelled) break;
                Vector3 currentCheckPos = headPos - (direction * offset);
                IntVec3 centerCell = currentCheckPos.ToIntVec3();
                if (!centerCell.InBounds(currentMap)) continue;

                IEnumerable<IntVec3> affectedCells = GenRadial.RadialCellsAround(centerCell, 2.5f, true);
                foreach (var cell in affectedCells)
                {
                    if (!cell.InBounds(currentMap)) continue;
                    List<Thing> targets = cell.GetThingList(currentMap).ToList();
                    for (int i = targets.Count - 1; i >= 0; i--)
                    {
                        Thing t = targets[i];
                        if (t == null || t.Destroyed || !t.Spawned || t.def == null || t == caster || t is Thing_MovingTrain)
                            continue;

                        if (t is Pawn p)
                        {
                            if (!p.Dead)
                            {
                                p.TakeDamage(new DamageInfo(DamageDefOf.Blunt, 500, 2f, -1, this));
                                PushPawn(p);
                            }
                        }
                        else if (t is Plant plant)
                        {
                            plant.Destroy(DestroyMode.Vanish);
                        }
                        else if (t is Building b)
                        {
                            if (!b.def.destroyable) continue;

                            if (b.def.useHitPoints && b.MaxHitPoints >= 300)
                            {
                                toughObjectsDestroyed++;
                                if (toughObjectsDestroyed >= MaxToughObjects)
                                {
                                    ExplodeAndDestroy(currentMap);
                                    return;
                                }
                            }
                            if (!b.Destroyed)
                            {
                                b.Destroy(DestroyMode.Deconstruct);
                            }
                        }
                    }
                }
            }
        }

        private void ExplodeAndDestroy(Map map)
        {
            if (map == null) return;
            IntVec3 explosionPos = (this.Position.ToVector3Shifted() + direction * distanceTravelled).ToIntVec3();
            if (explosionPos.InBounds(map))
            {
                GenExplosion.DoExplosion(explosionPos, map, 4.9f, DamageDefOf.Bomb, this, 100);
            }
            if (this.Spawned) this.Destroy();
        }

        private void PushPawn(Pawn p)
        {
            if (p == null || !p.Spawned || p.Map == null) return;
            IntVec3 pushDest = p.Position + (direction * 2f).ToIntVec3();
            if (pushDest.InBounds(p.Map) && pushDest.Walkable(p.Map))
            {
                p.Position = pushDest;
            }
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            if (!this.Spawned || this.Map == null) return;
            float d = distanceTravelled;
            float L = TrainLength;
            float W = TrainWidth;
            float currentVisibleLength = Mathf.Min(d, L);
            if (currentVisibleLength <= 0.05f) return;

            bool isHeadingRight = direction.x > 0;
            Vector3[] vertices = new Vector3[] {
                new Vector3(-W/2, 0, 0), new Vector3(W/2, 0, 0),
                new Vector3(-W/2, 0, currentVisibleLength), new Vector3(W/2, 0, currentVisibleLength)
            };
            float uvTail = 1.0f - (currentVisibleLength / L);
            Vector2[] uvs = isHeadingRight ?
                new Vector2[] { new Vector2(uvTail, 1), new Vector2(uvTail, 0), new Vector2(1, 1), new Vector2(1, 0) } :
                new Vector2[] { new Vector2(uvTail, 0), new Vector2(uvTail, 1), new Vector2(1, 0), new Vector2(1, 1) };

            Mesh mesh = DynamicMesh;
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = new int[] { 0, 2, 1, 1, 2, 3 };
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();

            Vector3 posShifted = this.Position.ToVector3Shifted();
            Vector3 renderOrigin = (d < L) ? posShifted : posShifted + direction * (d - L);
            renderOrigin.y = AltitudeLayer.MoteOverhead.AltitudeFor() + 0.1f;
            Graphics.DrawMesh(mesh, renderOrigin, Quaternion.LookRotation(direction), MaterialPool.MatFrom("Other/objectCa000", ShaderDatabase.Cutout), 0);
        }
    }
}
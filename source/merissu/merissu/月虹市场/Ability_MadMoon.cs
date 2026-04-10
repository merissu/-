using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace merissu
{
    public class Ability_MadMoon : Ability
    {
        public Ability_MadMoon() : base() { }
        public Ability_MadMoon(Pawn pawn) : base(pawn) { }
        public Ability_MadMoon(Pawn pawn, AbilityDef def) : base(pawn, def) { }

        public override bool Activate(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Activate(target, dest);
            Pawn pawn = this.pawn;
            if (pawn == null || pawn.Map == null) return false;

            ThingDef projectileDef = ThingDef.Named("Projectile_MadMoon");
            Projectile_MadMoon moon = (Projectile_MadMoon)GenSpawn.Spawn(projectileDef, pawn.Position, pawn.Map);
            moon.InitializeMoon(pawn, pawn.DrawPos, target.CenterVector3);

            return true;
        }
    }

    [StaticConstructorOnStartup]
    public class Projectile_MadMoon : ThingWithComps
    {
        private float rotationAngle = 0f;
        private const float RotationSpeed = 5f;
        private Vector3 startPosition;
        private Vector3 direction;
        private bool isEnding = false;
        private float endLerp = 1f;
        private float currentDist = 0f;
        public Pawn launcher;

        private const float MaxDistance = 35f;
        private const float FadeOutTicks = 30f;
        private const float MoveSpeedPerTick = 0.25f; 

        public void InitializeMoon(Pawn launcher, Vector3 origin, Vector3 targetPos)
        {
            this.launcher = launcher;
            this.startPosition = origin;
            this.direction = (targetPos - origin).normalized;
            this.direction.y = 0; 
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref rotationAngle, "rotationAngle", 0f);
            Scribe_Values.Look(ref startPosition, "startPosition");
            Scribe_Values.Look(ref direction, "direction");
            Scribe_Values.Look(ref isEnding, "isEnding", false);
            Scribe_Values.Look(ref endLerp, "endLerp", 1f);
            Scribe_Values.Look(ref currentDist, "currentDist", 0f);
            Scribe_References.Look(ref launcher, "launcher");
        }

        protected override void Tick()
        {
            base.Tick();
            rotationAngle += RotationSpeed;

            currentDist += MoveSpeedPerTick;
            this.Position = (startPosition + direction * currentDist).ToIntVec3();

            if (!isEnding)
            {
                if (currentDist >= MaxDistance || !this.Position.InBounds(this.Map))
                {
                    isEnding = true;
                }
                IEnumerable<IntVec3> interceptCells = GenRadial.RadialCellsAround(this.Position, 1.9f, true);
                foreach (IntVec3 cell in interceptCells)
                {
                    if (!cell.InBounds(this.Map)) continue;
                    List<Thing> cellThings = cell.GetThingList(this.Map);
                    for (int i = cellThings.Count - 1; i >= 0; i--)
                    {
                        if (cellThings[i] is Projectile p) p.Destroy();
                    }
                }

                if (this.IsHashIntervalTick(5))
                {
                    ApplyAreaEffects();
                }
            }
            else
            {
                endLerp -= 1f / FadeOutTicks;
                if (endLerp <= 0f) this.Destroy();
            }
        }
        private void ApplyAreaEffects()
        {
            IEnumerable<Pawn> victims = GenRadial.RadialDistinctThingsAround(this.Position, this.Map, 1.9f, true).OfType<Pawn>();
            foreach (Pawn victim in victims)
            {
                if (victim.Faction != this.launcher?.Faction)
                {
                    DamageInfo dinfo = new DamageInfo(DamageDefOf.Blunt, 5f, 0, -1f, this.launcher);
                    victim.TakeDamage(dinfo);

                    if (!victim.InMentalState)
                    {
                        if (victim.RaceProps.Humanlike)
                        {
                            victim.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Berserk, null, false);
                        }
                        else if (victim.RaceProps.IsMechanoid)
                        {
                            victim.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.BerserkMechanoid, null, false);
                        }
                        else if (victim.RaceProps.Animal)
                        {
                            victim.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Manhunter, null, false);
                        }
                    }
                }
            }
        }
        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            Vector3 realDrawPos = startPosition + direction * currentDist;
            realDrawPos.y = AltitudeLayer.MoteOverhead.AltitudeFor(); 

            Graphic graphic = this.DefaultGraphic;
            if (graphic != null)
            {
                float currentScale = 3f * endLerp;
                Vector3 s = new Vector3(currentScale, 1f, currentScale);
                Matrix4x4 matrix = default;
                matrix.SetTRS(realDrawPos, Quaternion.AngleAxis(rotationAngle, Vector3.up), s);

                Material mat = graphic.MatSingle;
                if (isEnding)
                {
                    mat = FadedMaterialPool.FadedVersionOf(mat, endLerp);
                }
                Graphics.DrawMesh(MeshPool.plane10, matrix, mat, 0);
            }
        }
    }
}
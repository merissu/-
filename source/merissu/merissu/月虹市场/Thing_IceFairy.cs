using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using System.Linq;

namespace merissu
{
    public class Thing_IceFairy : Thing
    {
        private Pawn caster;
        private float orbitAngle;
        private float selfRotationAngle;
        private Vector3 currentPos;

        private const float Radius = 1.2f;
        private const float CrystalScale = 0.8f;

        private const float IdleOrbitSpeed = 1.5f;
        private const float CombatOrbitSpeed = 6.0f;
        private const float IdleRotateSpeed = 4f;
        private const float CombatRotateSpeed = 12f;

        private Pawn cachedTarget;
        private const float ScanRadius = 40f;
        private const float ChaseSpeed = 0.2f;
        private const int DamageIntervalTicks = 3;
        private const float DamageAmount = 4f;
        private int nextDamageTick;

        private const string HediffDefName = "spiritualpower";
        private const float MinSeverityToAttack = 0.02f;
        private const float SeverityCost = 0.01f;
        private int damageCount = 0; 

        public void Init(Pawn pawn)
        {
            this.caster = pawn;
            this.currentPos = pawn.DrawPos;
        }

        protected override void Tick()
        {
            base.Tick();

            if (caster == null || !caster.Spawned || caster.Dead)
            {
                if (!Destroyed) Destroy();
                return;
            }

            if (Find.TickManager.TicksGame % 60 == 0)
            {
                if (caster.Drafted && GetSpiritualPowerSeverity() >= MinSeverityToAttack)
                {
                    cachedTarget = FindClosestEnemy(ScanRadius);
                }
                else
                {
                    cachedTarget = null;
                }
            }

            float currentOrbitSpeed = (cachedTarget != null) ? CombatOrbitSpeed : IdleOrbitSpeed;
            float currentRotateSpeed = (cachedTarget != null) ? CombatRotateSpeed : IdleRotateSpeed;

            orbitAngle = (orbitAngle + currentOrbitSpeed) % 360f;
            selfRotationAngle = (selfRotationAngle + currentRotateSpeed) % 360f;

            if (caster.Drafted && cachedTarget != null)
            {
                DoCombatLogic(cachedTarget);
            }
            else
            {
                currentPos = Vector3.MoveTowards(currentPos, caster.DrawPos, ChaseSpeed);
            }
        }

        private void DoCombatLogic(Pawn target)
        {
            if (target == null) return;

            currentPos = Vector3.MoveTowards(currentPos, target.DrawPos, ChaseSpeed);

            if ((currentPos - target.DrawPos).sqrMagnitude < 1.5f)
            {
                if (Find.TickManager.TicksGame >= nextDamageTick && GetSpiritualPowerSeverity() >= MinSeverityToAttack)
                {
                    ApplyDamageAndEffect(target);
                    nextDamageTick = Find.TickManager.TicksGame + DamageIntervalTicks;
                }
            }
        }

        private void ApplyDamageAndEffect(Pawn target)
        {
            target.TakeDamage(new DamageInfo(DamageDefOf.Frostbite, DamageAmount, 0f, -1f, this));

            damageCount++;
            if (damageCount >= 2)
            {
                ReduceSpiritualPower(SeverityCost);
                damageCount = 0; 
            }

            for (int i = 0; i < 3; i++)
            {
                ThrowColdMote(GetCurrentDrawPos(), caster.Map);
            }

            FireRandomProjectiles(3);
        }

        private float GetSpiritualPowerSeverity()
        {
            Hediff hediff = caster.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named(HediffDefName));
            return hediff?.Severity ?? 0f;
        }

        private void ReduceSpiritualPower(float amount)
        {
            Hediff hediff = caster.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named(HediffDefName));
            if (hediff != null)
            {
                hediff.Severity -= amount;
            }
        }

        private void FireRandomProjectiles(int count)
        {
            ThingDef bulletDef = ThingDef.Named("Crushedice");
            if (bulletDef == null) return;

            SoundDef shootSound = SoundDef.Named("Crushedice");
            Vector3 origin = GetCurrentDrawPos();

            for (int i = 0; i < count; i++)
            {
                if (shootSound != null)
                {
                    shootSound.PlayOneShot(new TargetInfo(origin.ToIntVec3(), caster.Map));
                }

                float randomAngle = Rand.Range(0, 360);
                Vector3 direction = Quaternion.AngleAxis(randomAngle, Vector3.up) * Vector3.forward;

                Vector3 targetVec = origin + direction * 10f;
                IntVec3 targetCell = targetVec.ToIntVec3();

                Projectile projectile = (Projectile)GenSpawn.Spawn(bulletDef, origin.ToIntVec3(), caster.Map);
                projectile.Launch(caster, origin, targetCell, targetCell, ProjectileHitFlags.All, false, null);
            }
        }

        private void ThrowColdMote(Vector3 loc, Map map)
        {
            ThingDef moteDef = ThingDef.Named("Mote_IceCloud");
            if (moteDef == null) return;

            MoteThrown mote = (MoteThrown)ThingMaker.MakeThing(moteDef);
            mote.Scale = Rand.Range(0.6f, 1.2f);
            mote.exactPosition = loc;
            mote.rotationRate = Rand.Range(-2f, 2f);
            mote.SetVelocity(Rand.Range(0, 360), Rand.Range(0.8f, 1.5f));
            mote.airTimeLeft = 999f;

            GenSpawn.Spawn(mote, loc.ToIntVec3(), map);
        }

        private Vector3 GetCurrentDrawPos()
        {
            float rad = orbitAngle * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(rad) * Radius, 0f, Mathf.Sin(rad) * Radius);
            return currentPos + offset;
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            Vector3 finalPos = GetCurrentDrawPos();
            finalPos.y = AltitudeLayer.MoteOverhead.AltitudeFor();
            Matrix4x4 matrix = default;
            matrix.SetTRS(finalPos, Quaternion.AngleAxis(selfRotationAngle, Vector3.up), new Vector3(CrystalScale, 1f, CrystalScale));
            Graphics.DrawMesh(MeshPool.plane10, matrix, Graphic.MatSingle, 0);
        }

        private Pawn FindClosestEnemy(float radius)
        {
            return (Pawn)GenClosest.ClosestThingReachable(
                caster.Position,
                caster.Map,
                ThingRequest.ForGroup(ThingRequestGroup.Pawn),
                Verse.AI.PathEndMode.Touch,
                TraverseParms.For(caster),
                radius,
                t => {
                    Pawn p = t as Pawn;
                    return p != null && !p.Dead && !p.Downed &&
                           p.Faction != null && p.Faction.HostileTo(caster.Faction) &&
                           !p.IsPrisoner && p != caster &&
                           GenSight.LineOfSight(caster.Position, p.Position, caster.Map);
                });
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref caster, "caster");
            Scribe_Values.Look(ref orbitAngle, "orbitAngle");
            Scribe_Values.Look(ref selfRotationAngle, "selfRotationAngle");
            Scribe_Values.Look(ref currentPos, "currentPos");
            Scribe_Values.Look(ref nextDamageTick, "nextDamageTick");
            Scribe_Values.Look(ref damageCount, "damageCount"); 
        }
    }
}
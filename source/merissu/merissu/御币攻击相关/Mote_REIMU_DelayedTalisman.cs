using UnityEngine;
using Verse;
using Verse.Sound;

namespace merissu
{
    public class Mote_REIMU_DelayedTalisman : Thing
    {
        public Vector3 startPosition;        
        public Vector3 targetPosition;
        public int moveDurationTicks;
        public int idleDurationTicks;
        public LocalTargetInfo targetToFireAt;
        public Thing launcher;
        public SoundDef convertSound;

        private int ticksElapsed = 0;
        private float spinAngle = 0f;
        private const float SpinSpeed = 20f;

        private bool isMoving => ticksElapsed < moveDurationTicks;
        private bool isIdle => ticksElapsed >= moveDurationTicks && ticksElapsed < moveDurationTicks + idleDurationTicks;

        private Vector3 currentDrawPos;
        private float currentScale;

        public override Vector3 DrawPos => currentDrawPos;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            UpdateTransform();
        }

        protected override void Tick()
        {
            base.Tick();
            spinAngle += SpinSpeed;
            ticksElapsed++;

            if (isMoving || isIdle)
            {
                UpdateTransform();
            }
            else 
            {
                ConvertToRealProjectile();
            }
        }

        private void UpdateTransform()
        {
            if (isMoving)
            {
                float progress = (float)ticksElapsed / moveDurationTicks;
                currentDrawPos = Vector3.Lerp(startPosition, targetPosition, progress);
                currentScale = Mathf.Lerp(0.1f, 1f, progress);
            }
            else 
            {
                currentDrawPos = targetPosition;
                currentScale = 1f;
            }
            Position = currentDrawPos.ToIntVec3();
        }

        private void ConvertToRealProjectile()
        {
            if (Map == null || launcher == null) return;

            convertSound?.PlayOneShot(new TargetInfo(Position, Map));

            ThingDef projDef = ThingDef.Named("REIMU_SpinningTalisman");
            if (projDef != null)
            {
                Projectile projectile = (Projectile)GenSpawn.Spawn(projDef, Position, Map);
                projectile.Launch(launcher, currentDrawPos, targetToFireAt, targetToFireAt,
                    ProjectileHitFlags.IntendedTarget, false, null, null);
            }

            Destroy();
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            Vector3 drawPos = currentDrawPos;
            drawPos.y = AltitudeLayer.MoteOverhead.AltitudeFor();
            Quaternion rot = Quaternion.Euler(0f, spinAngle, 0f);
            Matrix4x4 matrix = Matrix4x4.TRS(drawPos, rot, new Vector3(currentScale, 1f, currentScale));
            Graphics.DrawMesh(MeshPool.plane10, matrix,
                def.graphicData.Graphic.MatSingle, 0);
        }
    }
}
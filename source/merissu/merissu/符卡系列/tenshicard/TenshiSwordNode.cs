using RimWorld;
using UnityEngine;
using Verse;

namespace merissu
{
    public class PawnRenderNodeProperties_TenshiSword : PawnRenderNodeProperties
    {
        public string abilityDefName = "Hisouten";
        public float orbitRadius = 1.2f;
        public float orbitDegPerTick = 8f;
        public float spinDegPerTick = 12f;
        public float holdSeconds = 5f;

        public PawnRenderNodeProperties_TenshiSword()
        {
            nodeClass = typeof(PawnRenderNode_TenshiSword);
            workerClass = typeof(PawnRenderNodeWorker_TenshiSword);
            debugLabel = "TenshiSword";
            drawSize = Vector2.one;
            overrideMeshSize = new Vector2(1.5f, 1.5f);
            baseLayer = 92f;
            pawnType = RenderNodePawnType.Any;
        }
    }

    public class PawnRenderNode_TenshiSword : PawnRenderNode
    {
        public new PawnRenderNodeProperties_TenshiSword Props => (PawnRenderNodeProperties_TenshiSword)props;

        public float spinAngle;
        public float orbitAngle;

        private int lastTick = -1;
        private int holdUntilTick = -1;
        private float holdAngle;
        private bool wasAiming;

        public PawnRenderNode_TenshiSword(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree)
            : base(pawn, props, tree)
        {
            orbitAngle = pawn.Rotation.AsAngle;
        }

        public override Graphic GraphicFor(Pawn pawn)
        {
            string path = Props.texPath.NullOrEmpty() ? "Weapons/ScarletRhapsodySword" : Props.texPath;
            return GraphicDatabase.Get<Graphic_Single>(path, ShaderDatabase.MoteGlow, Vector2.one, Props.color ?? Color.white);
        }

        public void TickAngles(Pawn pawn)
        {
            int now = Find.TickManager.TicksGame;
            if (now == lastTick) return;
            lastTick = now;

            spinAngle = Mathf.Repeat(spinAngle + Props.spinDegPerTick, 360f);

            int holdTicks = Mathf.RoundToInt(Props.holdSeconds * 60f);
            float goal;

            if (IsAiming(pawn, out float aimAngle))
            {
                holdAngle = aimAngle;
                holdUntilTick = now + holdTicks;
                if (!wasAiming) orbitAngle = aimAngle;
                goal = aimAngle;
            }
            else if (holdUntilTick > 0 && now < holdUntilTick)
            {
                goal = holdAngle;
            }
            else
            {
                goal = (pawn != null && pawn.Spawned) ? pawn.Rotation.AsAngle : orbitAngle;
            }

            wasAiming = IsAiming(pawn, out _) || goal == holdAngle;
            wasAiming = IsAiming(pawn, out _);

            orbitAngle = Mathf.MoveTowardsAngle(orbitAngle, goal, Props.orbitDegPerTick);
            GlobalTextureAtlasManager.TryMarkPawnFrameSetDirty(pawn);
        }

        private bool IsAiming(Pawn pawn, out float aimAngle)
        {
            aimAngle = 0f;
            if (pawn == null || !pawn.Spawned) return false;
            if (pawn.stances?.curStance is Stance_Busy busy && busy.focusTarg.IsValid
                && busy.verb is Verb_CastAbility cast && cast.ability != null
                && (Props.abilityDefName.NullOrEmpty() || cast.ability.def.defName == Props.abilityDefName))
            {
                Vector3 dir = busy.focusTarg.CenterVector3 - pawn.DrawPos;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.001f)
                {
                    aimAngle = dir.AngleFlat();
                    return true;
                }
            }
            return false;
        }
    }

    public class PawnRenderNodeWorker_TenshiSword : PawnRenderNodeWorker
    {
        public override Vector3 OffsetFor(PawnRenderNode node, PawnDrawParms parms, out Vector3 pivot)
        {
            Vector3 offset = base.OffsetFor(node, parms, out pivot);
            if (!(node is PawnRenderNode_TenshiSword sword)) return offset;

            sword.TickAngles(parms.pawn ?? node.tree.pawn);

            Vector3 dir = Quaternion.AngleAxis(sword.orbitAngle, Vector3.up) * Vector3.forward;
            return offset + dir * sword.Props.orbitRadius;
        }

        public override Quaternion RotationFor(PawnRenderNode node, PawnDrawParms parms)
        {
            Quaternion rot = base.RotationFor(node, parms);
            if (node is PawnRenderNode_TenshiSword sword)
            {
                rot *= Quaternion.AngleAxis(sword.spinAngle, Vector3.up);
            }
            return rot;
        }
    }
}
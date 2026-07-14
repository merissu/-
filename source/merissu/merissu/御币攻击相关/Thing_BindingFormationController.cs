using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace merissu
{
    public class Thing_BindingFormationController : Thing
    {
        public Verb_GoheiRandomShoot verb;
        public Pawn caster;
        public Thing targetThing;
        public int startTick;
        public int warmupTicks;

        private List<Thing_BindingFormationNode> nodes = new List<Thing_BindingFormationNode>();
        private bool damageTriggered;
        private bool warmupCompleted;
        private bool ended;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if (targetThing == null || caster == null)
            {
                Destroy();
                return;
            }

            float initialDist = 3.6f;
            Vector3[] offsets = new Vector3[4]
            {
                Vector3.forward * initialDist,   
                Vector3.back * initialDist,      
                Vector3.right * initialDist,   
                Vector3.left * initialDist       
            };

            ThingDef startFXDef = ThingDef.Named("Mote_BindingStartFX");
            ThingDef nodeDef = ThingDef.Named("BindingFormationNode");

            foreach (var off in offsets)
            {
                Vector3 pos = targetThing.DrawPos + off;
                Vector3 dirToTarget = -off.normalized; 

                Quaternion baseRot = Quaternion.LookRotation(dirToTarget);
                Quaternion finalRot = baseRot * Quaternion.Euler(0, 90, 0); 

                Mote_OneShotFade fx = (Mote_OneShotFade)ThingMaker.MakeThing(startFXDef);
                fx.exactPosition = pos;
                fx.ticksLeft = 15;
                fx.rotation = finalRot;
                GenSpawn.Spawn(fx, pos.ToIntVec3(), map);

                Thing_BindingFormationNode node = (Thing_BindingFormationNode)ThingMaker.MakeThing(nodeDef);
                node.controller = this;
                node.startPos = pos;
                node.targetPawn = targetThing as Pawn;
                node.startTick = startTick;
                node.warmupTicks = warmupTicks;
                node.exactPosition = pos;
                node.nodeRotation = finalRot;
                GenSpawn.Spawn(node, pos.ToIntVec3(), map);
                nodes.Add(node);
            }
        }

        protected override void Tick()
        {
            base.Tick();
            if (ended) return;

            int elapsed = Find.TickManager.TicksGame - startTick;

            if (!warmupCompleted)
            {
                if (caster == null || caster.Destroyed || !caster.Spawned ||
                    targetThing == null || targetThing.Destroyed)
                {
                    EndSequence(false);
                    return;
                }
                if (!(caster.stances?.curStance is Stance_Warmup warmupStance) || warmupStance.verb != verb)
                {
                    EndSequence(false);
                    return;
                }
                if (elapsed >= warmupTicks)
                    warmupCompleted = true;
            }

            if (!damageTriggered)
            {
                foreach (var node in nodes)
                {
                    if (node != null && !node.Destroyed && node.IsCollidingWithTarget(targetThing))
                    {
                        TriggerDamage();
                        return;
                    }
                }
            }

            if (warmupCompleted && elapsed >= warmupTicks + 120)
                EndSequence(damageTriggered);
        }

        private void TriggerDamage()
        {
            if (damageTriggered) return;
            damageTriggered = true;
            if (targetThing is Pawn targetPawn)
            {
                DamageInfo dinfo = new DamageInfo(DamageDefOf.Blunt, 30f, armorPenetration: 1f);
                targetPawn.TakeDamage(dinfo);
            }
            SoundDef.Named("hitC")?.PlayOneShot(new TargetInfo(targetThing.Position, Map));
            EndSequence(true);
        }

        private void EndSequence(bool causedDamage)
        {
            if (ended) return;
            ended = true;
            ThingDef endFXDef = ThingDef.Named("Mote_BindingEndFX");
            foreach (var node in nodes)
            {
                if (node != null && !node.Destroyed)
                {
                    Mote_OneShotFade fx = (Mote_OneShotFade)ThingMaker.MakeThing(endFXDef);
                    fx.exactPosition = node.exactPosition;
                    fx.ticksLeft = 15;
                    fx.rotation = node.nodeRotation; 
                    GenSpawn.Spawn(fx, node.exactPosition.ToIntVec3(), Map);
                }
            }
            for (int i = nodes.Count - 1; i >= 0; i--)
            {
                if (nodes[i] != null && !nodes[i].Destroyed)
                    nodes[i].Destroy();
            }
            nodes.Clear();
            Destroy();
        }
    }
}
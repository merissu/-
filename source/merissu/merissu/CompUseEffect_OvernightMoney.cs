using System.Collections.Generic;
using RimWorld;
using Verse;
using UnityEngine;

namespace merissu
{
    public class CompProperties_OvernightMoney : CompProperties
    {
        public CompProperties_OvernightMoney()
        {
            this.compClass = typeof(CompUseEffect_OvernightMoney);
        }
    }

    public class CompUseEffect_OvernightMoney : CompUseEffect
    {
        public override void DoEffect(Pawn user)
        {
            Map map = user.Map;
            if (map == null) return;

            FleckDef moneyEffectDef = DefDatabase<FleckDef>.GetNamed("Merissu_OvernightMoneyEffect", false);
            if (moneyEffectDef != null)
            {
                FleckCreationData data = FleckMaker.GetDataStatic(user.TrueCenter(), map, moneyEffectDef);

                data.rotationRate = Rand.Range(-180f, 180f);

                data.rotation = Rand.Range(0f, 360f);

                map.flecks.CreateFleck(data);
            }
            Pawn closestEnemy = null;
            float minDist = 100f;
            IReadOnlyList<Pawn> allPawns = map.mapPawns.AllPawnsSpawned;

            for (int i = 0; i < allPawns.Count; i++)
            {
                Pawn p = allPawns[i];
                if (p == user || p.Dead || p.Downed) continue;
                if (!p.HostileTo(user.Faction)) continue;

                float dist = user.Position.DistanceToSquared(p.Position);
                if (dist < minDist)
                {
                    minDist = dist;
                    closestEnemy = p;
                }
            }

            if (closestEnemy != null)
            {
                IntVec3 currentPos = closestEnemy.Position;
                IntVec3 direction = currentPos - user.Position; 

                IntVec3 step = new IntVec3(
                    Mathf.Clamp(direction.x, -1, 1),
                    0,
                    Mathf.Clamp(direction.z, -1, 1)
                );

                IntVec3 targetPos = currentPos + (step * 2);

                if (!targetPos.InBounds(map) || !targetPos.Walkable(map))
                {
                    targetPos = currentPos + step;
                }

                if (targetPos.InBounds(map) && targetPos.Walkable(map))
                {
                    closestEnemy.Position = targetPos;

                    if (closestEnemy.pather != null)
                    {
                        closestEnemy.pather.StopDead();
                    }
                }
            }
            Thing refill = ThingMaker.MakeThing(this.parent.def);
            if (user.inventory != null)
            {
                user.inventory.innerContainer.TryAdd(refill);
            }
        }
    }
}
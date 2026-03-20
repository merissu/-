using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace U.N.Owen
{
    public class MapComponent_DanmakuScheduler : MapComponent
    {
        public class PendingShot : IExposable
        {
            public IntVec3 root;
            public IntVec3 target;
            public Pawn pawn;
            public int fireTick;

            public void ExposeData()
            {
                Scribe_Values.Look(ref root, "root");
                Scribe_Values.Look(ref target, "target");
                Scribe_References.Look(ref pawn, "pawn");
                Scribe_Values.Look(ref fireTick, "fireTick");
            }
        }

        private List<PendingShot> pendingShots = new List<PendingShot>();

        public MapComponent_DanmakuScheduler(Map map) : base(map) { }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref pendingShots, "pendingShots", LookMode.Deep);
            if (pendingShots == null) pendingShots = new List<PendingShot>();
        }

        public void Schedule(IntVec3 root, IntVec3 target, Pawn pawn, int fireTick)
        {
            pendingShots.Add(new PendingShot
            {
                root = root,
                target = target,
                pawn = pawn,
                fireTick = fireTick
            });
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();

            int now = Find.TickManager.TicksGame;
            for (int i = pendingShots.Count - 1; i >= 0; i--)
            {
                var shot = pendingShots[i];
                if (shot.fireTick > now) continue;

                Verb_SpawnDanmaku_Kedama.SpawnDanmaku(
                    shot.root, shot.target, shot.pawn, map, 0f);

                pendingShots.RemoveAt(i);
            }
        }
    }
}

using RimWorld;
using UnityEngine;
using Verse;

namespace merissu
{
    public class CompProperties_RefuelableGraphicAndGlow : CompProperties_Glower
    {
        public string noFuelTexPath;
        public bool glowWithoutFuel = false;
        public float noFuelGlowRadius = 0f;

        public ColorInt ManuelGlowColor = new ColorInt(255, 255, 255, 0);

        public CompProperties_RefuelableGraphicAndGlow()
        {
            this.compClass = typeof(CompRefuelableGraphicAndGlow);
        }
    }

    public class Building_RefuelableGraphicChange : Building
    {
        private Graphic noFuelGraphicInt;
        private CompRefuelable compRefuelable;
        private CompRefuelableGraphicAndGlow customComp;

        public override Graphic Graphic
        {
            get
            {
                if (compRefuelable == null) compRefuelable = GetComp<CompRefuelable>();

                if (compRefuelable != null && !compRefuelable.HasFuel)
                {
                    if (noFuelGraphicInt == null)
                    {
                        if (customComp == null) customComp = GetComp<CompRefuelableGraphicAndGlow>();

                        string path = customComp?.Props?.noFuelTexPath;
                        if (!path.NullOrEmpty())
                        {
                            noFuelGraphicInt = GraphicDatabase.Get(
                                def.graphicData.graphicClass,
                                path,
                                def.graphic.Shader,
                                def.graphicData.drawSize,
                                DrawColor,
                                DrawColorTwo
                            );
                        }
                    }

                    if (noFuelGraphicInt != null)
                    {
                        return noFuelGraphicInt;
                    }
                }

                return base.Graphic;
            }
        }
    }

    public class CompRefuelableGraphicAndGlow : CompGlower
    {
        private CompRefuelable compRefuelable;

        public CompProperties_RefuelableGraphicAndGlow Props => (CompProperties_RefuelableGraphicAndGlow)props;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            compRefuelable = parent.GetComp<CompRefuelable>();
        }

        protected override bool ShouldBeLitNow
        {
            get
            {
                if (!base.ShouldBeLitNow) return false;

                if (compRefuelable != null && !compRefuelable.HasFuel && !Props.glowWithoutFuel)
                {
                    return false;
                }
                return true;
            }
        }

        public override float GlowRadius
        {
            get
            {
                if (compRefuelable != null && !compRefuelable.HasFuel)
                {
                    return Props.glowWithoutFuel ? Props.noFuelGlowRadius : 0f;
                }
                return base.GlowRadius;
            }
        }

        public override ColorInt GlowColor
        {
            get
            {
                if (compRefuelable != null && !compRefuelable.HasFuel && Props.glowWithoutFuel)
                {
                    return Props.ManuelGlowColor;
                }
                return base.GlowColor;
            }
        }

        public override void ReceiveCompSignal(string signal)
        {
            base.ReceiveCompSignal(signal);

            if (signal == "Refueled" || signal == "RanOutOfFuel")
            {
                parent.Notify_ColorChanged();

                if (parent.Spawned)
                {
                    parent.Map.glowGrid.DeRegisterGlower(this);
                    parent.Map.glowGrid.RegisterGlower(this);

                    parent.Map.mapDrawer.MapMeshDirty(parent.Position, MapMeshFlagDefOf.Things);
                }
            }
        }
    }
}
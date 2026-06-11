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
            compClass = typeof(CompRefuelableGraphicAndGlow);
        }
    }

    public class Building_RefuelableGraphicChange : Building
    {
        private Graphic noFuelGraphicInt;
        private CompRefuelableGraphicAndGlow customComp;

        private CompRefuelable Refuelable => GetComp<CompRefuelable>();

        public override Graphic Graphic
        {
            get
            {
                if (Refuelable != null && !Refuelable.HasFuel)
                {
                    if (noFuelGraphicInt == null)
                    {
                        if (customComp == null)
                        {
                            customComp = GetComp<CompRefuelableGraphicAndGlow>();
                        }
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
        private bool needInitialRefresh = true;

        public CompProperties_RefuelableGraphicAndGlow Props =>
            (CompProperties_RefuelableGraphicAndGlow)props;

        private CompRefuelable Refuelable =>
            parent.GetComp<CompRefuelable>();

        protected override bool ShouldBeLitNow
        {
            get
            {
                if (!parent.Spawned)
                    return false;

                if (!FlickUtility.WantsToBeOn(parent))
                    return false;

                CompPowerTrader power = parent.TryGetComp<CompPowerTrader>();

                if (power != null && !power.PowerOn)
                    return false;

                if (Refuelable != null &&
                    !Refuelable.HasFuel &&
                    !Props.glowWithoutFuel)
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
                if (Refuelable != null && !Refuelable.HasFuel)
                {
                    return Props.glowWithoutFuel
                        ? Props.noFuelGlowRadius
                        : 0f;
                }

                return base.GlowRadius;
            }
        }

        public override ColorInt GlowColor
        {
            get
            {
                if (Refuelable != null &&
                    !Refuelable.HasFuel &&
                    Props.glowWithoutFuel)
                {
                    return Props.ManuelGlowColor;
                }

                return base.GlowColor;
            }
        }

        public override void CompTick()
        {
            base.CompTick();

            if (needInitialRefresh)
            {
                needInitialRefresh = false;

                if (parent.Spawned)
                {
                    parent.Map.glowGrid.DeRegisterGlower(this);
                    parent.Map.glowGrid.RegisterGlower(this);

                    parent.Notify_ColorChanged();
                    parent.Map.mapDrawer.MapMeshDirty(
                        parent.Position,
                        MapMeshFlagDefOf.Things
                    );
                }
            }
        }

        public override void ReceiveCompSignal(string signal)
        {
            base.ReceiveCompSignal(signal);

            if (signal == "Refueled" ||
                signal == "RanOutOfFuel")
            {
                RefreshGlow();
            }
        }

        private void RefreshGlow()
        {
            parent.Notify_ColorChanged();

            if (parent.Spawned)
            {
                parent.Map.glowGrid.DeRegisterGlower(this);
                parent.Map.glowGrid.RegisterGlower(this);

                parent.Map.mapDrawer.MapMeshDirty(
                    parent.Position,
                    MapMeshFlagDefOf.Things
                );
            }
        }
    }
}
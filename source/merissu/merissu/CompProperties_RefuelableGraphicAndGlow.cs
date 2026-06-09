using RimWorld;
using Verse;

namespace merissu
{
    public class CompProperties_RefuelableGraphicAndGlow : CompProperties
    {
        public string noFuelTexPath;
        public bool glowWithoutFuel = false;
        public float noFuelGlowRadius = 0f;

        public CompProperties_RefuelableGraphicAndGlow()
        {
            this.compClass = typeof(CompRefuelableGraphicAndGlow);
        }
    }

    public class Building_RefuelableGraphicChange : Building
    {
        public override Graphic Graphic
        {
            get
            {
                var comp = GetComp<CompRefuelableGraphicAndGlow>();
                if (comp != null && !comp.HasFuel && comp.NoFuelGraphic != null)
                {
                    return comp.NoFuelGraphic;
                }
                return base.Graphic;
            }
        }
    }

    public class CompRefuelableGraphicAndGlow : ThingComp
    {
        private CompRefuelable compRefuelable;
        private CompGlower compGlower;

        private Graphic defaultGraphic;
        private Graphic noFuelGraphic;
        private bool lastFuelState = true;

        public bool HasFuel => compRefuelable == null || compRefuelable.HasFuel;
        public Graphic NoFuelGraphic => noFuelGraphic;

        public CompProperties_RefuelableGraphicAndGlow Props => (CompProperties_RefuelableGraphicAndGlow)props;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            compRefuelable = parent.GetComp<CompRefuelable>();
            compGlower = parent.GetComp<CompGlower>();

            defaultGraphic = parent.def.graphic;

            if (!Props.noFuelTexPath.NullOrEmpty())
            {
                noFuelGraphic = GraphicDatabase.Get(
                    parent.def.graphicData.graphicClass,
                    Props.noFuelTexPath,
                    parent.def.graphic.Shader,
                    parent.def.graphicData.drawSize,
                    parent.DrawColor,
                    parent.DrawColorTwo
                );
            }

            if (compRefuelable != null)
            {
                lastFuelState = compRefuelable.HasFuel;
            }

            UpdateState(true);
        }

        public override void CompTick()
        {
            base.CompTick();
            CheckFuelState();
        }

        public override void CompTickRare()
        {
            base.CompTickRare();
            CheckFuelState();
        }

        private void CheckFuelState()
        {
            if (compRefuelable != null)
            {
                bool hasFuel = compRefuelable.HasFuel;
                if (hasFuel != lastFuelState)
                {
                    lastFuelState = hasFuel;
                    UpdateState(false);
                }
            }
        }

        private void UpdateState(bool forceRefresh)
        {
            if (compRefuelable == null) return;

            bool hasFuel = compRefuelable.HasFuel;

            if (noFuelGraphic != null && !forceRefresh)
            {
                parent.Notify_ColorChanged();
            }

            if (compGlower != null)
            {
                var originalGlowerProps = parent.def.GetCompProperties<CompProperties_Glower>();

                if (hasFuel)
                {
                    if (originalGlowerProps != null)
                    {
                        compGlower.Props.glowRadius = originalGlowerProps.glowRadius;
                    }

                    if (parent.Spawned)
                    {
                        parent.Map.glowGrid.RegisterGlower(compGlower);
                        parent.Map.glowGrid.DirtyCell(parent.Position);
                    }
                }
                else
                {
                    if (Props.glowWithoutFuel)
                    {
                        compGlower.Props.glowRadius = Props.noFuelGlowRadius;
                        if (parent.Spawned)
                        {
                            parent.Map.glowGrid.RegisterGlower(compGlower);
                            parent.Map.glowGrid.DirtyCell(parent.Position);
                        }
                    }
                    else
                    {
                        if (parent.Spawned)
                        {
                            parent.Map.glowGrid.DeRegisterGlower(compGlower);
                            parent.Map.glowGrid.DirtyCell(parent.Position);
                        }
                    }
                }
            }

            if (parent.Spawned && !forceRefresh)
            {
                parent.Map.mapDrawer.MapMeshDirty(parent.Position, MapMeshFlagDefOf.Things);
            }
        }
    }
}
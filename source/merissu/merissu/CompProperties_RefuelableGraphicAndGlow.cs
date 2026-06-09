using RimWorld;
using UnityEngine;
using Verse;

namespace merissu
{
    public class CompProperties_RefuelableGraphicAndGlow : CompProperties
    {
        public string noFuelTexPath;
        public bool glowWithoutFuel = false;
        public float noFuelGlowRadius = 0f;

        public Color ManuelGlowColor = Color.white;

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

    public class CompRefuelableGraphicAndGlow : ThingComp
    {
        private CompRefuelable compRefuelable;
        private CompGlower compGlower;
        private bool lastFuelState = true;

        public CompProperties_RefuelableGraphicAndGlow Props => (CompProperties_RefuelableGraphicAndGlow)props;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            compRefuelable = parent.GetComp<CompRefuelable>();
            compGlower = parent.GetComp<CompGlower>();

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

            if (!forceRefresh)
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
                        compGlower.Props.glowColor = originalGlowerProps.glowColor; 
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
                        compGlower.Props.glowColor = new ColorInt(Props.ManuelGlowColor);

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
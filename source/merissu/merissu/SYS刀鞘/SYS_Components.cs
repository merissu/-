using Verse;

namespace merissu
{
    public class CompProperties_WeaponExtention : CompProperties
    {
        public Offset northOffset;
        public Offset eastOffset;
        public Offset southOffset;
        public Offset westOffset;
        public bool littleDown = false;

        public CompProperties_WeaponExtention()
        {
            compClass = typeof(CompWeaponExtention);
        }
    }

    public class CompWeaponExtention : ThingComp
    {
        public bool littleDown = false;
        public CompProperties_WeaponExtention Props;

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            Props = (CompProperties_WeaponExtention)props;
            if (Props.littleDown) littleDown = true;
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            Props = (CompProperties_WeaponExtention)props;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Props = (CompProperties_WeaponExtention)props;
        }
    }

    public class CompProperties_Sheath : CompProperties
    {
        private static GraphicData nullData;
        public GraphicData sheathOnlyGraphicData = null;
        public GraphicData fullGraphicData = null;
        public DrawPosition drawPosition = DrawPosition.None;
        public Offset northOffset;
        public Offset eastOffset;
        public Offset southOffset;
        public Offset westOffset;

        public static Graphic NullData
        {
            get
            {
                if (nullData == null)
                {
                    nullData = new GraphicData();
                    nullData.graphicClass = typeof(Graphic_Single);
                    nullData.texPath = "Other/null";
                }
                return nullData.Graphic;
            }
        }

        public CompProperties_Sheath()
        {
            compClass = typeof(CompSheath);
        }
    }

    public class CompSheath : ThingComp
    {
        private Graphic fullGraphicInt;
        private Graphic sheathOnlyGraphicInt;
        public CompProperties_Sheath Props;

        public virtual Graphic FullGraphic
        {
            get
            {
                if (fullGraphicInt == null)
                {
                    if (Props.fullGraphicData == null) return parent.Graphic;
                    fullGraphicInt = Props.fullGraphicData.GraphicColoredFor(parent);
                }
                return fullGraphicInt;
            }
        }

        public virtual Graphic SheathOnlyGraphic
        {
            get
            {
                if (sheathOnlyGraphicInt == null)
                {
                    if (Props.sheathOnlyGraphicData == null)
                    {
                        sheathOnlyGraphicInt = CompProperties_Sheath.NullData;
                        return sheathOnlyGraphicInt;
                    }
                    sheathOnlyGraphicInt = Props.sheathOnlyGraphicData.GraphicColoredFor(parent);
                }
                return sheathOnlyGraphicInt;
            }
        }

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            Props = (CompProperties_Sheath)base.props;
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            Props = (CompProperties_Sheath)props;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Props = (CompProperties_Sheath)props;
        }
    }
}
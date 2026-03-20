using RimWorld;
using Verse;
using UnityEngine;
using System.Collections.Generic;

namespace merissu
{
    public class CompUsable_CustomIcon : CompUsable
    {
        public CompProperties_Usable_CustomIcon PropsEx =>
            (CompProperties_Usable_CustomIcon)this.props;

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo gizmo in base.CompGetGizmosExtra())
            {
                if (gizmo is Command_Action command)
                {
                    if (!PropsEx.iconPath.NullOrEmpty())
                    {
                        command.icon = ContentFinder<Texture2D>.Get(
                            PropsEx.iconPath,
                            true
                        );
                    }
                }

                yield return gizmo;
            }
        }
    }
}

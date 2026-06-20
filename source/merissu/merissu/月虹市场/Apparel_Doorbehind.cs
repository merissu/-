using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using System;

namespace merissu
{
    [StaticConstructorOnStartup]
    public static class CE_Compat_Patch_Doorbehind
    {
        static CE_Compat_Patch_Doorbehind()
        {
            if (ModLister.HasActiveModWithName("Combat Extended"))
            {
                Log.Message("背后之门已兼容ce");
                var harmony = new Harmony("merissu.doorbehind.ce_compat");
                PatchCE(harmony);
            }
        }

        private static void PatchCE(Harmony harmony)
        {
            var bulletCE = AccessTools.TypeByName("CombatExtended.BulletCE");

            if (bulletCE != null)
            {
                var impactMethod =
                    AccessTools.Method(
                        bulletCE,
                        "Impact",
                        new[] { typeof(Thing) });

                if (impactMethod != null)
                {
                    harmony.Patch(
                        impactMethod,
                        prefix: new HarmonyMethod(
                            typeof(CE_Compat_Patch_Doorbehind),
                            nameof(Prefix_ProjectileCE_Impact)));
                }
            }
            var typeCompSuppressable = Type.GetType("CombatExtended.CompSuppressable, CombatExtended");
            if (typeCompSuppressable != null)
            {
                var addSuppressionMethod = AccessTools.Method(typeCompSuppressable, "AddSuppression");
                if (addSuppressionMethod != null)
                {
                    harmony.Patch(addSuppressionMethod,
                        prefix: new HarmonyMethod(typeof(CE_Compat_Patch_Doorbehind), nameof(Prefix_AddSuppression)));
                }
            }
        }

        public static bool Prefix_ProjectileCE_Impact(
            Thing __instance,
            Thing hitThing)
        {
            if (__instance == null)
                return true;

            Pawn pawn = hitThing as Pawn;

            if (pawn == null)
                return true;

            if (pawn.apparel == null)
                return true;

            Apparel_Doorbehind doorbehind = null;

            for (int i = 0; i < pawn.apparel.WornApparel.Count; i++)
            {
                if (pawn.apparel.WornApparel[i] is Apparel_Doorbehind d)
                {
                    doorbehind = d;
                    break;
                }
            }

            if (doorbehind == null)
                return true;

            Thing launcher = null;

            try
            {
                launcher =
                    Traverse.Create(__instance)
                        .Field("launcher")
                        .GetValue<Thing>();
            }
            catch
            {
            }

            if (launcher == null)
                return true;

            Vector3 wearerPos = pawn.DrawPos;
            Vector3 instigatorPos = launcher.DrawPos;

            Vector3 facingDir =
                pawn.Rotation.FacingCell.ToVector3();

            Vector3 dirToInstigator =
                (instigatorPos - wearerPos).normalized;

            if (Vector3.Dot(
                    facingDir,
                    dirToInstigator) < 0f)
            {
                MoteMaker.ThrowText(
                    pawn.DrawPos,
                    pawn.Map,
                    "biu",
                    Color.gray);

                FleckMaker.ThrowMicroSparks(
                    __instance.DrawPos,
                    pawn.Map);

                __instance.Destroy(
                    DestroyMode.Vanish);

                return false;
            }

            return true;
        }
        public static bool Prefix_AddSuppression(ThingComp __instance, IntVec3 origin)
        {
            if (__instance.parent is Pawn pawn && pawn.apparel != null)
            {
                for (int i = 0; i < pawn.apparel.WornApparel.Count; i++)
                {
                    if (pawn.apparel.WornApparel[i] is Apparel_Doorbehind)
                    {
                        Vector3 wearerPos = pawn.DrawPos;
                        Vector3 instigatorPos = origin.ToVector3Shifted();
                        Vector3 facingDir = pawn.Rotation.FacingCell.ToVector3();
                        Vector3 dirToInstigator = (instigatorPos - wearerPos).normalized;

                        if (Vector3.Dot(facingDir, dirToInstigator) < 0)
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }
    }
    public class Apparel_Doorbehind : Apparel
    {
        private int tickCounter = 0;

        private const int DamageInterval = 60; 
        private const float DamageAmount = 10f;  

        protected override void Tick()
        {
            base.Tick();

            if (Wearer == null || Wearer.Dead || !Wearer.Spawned)
                return;

            tickCounter++;

            EraseProjectilesBehindExact();

            if (tickCounter % DamageInterval == 0)
            {
                DamageEnemiesBehind();
            }
        }
        private static readonly Type ProjectileCEType =
            AccessTools.TypeByName("CombatExtended.ProjectileCE");

        private void EraseProjectilesBehindExact()
        {
            Map map = Wearer.Map;

            IntVec3 backCell = Wearer.Position + Wearer.Rotation.Opposite.FacingCell;

            IntVec3 sideDir =
                Wearer.Rotation.FacingCell.x != 0
                    ? new IntVec3(0, 0, 1)
                    : new IntVec3(1, 0, 0);

            IntVec3[] targetCells =
            {
        backCell,
        backCell + sideDir,
        backCell - sideDir
    };

            for (int i = 0; i < targetCells.Length; i++)
            {
                IntVec3 cell = targetCells[i];

                if (!cell.InBounds(map))
                    continue;

                List<Thing> things = cell.GetThingList(map);

                for (int j = things.Count - 1; j >= 0; j--)
                {
                    Thing t = things[j];

                    if (t is Projectile vanillaProj)
                    {
                        Thing launcher = vanillaProj.Launcher;

                        if (launcher != null &&
                            !launcher.HostileTo(Wearer))
                        {
                            continue;
                        }

                        FleckMaker.ThrowMicroSparks(
                            t.DrawPos,
                            map);

                        t.Destroy(DestroyMode.Vanish);

                        continue;
                    }

                    if (ProjectileCEType != null &&
                        ProjectileCEType.IsAssignableFrom(t.GetType()))
                    {
                        Thing launcher = null;

                        try
                        {
                            launcher = Traverse.Create(t)
                                .Field("launcher")
                                .GetValue<Thing>();
                        }
                        catch
                        {
                        }

                        if (launcher == null)
                        {
                            try
                            {
                                launcher = Traverse.Create(t)
                                    .Property("Launcher")
                                    .GetValue<Thing>();
                            }
                            catch
                            {
                            }
                        }

                        if (launcher != null &&
                            !launcher.HostileTo(Wearer))
                        {
                            continue;
                        }

                        FleckMaker.ThrowMicroSparks(
                            t.DrawPos,
                            map);

                        t.Destroy(DestroyMode.Vanish);
                    }
                }
            }
        }
        private void DamageEnemiesBehind()
        {
            Map map = Wearer.Map;
            IntVec3 centerBehind = Wearer.Position + Wearer.Rotation.Opposite.FacingCell;

            CellRect rect = CellRect.SingleCell(centerBehind).ExpandedBy(1);

            foreach (IntVec3 cell in rect)
            {
                if (!cell.InBounds(map)) continue;
                if (cell == Wearer.Position) continue; 

                List<Thing> things = cell.GetThingList(map);
                for (int i = things.Count - 1; i >= 0; i--)
                {
                    if (things[i] is Pawn p && p.HostileTo(Wearer))
                    {
                        DamageInfo dinfo = new DamageInfo(DamageDefOf.Cut, DamageAmount, 0.5f, -1f, Wearer, null, Wearer.def);
                        p.TakeDamage(dinfo);
                        FleckMaker.ThrowMicroSparks(p.DrawPos, map);
                    }
                }
            }
        }

        public override bool CheckPreAbsorbDamage(DamageInfo dinfo)
        {
            if (dinfo.Def.isExplosive) return false;

            if (dinfo.Instigator != null && dinfo.Weapon != null && dinfo.Weapon.IsRangedWeapon)
            {
                Vector3 wearerPos = Wearer.DrawPos;
                Vector3 instigatorPos = dinfo.Instigator.DrawPos;
                Vector3 facingDir = Wearer.Rotation.FacingCell.ToVector3();

                Vector3 dirToInstigator = (instigatorPos - wearerPos).normalized;

                if (Vector3.Dot(facingDir, dirToInstigator) < 0)
                {
                    MoteMaker.ThrowText(Wearer.DrawPos, Wearer.Map, "biu", Color.gray);
                    return true;
                }
            }

            return false;
        }
        public override void DrawWornExtras()
        {
            Pawn wearer = this.Wearer;
            if (wearer == null || !wearer.Spawned || wearer.story?.bodyType == null) return;
            if (wearer.Downed) return;
            Rot4 rot = wearer.Rotation;
            Vector3 drawPos = wearer.DrawPos;

            string bodyTypeSuffix = wearer.story.bodyType.defName;
            string basePath = "Accessory/Backdoor_" + bodyTypeSuffix;

            string directionSuffix = "_south";

            if (rot == Rot4.South)
            {
                directionSuffix = "_south";
                drawPos.y -= 0.02f;
            }
            else if (rot == Rot4.North)
            {
                directionSuffix = "_north";
                drawPos.y += 0.05f;
            }
            else if (rot == Rot4.East)
            {
                directionSuffix = "_east";
                drawPos.y += 0.05f;
            }
            else if (rot == Rot4.West)
            {
                directionSuffix = "_west";
                drawPos.y += 0.05f;
            }

            string finalPath = basePath + directionSuffix;
            Material currentMat = MaterialPool.MatFrom(finalPath, ShaderDatabase.Mote);

            if (currentMat == null) return;

            Matrix4x4 matrix = default(Matrix4x4);
            Vector3 scale = new Vector3(3.0f, 1f, 3.0f);
            matrix.SetTRS(drawPos, Quaternion.identity, scale);

            Graphics.DrawMesh(MeshPool.plane10, matrix, currentMat, 0);
        }
    }
}
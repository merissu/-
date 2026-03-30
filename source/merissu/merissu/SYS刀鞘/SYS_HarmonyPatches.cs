using HarmonyLib;
using RimWorld;
using merissu;
using System.Reflection;
using UnityEngine;
using Verse;

namespace merissu
{
    [StaticConstructorOnStartup]
    public static class DrawPatch
    {
        public static bool HasMeleeAnimation;

        static DrawPatch()
        {
            HasMeleeAnimation = ModLister.GetActiveModWithIdentifier("co.uk.epicguru.meleeanimation") != null;
            Harmony val = new Harmony("com.merissu.rimworld.mod");
            val.PatchAll(Assembly.GetExecutingAssembly());
        }
    }

    [HarmonyPatch(typeof(PawnRenderUtility), "DrawEquipmentAndApparelExtras")]
    public static class DrawEquipment_WeaponBackPatch
    {
        public const float drawYPosition = 5f / 128f;
        public const float drawSYSYPosition = 0.03904f;

        [HarmonyPrefix]
        public static bool DrawEquipmentPrefix(Vector3 drawPos, Pawn pawn)
        {
            if (pawn.Dead || !pawn.Spawned || pawn.equipment?.Primary == null) return true;

            CompWeaponExtention comp = pawn.equipment.Primary.GetComp<CompWeaponExtention>();
            CompSheath compSheath = pawn.equipment.Primary.GetComp<CompSheath>();

            Stance_Busy stance_Busy = pawn.stances.curStance as Stance_Busy;
            bool isFighting = stance_Busy != null && !stance_Busy.neverAimWeapon && stance_Busy.focusTarg.IsValid;
            bool isDrafted = (pawn.carryTracker?.CarriedThing == null) && (pawn.Drafted || (pawn.CurJob?.def.alwaysShowWeapon ?? false));
            bool isStanding = !pawn.InBed() && pawn.GetPosture() == PawnPosture.Standing;

            if (compSheath != null && isStanding)
            {
                if (isFighting || isDrafted)
                {
                    DrawSheathLogic(compSheath, pawn, drawPos, compSheath.SheathOnlyGraphic);
                }
                else
                {
                    DrawSheathLogic(compSheath, pawn, drawPos, compSheath.FullGraphic);
                    return false;
                }
            }

            if (DrawPatch.HasMeleeAnimation)
            {
                if (isDrafted || isFighting) return true;
            }
            else
            {
                if (isFighting) return true;

                if (comp != null && isDrafted)
                {
                    Vector3 weaponPos = drawPos;
                    Offset offset;
                    bool flip = false;
                    switch (pawn.Rotation.AsInt)
                    {
                        case 0: offset = comp.Props.northOffset; break;
                        case 1: offset = comp.Props.eastOffset; weaponPos.y += drawYPosition; break;
                        case 2: offset = comp.Props.southOffset; weaponPos.y += drawYPosition; flip = true; break;
                        case 3: offset = comp.Props.westOffset; weaponPos.y += drawYPosition; flip = true; break;
                        default: offset = comp.Props.northOffset; break;
                    }
                    weaponPos += offset.position;
                    DrawEquipmentAimingInternal(pawn.equipment.Primary, weaponPos, offset.angle, flip);

                    return false; 
                }
            }

            return true;
        }


        public static void DrawEquipmentAimingInternal(Thing eq, Vector3 drawLoc, float aimAngle, bool isFlip = false)
        {
            Material matSingle = eq.Graphic.MatSingle;
            Graphics.DrawMesh(isFlip ? MeshPool.plane10Flip : MeshPool.plane10, drawLoc, Quaternion.AngleAxis(aimAngle, Vector3.up), matSingle, 0);
        }

        public static void DrawSheathLogic(CompSheath compSheath, Pawn pawn, Vector3 drawLoc, Graphic graphic)
        {
            if (graphic == null) return;
            Offset offset;
            bool isBack = compSheath.Props.drawPosition == DrawPosition.Back;

            if (pawn.Rotation == Rot4.South) offset = isBack ? compSheath.Props.southOffset : compSheath.Props.northOffset;
            else if (pawn.Rotation == Rot4.North) offset = isBack ? compSheath.Props.northOffset : compSheath.Props.southOffset;
            else if (pawn.Rotation == Rot4.East) offset = compSheath.Props.eastOffset;
            else offset = compSheath.Props.westOffset;

            Vector3 finalLoc = drawLoc + offset.position;
            finalLoc.y += drawSYSYPosition;
            Graphics.DrawMesh(graphic.MeshAt(pawn.Rotation), finalLoc, Quaternion.AngleAxis(offset.angle % 360f, Vector3.up), graphic.MatAt(pawn.Rotation), 0);
        }
    }
}
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using HarmonyLib;

namespace merissu
{
    [HarmonyPatch(typeof(MaterialPool), "MatFrom", new Type[] { typeof(MaterialRequest) })]
    public static class Patch_MaterialPool_WindFix
    {
        public static void Postfix(Material __result, MaterialRequest req)
        {
            if (__result != null && req.shader != null && req.shader.name == "merissu/RW_Plant_bioluminescence")
            {
                WindManager.Notify_PlantMaterialCreated(__result);
            }
        }
    }
}
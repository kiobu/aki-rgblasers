using Aki.Reflection.Patching;
using System.Reflection;
using UnityEngine;

namespace RGBLasers
{
    internal class LaserBeamPatch : Aki.Reflection.Patching.ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(LaserBeam).GetMethod("method_0", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        [PatchPostfix]
        public static void PatchPostfix(ref Light ___light_0, ref LaserBeam __instance)
        {
            RGBLaserInjector.points.Add(___light_0);
            RGBLaserInjector.beams.Add(__instance);
        }
    }
}

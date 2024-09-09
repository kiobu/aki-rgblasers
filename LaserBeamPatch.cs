using SPT.Reflection.Patching;
using System.Reflection;
using UnityEngine;

namespace RGBLasers
{
    internal class LaserBeamPatch : SPT.Reflection.Patching.ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(LaserBeam).GetMethod(nameof(LaserBeam.method_0));
        }

        [PatchPostfix]
        public static void PatchPostfix(ref Light ___light_0, ref LaserBeam __instance, ref Mesh ___mesh_0, ref Mesh ___mesh_1)
        {
            RGBLaserInjector.points.Add(___light_0);
            RGBLaserInjector.beams.Add(__instance);
            RGBLaserInjector.pointMeshes.Add(___mesh_0);
            RGBLaserInjector.beamMeshes.Add(___mesh_1);

            if (!RGBLaserInjector.cachedBeamMesh)
            {
                RGBLaserInjector.cachedBeamMesh = Object.Instantiate(___mesh_1);
            }

            if (!RGBLaserInjector.cachedPointMesh)
            {
                RGBLaserInjector.cachedPointMesh = Object.Instantiate(___mesh_0);
            }
        }
    }
}

using BepInEx;
using UnityEngine;
using BepInEx.Configuration;
using BepInEx.Logging;
using Comfort.Common;
using EFT;
using System.Collections.Generic;
using System.Reflection;

namespace RGBLasers
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class RGBLasersPlugin : BaseUnityPlugin
    { 
        internal static ConfigEntry<float> Red;
        internal static ConfigEntry<float> Green;
        internal static ConfigEntry<float> Blue;
        internal static ConfigEntry<bool>  Rainbow;
        internal static ConfigEntry<float> RainbowSpeed;
        internal static ConfigEntry<float> LightIntensity;
        // internal static ConfigEntry<float> LightFactor;
        internal static ConfigEntry<float> BeamSize;
        internal static ConfigEntry<float> PointSize;
        internal static ConfigEntry<float> MaxDist;
        // internal static ConfigEntry<float> LightRange;
        internal static ConfigEntry<bool> UseCustomMeshVtxs;

        private static GameObject _hookObject;
        public static ManualLogSource logger;

        private void Awake()
        {
            new LaserBeamPatch().Enable(); // Enable the Harmony patch.

            logger = Logger;

            // Plugin startup logic
            logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
            _hookObject = new GameObject("RGBLasers");
            _hookObject.AddComponent<RGBLaserInjector>();
            DontDestroyOnLoad(_hookObject);

            Blue = Config.Bind("Laser Color", "B", 1f, new ConfigDescription("The blue value.", new AcceptableValueRange<float>(0, 1)));
            Green = Config.Bind("Laser Color", "G", 0f, new ConfigDescription("The green value.", new AcceptableValueRange<float>(0, 1)));
            Red = Config.Bind("Laser Color", "R", 0f, new ConfigDescription("The red value.", new AcceptableValueRange<float>(0, 1)));
            Rainbow = Config.Bind("Laser Color", "Rainbow", false, new ConfigDescription("Rainbow effect (overrides RGB values)."));
            RainbowSpeed = Config.Bind("Laser Color", "Rainbow Speed", 1f, new ConfigDescription("The speed of the rainbow effect.", new AcceptableValueRange<float>(0, 2)));

            LightIntensity = Config.Bind("Parameters", "Light Intensity", 1f, new ConfigDescription("LightIntensity param. Modifies both beam and point. Very high values will result in glare.", new AcceptableValueRange<float>(1, 20)));
            // LightFactor = Config.Bind("Parameters", "Intensity Factor", 1f, new ConfigDescription("IntensityFactor param. Does not seem to do anything.", new AcceptableValueRange<float>(1, 100)));
            MaxDist = Config.Bind("Parameters", "Point Light Distance", 100f, new ConfigDescription("MaxDistance param. This (seems) to adjust the point light size/distance ratio.", new AcceptableValueRange<float>(50, 1000)));
            // LightRange = Config.Bind("Parameters", "Light Range", 100f, new ConfigDescription("LightRange param.", new AcceptableValueRange<float>(1, 10000)));

            UseCustomMeshVtxs = Config.Bind("Custom", "Use Custom Mesh Vertices", false, new ConfigDescription("Whether or not to use custom mesh vertices."));
            BeamSize = Config.Bind("Custom", "Beam Size", .002f, new ConfigDescription("BeamSize param used to adjust beam light mesh.", new AcceptableValueRange<float>(0.001f, 0.03f)));
            PointSize = Config.Bind("Custom", "Point Light Size", 1f, new ConfigDescription("Custom point size parameter used to adjust point light mesh.", new AcceptableValueRange<float>(0f, 50f)));
        }
    }

    public class RGBLaserInjector : MonoBehaviour
    {
        public bool IsInWorld() => Singleton<GameWorld>.Instance != null;

        public static List<Light> points = new List<Light>();
        public static List<Mesh> pointMeshes = new List<Mesh>();
        public static List<LaserBeam> beams = new List<LaserBeam>();
        public static List<Mesh> beamMeshes = new List<Mesh>();

        public static Mesh cachedPointMesh;        
        public static Mesh cachedBeamMesh;

        public void Render(Color c)
        {
            // No lasers to render, ignore this call (shouldn't ever happen).
            if (points == null || beams == null || points.Count == 0 || beams.Count == 0)
            {
                return;
            }

            // Remove outdated references.
            points.RemoveAll(p => p == null);
            pointMeshes.RemoveAll(p => p == null);
            beams.RemoveAll(b => b == null);
            beamMeshes.RemoveAll(b => b == null);

            foreach (var beam in beams)
            {
                if (beam == null) { beams.Remove(beam); continue; } // Remove beam if it no longer exists, and skip it.

                // beam.LightRange = RGBLasersPlugin.LightRange.Value;
                beam.PointMaterial.color = c;
                beam.BeamMaterial.color = c;
                beam.LightIntensity = RGBLasersPlugin.LightIntensity.Value;
                beam.BeamSize = RGBLasersPlugin.BeamSize.Value;
                beam.MaxDistance = RGBLasersPlugin.MaxDist.Value;
                typeof(LaserBeam).GetField("IntensityFactor", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(beam, RGBLasersPlugin.LightIntensity.Value);
            }

            foreach (var point in points)
            {
                if (point == null) { points.Remove(point); continue; } // Remove point if it no longer exists, and skip it.

                point.color = c;
                point.intensity = RGBLasersPlugin.LightIntensity.Value;
                point.enabled = true;
            }
        }

        public static void AdjustMeshes()
        {
            // No lasers to render, ignore this call (shouldn't ever happen).
            if (pointMeshes == null || beamMeshes == null || pointMeshes.Count == 0 || beamMeshes.Count == 0)
            {
                return;
            }

            foreach (var mesh in beamMeshes)
            {
                mesh.vertices = new Vector3[]
                {
                    new Vector3(-RGBLasersPlugin.BeamSize.Value, 0f),
                    new Vector3(RGBLasersPlugin.BeamSize.Value, 0f),
                    new Vector3(-RGBLasersPlugin.BeamSize.Value, 1f),
                    new Vector3(RGBLasersPlugin.BeamSize.Value, 1f)
                };
                mesh.bounds = new Bounds(
                    new Vector3(RGBLasersPlugin.BeamSize.Value, RGBLasersPlugin.BeamSize.Value, RGBLasersPlugin.MaxDist.Value * 0.5f),
                    new Vector3(RGBLasersPlugin.BeamSize.Value * 2f, RGBLasersPlugin.BeamSize.Value * 2f, RGBLasersPlugin.MaxDist.Value)
                );
            }

            foreach (var mesh in pointMeshes)
            {
                mesh.vertices = new Vector3[]
                {
                    new Vector3(-RGBLasersPlugin.PointSize.Value, -RGBLasersPlugin.PointSize.Value),
                    new Vector3(RGBLasersPlugin.PointSize.Value, -RGBLasersPlugin.PointSize.Value),
                    new Vector3(-RGBLasersPlugin.PointSize.Value, RGBLasersPlugin.PointSize.Value),
                    new Vector3(RGBLasersPlugin.PointSize.Value, RGBLasersPlugin.PointSize.Value)
                };

                mesh.RecalculateBounds();
            }
        }

        public static void ResetMeshes()
        {
            foreach (var mesh in beamMeshes)
            {
                mesh.vertices = cachedBeamMesh.vertices;
                mesh.bounds = cachedBeamMesh.bounds;
            }

            foreach (var mesh in pointMeshes)
            {
                mesh.vertices = cachedPointMesh.vertices;
                mesh.RecalculateBounds();
            }
        }

        public void Update()
        {
            if (IsInWorld())
            {
                // At least one laser is active.
                if (beams.Count >= 1)
                {
                    if (RGBLasersPlugin.Rainbow.Value)
                    {
                        Render(HSBColor.ToColor(new HSBColor(Mathf.PingPong(Time.time * RGBLasersPlugin.RainbowSpeed.Value, 1), 1, 1)));
                    }
                    else
                    {
                        Render(new Color(RGBLasersPlugin.Red.Value, RGBLasersPlugin.Green.Value, RGBLasersPlugin.Blue.Value));
                    }

                    if (RGBLasersPlugin.UseCustomMeshVtxs.Value)
                    {
                        AdjustMeshes();
                    }
                    else
                    {
                        ResetMeshes();
                    }
                }
            }
        }
    }
}

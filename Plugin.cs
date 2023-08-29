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
        // internal static ConfigEntry<float> PointIntensity;
        internal static ConfigEntry<float> LightFactor;
        internal static ConfigEntry<float> BeamSize;
        internal static ConfigEntry<float> MaxDist;
        internal static ConfigEntry<float> LightRange;
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

            Blue              = Config.Bind("Laser Color", "B", 1f, new ConfigDescription("The blue value.", new AcceptableValueRange<float>(0, 1)));
            Green             = Config.Bind("Laser Color", "G", 0f, new ConfigDescription("The green value.", new AcceptableValueRange<float>(0, 1)));
            Red               = Config.Bind("Laser Color", "R", 0f, new ConfigDescription("The red value.", new AcceptableValueRange<float>(0, 1)));
            Rainbow           = Config.Bind("Laser Color", "Rainbow", false, new ConfigDescription("Rainbow effect (overrides RGB values)."));
            RainbowSpeed      = Config.Bind("Laser Color", "Rainbow Speed", 1f, new ConfigDescription("The speed of the rainbow effect.", new AcceptableValueRange<float>(0, 2)));

            _                 = Config.Bind("Parameters", "These are the parameters for the LaserBeam class defined by BSG, as such the result of modifying these values may not be what you expect to see.", "");

            LightIntensity    = Config.Bind("Parameters", "Beam Intensity", 1f, new ConfigDescription("Beam intensity.", new AcceptableValueRange<float>(1, 100)));
            // PointIntensity    = Config.Bind("Parameters", "Point Light Intensity", 1f, new ConfigDescription("Laser point intensity.", new AcceptableValueRange<float>(1, 100)));
            BeamSize          = Config.Bind("Parameters", "Beam Size", 1f, new ConfigDescription("Beam size.", new AcceptableValueRange<float>(1, 100)));
            MaxDist           = Config.Bind("Parameters", "Max Beam Distance", 100f, new ConfigDescription("Max renderable beam distance. Setting this lower typically results in a large point light if beam intensity is high.", new AcceptableValueRange<float>(100, 10000)));
            LightRange        = Config.Bind("Parameters", "Light Range", 100f, new ConfigDescription("Light range.", new AcceptableValueRange<float>(1, 10000)));

            // LightFactor       = Config.Bind("DEPRECATED", "Intensity Factor", 1f, new ConfigDescription("Beam intensity factor. Does not seem to do anything.", new AcceptableValueRange<float>(1, 100)));
            // UseCustomMeshVtxs = Config.Bind("DEPRECATED", "Use Custom Mesh Vertices", false, new ConfigDescription("Whether or not to use custom mesh vertices based on configured beam size."));
        }
    }

    public class RGBLaserInjector : MonoBehaviour
    {
        public bool IsInWorld() => Singleton<GameWorld>.Instance != null;

        public static List<Light> points = new List<Light>();
        public static List<Mesh> pointMeshes = new List<Mesh>();
        public static List<LaserBeam> beams = new List<LaserBeam>();
        public static List<Mesh> beamMeshes = new List<Mesh>();
        private static FieldInfo intensityFactorField = typeof(LaserBeam).GetField("IntensityFactor", BindingFlags.Instance | BindingFlags.NonPublic);

        public void Render(Color c)
        {
            // No lasers to render, ignore this call.
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

                beam.LightRange = RGBLasersPlugin.LightRange.Value;
                beam.PointMaterial.color = c;
                beam.BeamMaterial.color = c;
                beam.LightIntensity = RGBLasersPlugin.LightIntensity.Value;
                beam.BeamSize = RGBLasersPlugin.BeamSize.Value;
                beam.MaxDistance = RGBLasersPlugin.MaxDist.Value;
                intensityFactorField.SetValue(beam, RGBLasersPlugin.LightIntensity.Value);
            }

            foreach (var point in points)
            {
                if (point == null) { points.Remove(point); continue; } // Remove point if it no longer exists, and skip it.

                point.color = c;
                point.intensity = RGBLasersPlugin.LightIntensity.Value;
                point.enabled = true;
            }

            /*
            if (RGBLasersPlugin.UseCustomMeshVtxs.Value)
            {
                // mesh_1
                foreach (var beamMesh in beamMeshes)
                {
                    if (beamMesh == null) { beamMeshes.Remove(beamMesh); continue; }

                    beamMesh.vertices = new Vector3[]
                    {
                        new Vector3(-RGBLasersPlugin.BeamSize.Value, 0f),
                        new Vector3(RGBLasersPlugin.BeamSize.Value, 0f),
                        new Vector3(-RGBLasersPlugin.BeamSize.Value, 1f),
                        new Vector3(RGBLasersPlugin.BeamSize.Value, 1f)
                    };
                    beamMesh.bounds = new Bounds(
                        new Vector3(RGBLasersPlugin.BeamSize.Value, RGBLasersPlugin.BeamSize.Value, RGBLasersPlugin.MaxDist.Value * 0.5f),
                        new Vector3(RGBLasersPlugin.BeamSize.Value * 2f, RGBLasersPlugin.BeamSize.Value * 2f, RGBLasersPlugin.MaxDist.Value)
                    );
                }
            }
            */
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
                }
            }
        }
    }
}

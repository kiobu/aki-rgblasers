using BepInEx;
using UnityEngine;
using BepInEx.Configuration;
using BepInEx.Logging;
using Comfort.Common;
using EFT;
using System.Collections.Generic;

namespace RGBLasers
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class RGBLasersPlugin : BaseUnityPlugin
    { 
        internal static ConfigEntry<float> Red;
        internal static ConfigEntry<float> Green;
        internal static ConfigEntry<float> Blue;
        internal static ConfigEntry<bool> Rainbow;
        internal static ConfigEntry<float> RainbowSpeed;

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
        }
    }

    public class RGBLaserInjector : MonoBehaviour
    {
        public bool IsInWorld() => Singleton<GameWorld>.Instance != null;

        public static List<Light> points = new List<Light>();
        public static List<LaserBeam> beams = new List<LaserBeam>();

        public void Render(Color c)
        {
            // No lasers to render, ignore this call.
            if (points == null || beams == null || points.Count == 0 || beams.Count == 0)
            {
                return;
            }

            // Remove outdated references.
            points.RemoveAll(p => p == null);
            beams.RemoveAll(b => b == null);

            foreach (var point in points)
            {
                if (point == null) { points.Remove(point); continue; } // Remove point if it no longer exists, and skip it.

                point.color = c;
                point.enabled = true;
            }

            foreach (var beam in beams)
            {
                if (beam == null) { beams.Remove(beam); continue; } // Remove beam if it no longer exists, and skip it.

                // beam.LightColor = c;
                beam.PointMaterial.color = c;
                beam.BeamMaterial.color = c;
                beam.LightIntensity = 10f;
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
                }
            }
        }
    }
}

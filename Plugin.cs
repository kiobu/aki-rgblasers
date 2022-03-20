using BepInEx;
using UnityEngine;
using BepInEx.Configuration;
using BepInEx.Logging;
using Comfort.Common;
using EFT;
using Aki.Reflection.Utils;
using System.Reflection;
using System;
using System.Linq;

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

        private Light[] points;
        private LaserBeam[] beams;

        private bool IsSameColor(float r1, float g1, float b1, float r2, float g2, float b2)
        {
            if (r1 == r2 && g1 == g2 && b1 == b2)
            {
                return true;
            }

            return false;
        }

        public void Render(Color c)
        {
            var points = this.points;
            var beams = this.beams;

            // No lasers to render, ignore this call.
            if (points == null || beams == null)
            {
                return;
            }

            var lights = points.Where(l => l.name == "laserBeamLight");
            var laserbeams = beams;

            foreach (var _laser in lights)
            {
                var laser = _laser.GetComponent<Light>();
                laser.color = c;
                laser.enabled = true;
            }

            foreach (var _beam in laserbeams)
            {
                var beam = _beam.GetComponent<LaserBeam>();
                beam.LightColor = c;
                beam.PointMaterial.color = c;
                beam.BeamMaterial.color = c;
                beam.LightIntensity = 10f;
            }
        }

        public void Update()
        {
            if (IsInWorld())
            {
                var laser_object = GameObject.Find("laserBeamLight");

                // At least one laser is active.
                if (laser_object != null)
                {
                    if (this.points == null || this.beams == null)
                    {
                        RGBLasersPlugin.logger.LogInfo("Got new light objects.");
                        this.points = Resources.FindObjectsOfTypeAll(typeof(Light)) as Light[];
                        this.beams = Resources.FindObjectsOfTypeAll(typeof(LaserBeam)) as LaserBeam[];
                    }

                    if (RGBLasersPlugin.Rainbow.Value)
                    {
                        Render(HSBColor.ToColor(new HSBColor(Mathf.PingPong(Time.time * RGBLasersPlugin.RainbowSpeed.Value, 1), 1, 1)));
                    }
                    else
                    {
                        Render(new Color(RGBLasersPlugin.Red.Value, RGBLasersPlugin.Green.Value, RGBLasersPlugin.Blue.Value));
                    }
                }
                else
                {
                    if (this.points != null || this.beams != null)
                    {
                        RGBLasersPlugin.logger.LogInfo("Clearing old light objects.");

                        this.points = null;
                        this.beams = null;
                    }
                }
            }
        }
    }
}

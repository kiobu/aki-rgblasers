using BepInEx;
using UnityEngine;
using BepInEx.Configuration;
using BepInEx.Logging;
using Comfort.Common;
using EFT;

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

        private Light laser;
        private LaserBeam beam;

        private bool IsSameColor(float r1, float g1, float b1, float r2, float g2, float b2)
        {
            if (r1 == r2 && g1 == g2 && b1 == b2)
            {
                return true;
            }

            return false;
        }

        public void LateUpdate()
        {
            if (IsInWorld())
            {
                var laser_object = GameObject.Find("laserBeamLight");

                if (laser_object != null)
                {
                    // Cache components for optimization.
                    if (!laser || !beam)
                    {
                        var laser = laser_object.GetComponent<Light>();
                        var _b = GameObject.FindObjectOfType<LaserBeam>();
                        LaserBeam beam = _b.GetComponent<LaserBeam>();

                        this.laser = laser;
                        this.beam = beam;
                    }

                    if (RGBLasersPlugin.Rainbow.Value)
                    {
                        var RainbowColor = HSBColor.ToColor(new HSBColor(Mathf.PingPong(Time.time * RGBLasersPlugin.RainbowSpeed.Value, 1), 1, 1));

                        laser.color = RainbowColor;
                        laser.enabled = true;

                        beam.LightColor = RainbowColor;
                        beam.PointMaterial.color = RainbowColor;
                        beam.BeamMaterial.color = RainbowColor;
                        beam.LightIntensity = 10f;
                    }
                    else
                    {
                        var c = new Color(RGBLasersPlugin.Red.Value, RGBLasersPlugin.Green.Value, RGBLasersPlugin.Blue.Value);

                        if (!IsSameColor(laser.color.r, laser.color.g, laser.color.b, RGBLasersPlugin.Red.Value, RGBLasersPlugin.Green.Value, RGBLasersPlugin.Blue.Value))
                        {
                            laser.color = c;
                            laser.enabled = true;

                            beam.LightColor = c;
                            beam.PointMaterial.color = c;
                            beam.BeamMaterial.color = c;
                            beam.LightIntensity = 10f;
                        }
                    }
                }
                else
                {
                    // Destroy cached references.
                    if (laser || beam)
                    {
                        laser = null;
                        beam = null;
                    }
                }
            }
        }
    }
}

using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using EFT;
using Comfort.Common;
using UnityEngine;
using UnityEditor;

namespace Dronecam
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class DronecamPlugin : BaseUnityPlugin
    {
        public static GameObject hookObject;
        public static ManualLogSource logger;

        public static int cullingLayer = 30;

        internal static ConfigEntry<bool> EnableCam;
        internal static ConfigEntry<bool> EnableCamlight;
        internal static ConfigEntry<float> CamDistance;
        internal static ConfigEntry<float> CamlightIntensity;

        private void Awake()
        {
            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");

            hookObject = new GameObject("DronecameraPlugin");
            hookObject.AddComponent<Dronecamera>();
            hookObject.AddComponent<Camera>();
            hookObject.AddComponent<Light>();
            hookObject.layer = cullingLayer;
            DontDestroyOnLoad(hookObject);

            hookObject.GetComponent<Camera>().enabled = false;

            EnableCam = Config.Bind("Camera", "Enable Camera", false, new ConfigDescription("Enable dronecam."));
            EnableCamlight = Config.Bind("Camlight", "Enable Camlight", false, new ConfigDescription("Enable dronecam light."));
            CamDistance = Config.Bind("Camera", "Distance", 5f, new ConfigDescription("The camera's distance from the player.", new AcceptableValueRange<float>(0, 50)));
            CamlightIntensity = Config.Bind("Camlight", "Camlight Intensity", 5f, new ConfigDescription("The camlight intensity.", new AcceptableValueRange<float>(0, 50)));

            DronecamPlugin.EnableCam.Value = false;
        }
    }

    public class Dronecamera : MonoBehaviour
    {
        public static bool IsInWorld() => Singleton<GameWorld>.Instance != null;
        public static Player LocalPlayer() => Singleton<GameWorld>.Instance.RegisteredPlayers.Find(p => p.IsYourPlayer);
        public Player ply;
        public Camera dronecam;
        public Light camlight;
        public Shader shader;

        public bool isEnabled;

        public void LateUpdate()
        {
            if (IsInWorld())
            {
                if (!dronecam)
                {
                    dronecam = DronecamPlugin.hookObject.GetComponent<Camera>();
                    dronecam.depth = 50;
                    dronecam.enabled = false;
                    dronecam.rect = new Rect(0.0f, 0.75f, 0.25f, 0.25f);

                    camlight = DronecamPlugin.hookObject.GetComponent<Light>();
                    camlight.color = Color.white;
                    camlight.enabled = false;
                }

                ply ??= LocalPlayer();

                if (DronecamPlugin.EnableCam.Value)
                {
                    dronecam.enabled = true;

                    dronecam.transform.position = new Vector3(ply.Transform.position.x, ply.Transform.position.y + DronecamPlugin.CamDistance.Value, ply.Transform.position.z);
                    dronecam.transform.LookAt(ply.Transform.position);
                }
                else
                {
                    dronecam.enabled = false;
                }

                if (DronecamPlugin.EnableCamlight.Value)
                {
                    camlight.enabled = true;
                    camlight.intensity = DronecamPlugin.CamlightIntensity.Value;
                }
                else
                {
                    camlight.enabled = false;
                }
            }
            else
            {
                if (dronecam)
                {
                    dronecam = null; 
                    camlight = null;
                }
            }
        }
    }
}

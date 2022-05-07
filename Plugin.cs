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
        public static GameObject dronecamObject;
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

            dronecamObject = new GameObject("DronecameraPlugin");
            dronecamObject.AddComponent<Dronecamera>();
            dronecamObject.AddComponent<Camera>();
            dronecamObject.AddComponent<Light>();
            dronecamObject.layer = cullingLayer;
            DontDestroyOnLoad(dronecamObject);

            dronecamObject.GetComponent<Camera>().enabled = false;

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

        public Camera dronecam;
        public Light camlight;

        public Player ply;
        public Camera maincam;
        public GameObject playerBlip;

        public void Start()
        {
            dronecam = DronecamPlugin.dronecamObject.GetComponent<Camera>();
            dronecam.cullingMask = 1 << DronecamPlugin.cullingLayer;

            camlight = DronecamPlugin.dronecamObject.GetComponent<Light>();

            playerBlip = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            playerBlip.layer = DronecamPlugin.cullingLayer;
            playerBlip.GetComponent<Renderer>().material.color = Color.green;
        }

        public void LateUpdate()
        {
            if (!dronecam)
            {
                DronecamPlugin.logger.LogError("Dronecam was null somehow.");
                dronecam = DronecamPlugin.dronecamObject.GetComponent<Camera>();
            }

            if (IsInWorld())
            {
                // Cache the local player.
                ply ??= LocalPlayer();

                // Cache the main camera (this will loop until the maincam is rendered.)
                maincam ??= Camera.main;

                // Set up the dronecamera component.
                dronecam.depth = 50;
                dronecam.enabled = false;
                dronecam.rect = new Rect(0.0f, 0.75f, 0.25f, 0.25f);

                /*
                camlight.color = Color.white;
                camlight.enabled = false;
                */

                // Set the main camera's culling mask to ignore our gameobject.
                if (maincam)
                {
                    maincam.cullingMask = maincam.cullingMask | ~(1 << DronecamPlugin.cullingLayer);

                    if (DronecamPlugin.EnableCam.Value)
                    {
                        dronecam.enabled = true;

                        dronecam.transform.position = new Vector3(ply.Transform.position.x, ply.Transform.position.y + DronecamPlugin.CamDistance.Value, ply.Transform.position.z);
                        dronecam.transform.LookAt(ply.Transform.position);

                        playerBlip.transform.position = new Vector3(ply.Transform.position.x, ply.Transform.position.y + DronecamPlugin.CamDistance.Value - 5f, ply.Transform.position.z);
                    }
                    else
                    {
                        dronecam.enabled = false;
                    }

                    /*
                    if (DronecamPlugin.EnableCamlight.Value)
                    {
                        camlight.enabled = true;
                        camlight.intensity = DronecamPlugin.CamlightIntensity.Value;
                    }
                    else
                    {
                        camlight.enabled = false;
                    }
                    */
                }
            }
            else
            {
                dronecam.enabled = false;
            }
        }
    }
}

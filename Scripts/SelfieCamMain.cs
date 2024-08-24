// Project:         Selfie Cam mod for Daggerfall Unity (http://www.dfworkshop.net)
// Copyright:       Copyright (C) 2024 Kirk.O
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Author:          Kirk.O
// Created On: 	    8/19/2024, 1:30 PM
// Last Edit:		8/23/2024, 11:30 PM
// Version:			1.00
// Special Thanks:  
// Modifier:

using UnityEngine;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Game.Serialization;
using DaggerfallWorkshop.Game.Utility.ModSupport.ModSettings;
using System;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using DaggerfallWorkshop.Game.Utility;
using DaggerfallWorkshop;
using UnityEngine.Rendering.PostProcessing;

namespace SelfieCam
{
    public partial class SelfieCamMain : MonoBehaviour
    {
        public static SelfieCamMain Instance;

        static Mod mod;

        // General Options
        public static KeyCode SelfieModeToggleKey { get; set; }
        public static bool HideHudInSelfieMode { get; set; }
        public static bool SupressAIInSelfieMode { get; set; }
        public static float SelfieModeYOffset { get; set; }
        public static bool AllowHiding { get; set; }

        // Camera Options
        public static float StartingZoomLevel { get; set; }
        public static float ZoomIncrements { get; set; }
        public static bool AllowZoomKeys { get; set; }
        public static KeyCode ZoomInKey { get; set; }
        public static KeyCode ZoomOutKey { get; set; }
        public static bool AllowFreezeCameraKey { get; set; }
        public static KeyCode FreezeCameraKey { get; set; }

        // Movement Options
        // Continue here tomorrow, I suppose. 

        // HUD Hiding Options
        public static bool HideEverything { get; set; }
        public static bool HideCompass { get; set; }
        public static bool HideVitals { get; set; }
        public static bool HideCrosshair { get; set; }
        public static bool HideInteractionModeIcon { get; set; }
        public static bool HideActiveSpells { get; set; }
        public static bool HideArrowCount { get; set; }
        public static bool HideBreathBar { get; set; }
        public static bool HidePopupText { get; set; }
        public static bool HideMidScreenText { get; set; }
        public static bool HideEscortingFaces { get; set; }
        public static bool HideLocalQuestPlaces { get; set; }

        // Misc Options
        public static bool AllowKeyPressQuickToggle { get; set; }
        public static KeyCode QuickToggleKey { get; set; }

        // Variables
        public static bool[] hudOriginalValues = { false, false, false, false, false, false, false, false, false, false, false }; // Compass, Vitals, Crosshair, InteractionModeIcon, ActiveSpells, ArrowCount, BreathBar, PopupText, MidScreenText, EscortingFaces, LocalQuestPlaces
        public static bool QuickToggleState { get; set; }

        // Added by Third Person Camera Code

        private GameObject paperDoll;
        private GameObject pivot;
        private Transform previousCameraParent;
        private PostProcessVolume volume;
        private int layer;
        private Camera camera;
        private bool headBobberEnabled;
        private GameObject torch;

        private GameObject torchObject;

        private float torchDist = 0.5f;
        private float torchHeight = 1f;

        private bool showTorchBillboard = false;
        private Vector3 torchPosition;

        public GameObject greenScreenObject;

        // Added by Third Person Camera Code

        [Invoke(StateManager.StateTypes.Start, 0)]
        public static void Init(InitParams initParams)
        {
            mod = initParams.Mod;
            var go = new GameObject(mod.Title);
            go.AddComponent<SelfieCamMain>(); // Add script to the scene.

            mod.LoadSettingsCallback = LoadSettings; // To enable use of the "live settings changes" feature in-game.

            mod.IsReady = true;
        }

        private void Start()
        {
            Debug.Log("Begin mod init: Selfie Cam");

            Instance = this;

            QuickToggleState = false;

            mod.LoadSettings();

            //StartGameBehaviour.OnStartGame += RefreshHUDVisibility_OnStartGame;
            //SaveLoadManager.OnLoad += RefreshHUDVisibility_OnSaveLoad;

            Debug.Log("Finished mod init: Selfie Cam");
        }

        private static void LoadSettings(ModSettings modSettings, ModSettingsChange change)
        {
            AllowHiding = mod.GetSettings().GetValue<bool>("GeneralSettings", "AllowHudHiding");

            HideEverything = mod.GetSettings().GetValue<bool>("HudHidingSettings", "HideEverything");
            HideCompass = mod.GetSettings().GetValue<bool>("HudHidingSettings", "HideCompass");
            HideVitals = mod.GetSettings().GetValue<bool>("HudHidingSettings", "HideVitals");
            HideCrosshair = mod.GetSettings().GetValue<bool>("HudHidingSettings", "HideCrosshair");
            HideInteractionModeIcon = mod.GetSettings().GetValue<bool>("HudHidingSettings", "HideInteractionModeIcon");
            HideActiveSpells = mod.GetSettings().GetValue<bool>("HudHidingSettings", "HideActiveSpells");
            HideArrowCount = mod.GetSettings().GetValue<bool>("HudHidingSettings", "HideArrowCount");
            HideBreathBar = mod.GetSettings().GetValue<bool>("HudHidingSettings", "HideBreathBar");
            HidePopupText = mod.GetSettings().GetValue<bool>("HudHidingSettings", "HidePopupText");
            HideMidScreenText = mod.GetSettings().GetValue<bool>("HudHidingSettings", "HideMidScreenText");
            HideEscortingFaces = mod.GetSettings().GetValue<bool>("HudHidingSettings", "HideEscortingFaces");
            HideLocalQuestPlaces = mod.GetSettings().GetValue<bool>("HudHidingSettings", "HideLocalQuestPlaces");

            AllowKeyPressQuickToggle = mod.GetSettings().GetValue<bool>("MiscSettings", "EnableKeyPressQuickToggle");
            var quickToggleKeyText = mod.GetSettings().GetValue<string>("MiscSettings", "QuickToggleKey");
            if (Enum.TryParse(quickToggleKeyText, out KeyCode result))
                QuickToggleKey = result;
            else
            {
                QuickToggleKey = KeyCode.G;
                Debug.Log("Selfie Cam: Invalid quick toggle keybind detected. Setting default. 'G' Key");
                DaggerfallUI.AddHUDText("Selfie Cam:", 6f);
                DaggerfallUI.AddHUDText("Invalid quick toggle keybind detected. Setting default. 'G' Key", 6f);
            }

            if (AllowKeyPressQuickToggle && QuickToggleState)
            {
            }
            else
            {
            }
        }

        private void Update()
        {
            if (GameManager.IsGamePaused || SaveLoadManager.Instance.LoadInProgress)
                return;

            // Handle key presses
            if (!GameManager.IsGamePaused && InputManager.Instance.GetAnyKeyDown() == QuickToggleKey)
            {
                if (paperDoll)
                {
                    AutoUnhideHUD();
                    StopPaperDoll();
                }
                else
                {
                    AutoHideHUD();
                    StartPaperDoll();
                }
            }

            if (paperDoll && !GameManager.Instance.TransportManager.IsOnFoot)
                StopPaperDoll();

            //Update rotation cause there's a stupid issue for some reason
            if (paperDoll)
            {
                //Torch
                if (torch != null)
                {
                    Vector3 dist = (paperDoll.transform.position - camera.transform.position);
                    dist.y = 0;
                    torch.transform.position = paperDoll.transform.position + dist.normalized * -torchDist + new Vector3(0, torchHeight, 0);
                }

                if (InputManager.Instance.GetKey(KeyCode.W))
                {
                    paperDoll.transform.localPosition = paperDoll.transform.localPosition - paperDoll.transform.forward * 0.05f;
                    pivot.transform.position = paperDoll.transform.position + new Vector3(0, .06f, 0);
                }
                if (InputManager.Instance.GetKey(KeyCode.S))
                {
                    paperDoll.transform.localPosition = paperDoll.transform.localPosition - paperDoll.transform.forward * -0.05f;
                    pivot.transform.position = paperDoll.transform.position + new Vector3(0, .06f, 0);
                }
                if (InputManager.Instance.GetKey(KeyCode.A))
                {
                    paperDoll.transform.localPosition = paperDoll.transform.localPosition - paperDoll.transform.right * -0.05f;
                    pivot.transform.position = paperDoll.transform.position + new Vector3(0, .06f, 0);
                }
                if (InputManager.Instance.GetKey(KeyCode.D))
                {
                    paperDoll.transform.localPosition = paperDoll.transform.localPosition - paperDoll.transform.right * 0.05f;
                    pivot.transform.position = paperDoll.transform.position + new Vector3(0, .06f, 0);
                }
                if (InputManager.Instance.GetKey(KeyCode.Space))
                {
                    paperDoll.transform.localPosition = paperDoll.transform.localPosition - paperDoll.transform.up * -0.05f;
                    pivot.transform.position = paperDoll.transform.position + new Vector3(0, .06f, 0);
                }
                if (InputManager.Instance.GetKey(KeyCode.LeftShift))
                {
                    paperDoll.transform.localPosition = paperDoll.transform.localPosition - paperDoll.transform.up * 0.05f;
                    pivot.transform.position = paperDoll.transform.position + new Vector3(0, .06f, 0);
                }

                // Update greenscreen object position if it exists, this way it will follow properly if moving camera view around, etc. 
                if (greenScreenObject) { greenScreenObject.transform.localPosition = paperDoll.transform.localPosition - paperDoll.transform.forward * 0.25f; }

                // Toggle Greenscreen Object 
                if (InputManager.Instance.GetKeyDown(KeyCode.F))
                {
                    ToggleGreenScreenObject();
                }

                //Toggle DOF
                if (InputManager.Instance.GetKeyDown(KeyCode.G))
                {
                    volume.enabled = !volume.enabled;
                }

                //Toggle layers
                /*if (InputManager.Instance.GetKeyDown(KeyCode.S))
                {
                    layer++;

                    if (layer > 1)
                        layer = 0;

                    var layerFlags = PaperDollRenderer.LayerFlags.All;

                    if (layer == 1)
                        layerFlags = PaperDollRenderer.LayerFlags.Body;

                    DaggerfallUI.Instance.PaperDollRenderer.Refresh(layerFlags);
                    paperDoll.GetComponent<DaggerfallBillboard>().SetMaterial(DaggerfallUI.Instance.PaperDollRenderer.PaperDollTexture, new Vector2(1.4f, 2.2f));
                }*/
            }
        }

        public void AutoHideHUD()
        {
            DaggerfallHUD dfuHud = DaggerfallUI.Instance.DaggerfallHUD;

            hudOriginalValues[0] = dfuHud.ShowCompass;
            hudOriginalValues[1] = dfuHud.ShowVitals;
            hudOriginalValues[2] = dfuHud.ShowCrosshair;
            hudOriginalValues[3] = dfuHud.ShowInteractionModeIcon;
            hudOriginalValues[4] = dfuHud.ShowActiveSpells;
            hudOriginalValues[5] = dfuHud.ShowArrowCount;
            hudOriginalValues[6] = dfuHud.ShowBreathBar;
            hudOriginalValues[7] = dfuHud.ShowPopupText;
            hudOriginalValues[8] = dfuHud.ShowMidScreenText;
            hudOriginalValues[9] = dfuHud.ShowEscortingFaces;
            hudOriginalValues[10] = dfuHud.ShowLocalQuestPlaces;

            dfuHud.ShowCompass = false;
            dfuHud.ShowVitals = false;
            dfuHud.ShowCrosshair = false;
            dfuHud.ShowInteractionModeIcon = false;
            dfuHud.ShowActiveSpells = false;
            dfuHud.ShowArrowCount = false;
            dfuHud.ShowBreathBar = false;
            dfuHud.ShowPopupText = false;
            dfuHud.ShowMidScreenText = false;
            dfuHud.ShowEscortingFaces = false;
            dfuHud.ShowLocalQuestPlaces = false;
        }

        public void AutoUnhideHUD()
        {
            DaggerfallHUD dfuHud = DaggerfallUI.Instance.DaggerfallHUD;

            dfuHud.ShowCompass = hudOriginalValues[0];
            dfuHud.ShowVitals = hudOriginalValues[1];
            dfuHud.ShowCrosshair = hudOriginalValues[2];
            dfuHud.ShowInteractionModeIcon = hudOriginalValues[3];
            dfuHud.ShowActiveSpells = hudOriginalValues[4];
            dfuHud.ShowArrowCount = hudOriginalValues[5];
            dfuHud.ShowBreathBar = hudOriginalValues[6];
            dfuHud.ShowPopupText = hudOriginalValues[7];
            dfuHud.ShowMidScreenText = hudOriginalValues[8];
            dfuHud.ShowEscortingFaces = hudOriginalValues[9];
            dfuHud.ShowLocalQuestPlaces = hudOriginalValues[10];

            for (int i = 0; i < hudOriginalValues.Length; i++)
            {
                hudOriginalValues[i] = false;
            }
        }

        public void StopPaperDoll()
        {
            if (torch != null)
            {
                torch.transform.localPosition = torchPosition;
            }

            if (torchObject != null)
                Destroy(torchObject);

            if (greenScreenObject) { Destroy(greenScreenObject); }

            Destroy(paperDoll);
            paperDoll = null;

            GameManager.Instance.PlayerMotor.enabled = true;
            GameManager.Instance.RightHandWeapon.ShowWeapon = true;
            GameManager.Instance.WeaponManager.enabled = true;
            GameManager.Instance.PlayerMouseLook.enabled = true;
            GameManager.Instance.PlayerActivate.enabled = true;
            GameManager.Instance.PlayerObject.GetComponent<FPSSpellCasting>().enabled = true;

            if (headBobberEnabled)
                GameManager.Instance.PlayerObject.GetComponent<HeadBobber>().enabled = true;

            camera.transform.parent = previousCameraParent.transform;
            camera.transform.localPosition = new Vector3(0, 0, 0);
            camera.transform.localRotation = Quaternion.identity;

            Destroy(volume.profile);
            Destroy(volume.gameObject);

            Destroy(camera.GetComponent<SC_MousePivot>());
            Destroy(pivot);
        }

        public GameObject GetPlayerTorch()
        {
            foreach (var light in GameManager.Instance.PlayerObject.GetComponentsInChildren<Light>())
            {
                if (light.gameObject.name == "Torch")
                    return light.gameObject;
            }

            return null;
        }

        public void StartPaperDoll()
        {
            if (!GameManager.Instance.TransportManager.IsOnFoot)
                return;

            if (GameManager.Instance.PlayerSpellCasting.IsPlayingAnim)
                return;

            torch = GetPlayerTorch();
            if (torch != null)
            {
                torchPosition = torch.transform.localPosition;

                if (showTorchBillboard)
                    SpawnTorch(torch);
            }

            layer = 0;

            paperDoll = SpawnPaperDoll(GameManager.Instance.PlayerObject.transform.position);

            // Probably disable HUD stuff here. 

            GameManager.Instance.PlayerMotor.enabled = false;
            GameManager.Instance.RightHandWeapon.ShowWeapon = false;
            GameManager.Instance.WeaponManager.enabled = false;
            GameManager.Instance.PlayerMouseLook.enabled = false;
            GameManager.Instance.PlayerActivate.enabled = false;
            GameManager.Instance.PlayerObject.GetComponent<FPSSpellCasting>().enabled = false;

            headBobberEnabled = GameManager.Instance.PlayerObject.GetComponent<HeadBobber>().enabled;
            if (headBobberEnabled)
                GameManager.Instance.PlayerObject.GetComponent<HeadBobber>().enabled = false;

            //Camera Junk

            pivot = new GameObject("Pivot");
            pivot.transform.position = GameManager.Instance.PlayerObject.transform.position + new Vector3(0, .1f, 0);
            pivot.transform.forward = GameManager.Instance.PlayerObject.transform.forward;

            previousCameraParent = GameManager.Instance.MainCamera.transform.parent;

            camera = GameManager.Instance.MainCamera;

            camera.transform.parent = pivot.transform;
            camera.transform.parent = pivot.transform;
            camera.transform.localPosition = new Vector3(0, 0, -2);
            camera.transform.localRotation = Quaternion.identity;

            var mousePivot = camera.gameObject.AddComponent<SC_MousePivot>();
            mousePivot.SetPivot(pivot.transform, pivot.transform.rotation);

            //Post Processing
            GameObject volumeObj = new GameObject("Third Person DOF Volume");
            volumeObj.layer = 15;

            volume = volumeObj.AddComponent<PostProcessVolume>();
            volume.isGlobal = true;
            volume.weight = 1;
            volume.priority = 5000;

            volume.profile = ScriptableObject.CreateInstance<PostProcessProfile>();

            var depthofField = (DepthOfField)volume.profile.AddSettings(typeof(DepthOfField));

            depthofField.enabled.Override(true);
            depthofField.focusDistance.Override(1.6f);
            depthofField.aperture.Override(6.2f);
            depthofField.focalLength.Override(58);
        }

        public GameObject SpawnPaperDoll(Vector3 position)
        {
            GameObject go = new GameObject("Selfie Cam Paper Doll");
            go.transform.position = position + new Vector3(0, 0.04f, 0); // This was to "fix" the left foot being slightly in the ground thing. 

            var billboard = go.AddComponent<DaggerfallBillboard>();

            DaggerfallUI.Instance.PaperDollRenderer.Refresh();

            billboard.SetMaterial(DaggerfallUI.Instance.PaperDollRenderer.PaperDollTexture, new Vector2(1.4f, 2.2f));

            return go;
        }

        public void SpawnTorch(GameObject root)
        {
            torchObject = new GameObject();
            torchObject.transform.parent = root.transform;
            torchObject.transform.localPosition = Vector3.zero;

            DaggerfallBillboard billboard = torchObject.AddComponent<DaggerfallBillboard>();
            billboard.SetMaterial(210, 16);

        }

        public void ToggleGreenScreenObject()
        {
            if (!greenScreenObject)
            {
                greenScreenObject = new GameObject("Green Screen Sprite");
                greenScreenObject.transform.localPosition = paperDoll.transform.localPosition - paperDoll.transform.forward * 0.25f;

                var billboard = greenScreenObject.AddComponent<DaggerfallBillboard>();

                Texture2D texture = new Texture2D(4, 4);
                Color[] greenColors = new Color[4 * 4];
                Color greenColor = Color.green;

                for (int i = 0; i < greenColors.Length; i++)
                {
                    greenColors[i] = greenColor;
                }

                texture.SetPixels(greenColors);
                texture.Apply();

                billboard.SetMaterial(texture, new Vector2(4, 4));
            }
            else
            {
                Destroy(greenScreenObject);
            }
        }
    }
}

// Project:         Selfie Cam mod for Daggerfall Unity (http://www.dfworkshop.net)
// Copyright:       Copyright (C) 2024 Kirk.O
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Author:          Kirk.O
// Created On: 	    8/19/2024, 1:30 PM
// Last Edit:		8/26/2024, 7:30 PM
// Version:			1.00
// Special Thanks:  Joshcamas, Jodie
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
        public static bool SuppressAIInSelfieMode { get; set; }
        public static int SelfieModeYOffset { get; set; }

        // Camera Options
        public static float StartingZoomLevel { get; set; }
        public static int ZoomIncrements { get; set; }
        public static bool AllowZoomKeys { get; set; }
        public static KeyCode ZoomInKey { get; set; }
        public static KeyCode ZoomOutKey { get; set; }
        public static bool AllowFreezeCameraKey { get; set; }
        public static KeyCode FreezeCameraKey { get; set; }

        // Movement Options
        public static bool AllowPaperdollMovement { get; set; }
        public static int PaperdollMovementSpeed { get; set; }
        public static KeyCode MoveForwardKey { get; set; }
        public static KeyCode MoveBackwardKey { get; set; }
        public static KeyCode MoveLeftKey { get; set; }
        public static KeyCode MoveRightKey { get; set; }
        public static KeyCode MoveUpKey { get; set; }
        public static KeyCode MoveDownKey { get; set; }

        // Depth Of Field Options
        public static bool AllowDepthOfField { get; set; }
        public static bool DepthOfFieldStartingState { get; set; }
        public static int DofFocusDistance { get; set; }
        public static int DofAperture { get; set; }
        public static int DofFocalLength { get; set; }
        public static KeyCode DepthOfFieldToggleKey { get; set; }

        // Green Screen Options
        public static bool AllowGreenScreen { get; set; }
        public static int GreenScreenWidth { get; set; }
        public static int GreenScreenHeight { get; set; }
        public static Color32 GreenScreenColor { get; set; }
        public static KeyCode GreenScreenToggleKey { get; set; }

        // Misc Options
        public static bool AllowSuppressAIQuickToggle { get; set; }
        public static KeyCode SuppressAIToggleKey { get; set; }

        // Variables
        public static bool[] hudOriginalValues = { false, false, false, false, false, false, false, false, false, false, false }; // Compass, Vitals, Crosshair, InteractionModeIcon, ActiveSpells, ArrowCount, BreathBar, PopupText, MidScreenText, EscortingFaces, LocalQuestPlaces
        public static bool hidingHud = false;
        public static bool aiWasSupressed = false;
        public static bool cameraLookFrozen = false;
        public static bool initialBloomState = false;

        private GameObject paperDoll;
        private GameObject pivot;
        private Transform previousCameraParent;
        private PostProcessVolume volume;
        private Camera scCamera;
        private bool headBobberEnabled;
        private GameObject torch;

        private GameObject torchObject;

        private float torchDist = 0.5f;
        private float torchHeight = 1f;

        private bool showTorchBillboard = false;
        private Vector3 torchPosition;

        public GameObject greenScreenObject;

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

            initialBloomState = DaggerfallUnity.Settings.BloomEnable;

            mod.LoadSettings();

            StartGameBehaviour.OnStartGame += RefreshVariousStates_OnStartGame;
            SaveLoadManager.OnLoad += RefreshVariousStates_OnSaveLoad;

            Debug.Log("Finished mod init: Selfie Cam");
        }

        private static void LoadSettings(ModSettings modSettings, ModSettingsChange change)
        {
            SelfieModeToggleKey = RegisterModFunctionKey("GeneralSettings", "SelfieModeToggleKey", KeyCode.P);
            HideHudInSelfieMode = mod.GetSettings().GetValue<bool>("GeneralSettings", "AutoHideHud");
            SuppressAIInSelfieMode = mod.GetSettings().GetValue<bool>("GeneralSettings", "SuppressAiInSelfieMode");
            SelfieModeYOffset = mod.GetSettings().GetValue<int>("GeneralSettings", "InitialYOffset");

            StartingZoomLevel = mod.GetSettings().GetValue<float>("CameraSettings", "StartingZoomLevel");
            ZoomIncrements = mod.GetSettings().GetValue<int>("CameraSettings", "ZoomIncrements");
            AllowZoomKeys = mod.GetSettings().GetValue<bool>("CameraSettings", "AllowZoomKeys");
            ZoomInKey = RegisterModFunctionKey("CameraSettings", "ZoomInKey", KeyCode.E);
            ZoomOutKey = RegisterModFunctionKey("CameraSettings", "ZoomOutKey", KeyCode.Q);
            AllowFreezeCameraKey = mod.GetSettings().GetValue<bool>("CameraSettings", "AllowFreezeCameraKey");
            FreezeCameraKey = RegisterModFunctionKey("CameraSettings", "FreezeCameraKey", KeyCode.R);

            AllowPaperdollMovement = mod.GetSettings().GetValue<bool>("MovementSettings", "AllowPaperdollMovement");
            PaperdollMovementSpeed = mod.GetSettings().GetValue<int>("MovementSettings", "PaperdollMovementSpeed");
            MoveForwardKey = RegisterModFunctionKey("MovementSettings", "MoveForwardKey", KeyCode.W);
            MoveBackwardKey = RegisterModFunctionKey("MovementSettings", "MoveBackwardKey", KeyCode.S);
            MoveLeftKey = RegisterModFunctionKey("MovementSettings", "MoveLeftKey", KeyCode.A);
            MoveRightKey = RegisterModFunctionKey("MovementSettings", "MoveRightKey", KeyCode.D);
            MoveUpKey = RegisterModFunctionKey("MovementSettings", "MoveUpKey", KeyCode.F);
            MoveDownKey = RegisterModFunctionKey("MovementSettings", "MoveDownKey", KeyCode.V);

            AllowDepthOfField = mod.GetSettings().GetValue<bool>("DepthOfFieldSettings", "AllowDepthOfField");
            DepthOfFieldStartingState = mod.GetSettings().GetValue<bool>("DepthOfFieldSettings", "DofStartingState");
            DofFocusDistance = mod.GetSettings().GetValue<int>("DepthOfFieldSettings", "FocusDistance");
            DofAperture = mod.GetSettings().GetValue<int>("DepthOfFieldSettings", "Aperture");
            DofFocalLength = mod.GetSettings().GetValue<int>("DepthOfFieldSettings", "FocalLength");
            DepthOfFieldToggleKey = RegisterModFunctionKey("DepthOfFieldSettings", "DepthOfFieldToggleKey", KeyCode.C);

            AllowGreenScreen = mod.GetSettings().GetValue<bool>("GreenScreenSettings", "AllowGreenScreen");
            GreenScreenWidth = mod.GetSettings().GetValue<int>("GreenScreenSettings", "GreenScreenWidth");
            GreenScreenHeight = mod.GetSettings().GetValue<int>("GreenScreenSettings", "GreenScreenHeight");
            GreenScreenColor = mod.GetSettings().GetValue<Color32>("GreenScreenSettings", "GreenScreenColor");
            GreenScreenToggleKey = RegisterModFunctionKey("GreenScreenSettings", "GreenScreenToggleKey", KeyCode.Z);

            AllowSuppressAIQuickToggle = mod.GetSettings().GetValue<bool>("MiscSettings", "AllowSuppressAiKey");
            SuppressAIToggleKey = RegisterModFunctionKey("MiscSettings", "SuppressAiKey", KeyCode.X);

            if (!HideHudInSelfieMode && hidingHud) { AutoUnhideHUD(); hidingHud = false; }

            if (!SuppressAIInSelfieMode && aiWasSupressed && GameManager.Instance != null)
            {
                GameManager.Instance.DisableAI = false;
                aiWasSupressed = false;
            }

            if (!AllowSuppressAIQuickToggle && !SuppressAIInSelfieMode && aiWasSupressed && GameManager.Instance != null)
            {
                GameManager.Instance.DisableAI = false;
                aiWasSupressed = false;
            }

            if (!AllowFreezeCameraKey && cameraLookFrozen) { cameraLookFrozen = false; }

            if (!AllowDepthOfField && Instance.volume != null)
            {
                Instance.volume.enabled = false;
            }
        }

        public static KeyCode RegisterModFunctionKey(string group, string optionName, KeyCode defaultKey)
        {
            string keyText = mod.GetSettings().GetValue<string>(group, optionName);
            if (Enum.TryParse(keyText, out KeyCode result))
                return result;
            else
            {
                Debug.Log("Selfie Cam: Invalid '" + optionName + "' keybind detected. Setting default. '" + defaultKey + "' Key");
                DaggerfallUI.AddHUDText("Selfie Cam:", 6f);
                DaggerfallUI.AddHUDText("Invalid '" + optionName + "' keybind detected. Setting default. '" + defaultKey + "' Key", 6f);
                return defaultKey;
            }
        }

        public static void RefreshVariousStates_OnStartGame(object sender, EventArgs e)
        {
            if (Instance.paperDoll)
            {
                Instance.StopPaperDoll();
            }

            if (hidingHud) { AutoUnhideHUD(); hidingHud = false; }

            if (aiWasSupressed && GameManager.Instance != null)
            {
                GameManager.Instance.DisableAI = false;
                aiWasSupressed = false;
            }

            cameraLookFrozen = false;

            initialBloomState = DaggerfallUnity.Settings.BloomEnable;
        }

        public static void RefreshVariousStates_OnSaveLoad(SaveData_v1 saveData)
        {
            if (Instance.paperDoll)
            {
                Instance.StopPaperDoll();
            }

            if (hidingHud) { AutoUnhideHUD(); hidingHud = false; }

            if (aiWasSupressed && GameManager.Instance != null)
            {
                GameManager.Instance.DisableAI = false;
                aiWasSupressed = false;
            }

            cameraLookFrozen = false;

            initialBloomState = DaggerfallUnity.Settings.BloomEnable;
        }

        private void Update()
        {
            if (GameManager.IsGamePaused || SaveLoadManager.Instance.LoadInProgress)
                return;

            // Handle key presses
            if (InputManager.Instance.GetKeyDown(SelfieModeToggleKey))
            {
                if (paperDoll)
                {
                    StopPaperDoll();
                }
                else
                {
                    StartPaperDoll();
                }
            }

            if (paperDoll && !GameManager.Instance.TransportManager.IsOnFoot)
            {
                StopPaperDoll();
            }

            // Toggle AI suppression state, if key is enabled
            if (AllowSuppressAIQuickToggle && InputManager.Instance.GetKeyDown(SuppressAIToggleKey))
            {
                if (GameManager.Instance != null)
                {
                    if (!GameManager.Instance.DisableAI)
                    {
                        GameManager.Instance.DisableAI = true;
                        aiWasSupressed = true;
                        DaggerfallUI.AddHUDText("Selfie Cam: AI Turned OFF", 2f);
                    }
                    else
                    {
                        GameManager.Instance.DisableAI = false;
                        aiWasSupressed = false;
                        DaggerfallUI.AddHUDText("Selfie Cam: AI Turned ON", 2f);
                    }
                }
            }

            //Update rotation cause there's a stupid issue for some reason
            if (paperDoll)
            {
                //Torch
                if (torch != null)
                {
                    Vector3 dist = (paperDoll.transform.position - scCamera.transform.position);
                    dist.y = 0;
                    torch.transform.position = paperDoll.transform.position + dist.normalized * -torchDist + new Vector3(0, torchHeight, 0);
                }

                // Toggle Camera Look Freeze State
                if (AllowFreezeCameraKey && InputManager.Instance.GetKeyDown(FreezeCameraKey))
                {
                    cameraLookFrozen = !cameraLookFrozen;
                }

                if (AllowPaperdollMovement)
                {
                    if (InputManager.Instance.GetKey(MoveForwardKey))
                    {
                        paperDoll.transform.localPosition = paperDoll.transform.localPosition - paperDoll.transform.forward * (PaperdollMovementSpeed * 0.01f);
                        pivot.transform.position = paperDoll.transform.position + new Vector3(0, .1f, 0);
                    }
                    if (InputManager.Instance.GetKey(MoveBackwardKey))
                    {
                        paperDoll.transform.localPosition = paperDoll.transform.localPosition - paperDoll.transform.forward * -(PaperdollMovementSpeed * 0.01f);
                        pivot.transform.position = paperDoll.transform.position + new Vector3(0, .1f, 0);
                    }
                    if (InputManager.Instance.GetKey(MoveLeftKey))
                    {
                        paperDoll.transform.localPosition = paperDoll.transform.localPosition - paperDoll.transform.right * -(PaperdollMovementSpeed * 0.01f);
                        pivot.transform.position = paperDoll.transform.position + new Vector3(0, .1f, 0);
                    }
                    if (InputManager.Instance.GetKey(MoveRightKey))
                    {
                        paperDoll.transform.localPosition = paperDoll.transform.localPosition - paperDoll.transform.right * (PaperdollMovementSpeed * 0.01f);
                        pivot.transform.position = paperDoll.transform.position + new Vector3(0, .1f, 0);
                    }
                    if (InputManager.Instance.GetKey(MoveUpKey))
                    {
                        paperDoll.transform.localPosition = paperDoll.transform.localPosition - paperDoll.transform.up * -(PaperdollMovementSpeed * 0.01f);
                        pivot.transform.position = paperDoll.transform.position + new Vector3(0, .1f, 0);
                    }
                    if (InputManager.Instance.GetKey(MoveDownKey))
                    {
                        paperDoll.transform.localPosition = paperDoll.transform.localPosition - paperDoll.transform.up * (PaperdollMovementSpeed * 0.01f);
                        pivot.transform.position = paperDoll.transform.position + new Vector3(0, .1f, 0);
                    }
                }

                // Update greenscreen object position if it exists, this way it will follow properly if moving camera view around, etc. 
                if (greenScreenObject) { greenScreenObject.transform.localPosition = paperDoll.transform.localPosition - paperDoll.transform.forward * 0.25f; }

                // Toggle Greenscreen Object 
                if (AllowGreenScreen && InputManager.Instance.GetKeyDown(GreenScreenToggleKey))
                {
                    ToggleGreenScreenObject();
                }

                //Toggle DOF
                if (AllowDepthOfField && InputManager.Instance.GetKeyDown(DepthOfFieldToggleKey))
                {
                    volume.enabled = !volume.enabled;
                }
            }
        }

        public static void AutoHideHUD()
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

        public static void AutoUnhideHUD()
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
            if (HideHudInSelfieMode || hidingHud) { AutoUnhideHUD(); hidingHud = false; }

            if (aiWasSupressed && GameManager.Instance != null)
            {
                GameManager.Instance.DisableAI = false;
                aiWasSupressed = false;
            }

            cameraLookFrozen = false;

            if (torch != null)
            {
                torch.transform.localPosition = torchPosition;
            }

            if (torchObject != null)
                Destroy(torchObject);

            if (greenScreenObject)
            {
                if (initialBloomState)
                    DaggerfallUnity.Settings.BloomEnable = true;

                Destroy(greenScreenObject);
            }

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

            scCamera.transform.parent = previousCameraParent.transform;
            scCamera.transform.localPosition = new Vector3(0, 0, 0);
            scCamera.transform.localRotation = Quaternion.identity;

            Destroy(volume.profile);
            Destroy(volume.gameObject);

            Destroy(scCamera.GetComponent<SC_MousePivot>());
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

            if (HideHudInSelfieMode) { AutoHideHUD(); hidingHud = true; }

            if (SuppressAIInSelfieMode && GameManager.Instance != null)
            {
                GameManager.Instance.DisableAI = true;
                aiWasSupressed = true;
            }

            torch = GetPlayerTorch();
            if (torch != null)
            {
                torchPosition = torch.transform.localPosition;

                if (showTorchBillboard)
                    SpawnTorch(torch);
            }

            paperDoll = SpawnPaperDoll(GameManager.Instance.PlayerObject.transform.position);

            GameManager.Instance.PlayerMotor.enabled = false;
            GameManager.Instance.RightHandWeapon.ShowWeapon = false;
            GameManager.Instance.WeaponManager.enabled = false;
            GameManager.Instance.PlayerMouseLook.enabled = false;
            GameManager.Instance.PlayerActivate.enabled = false;
            GameManager.Instance.PlayerObject.GetComponent<FPSSpellCasting>().enabled = false;

            headBobberEnabled = GameManager.Instance.PlayerObject.GetComponent<HeadBobber>().enabled;
            if (headBobberEnabled)
                GameManager.Instance.PlayerObject.GetComponent<HeadBobber>().enabled = false;

            //Camera Related Stuff
            pivot = new GameObject("Pivot");
            float yOffset = (SelfieModeYOffset - 50) * 0.01f;
            pivot.transform.position = GameManager.Instance.PlayerObject.transform.position + new Vector3(0, .1f + yOffset, 0);
            pivot.transform.forward = GameManager.Instance.PlayerObject.transform.forward;
            pivot.transform.localScale = new Vector3(StartingZoomLevel, StartingZoomLevel, StartingZoomLevel);

            previousCameraParent = GameManager.Instance.MainCamera.transform.parent;

            scCamera = GameManager.Instance.MainCamera;

            scCamera.transform.parent = pivot.transform;
            scCamera.transform.parent = pivot.transform;
            scCamera.transform.localPosition = new Vector3(0, 0, -2);
            scCamera.transform.localRotation = Quaternion.identity;

            var mousePivot = scCamera.gameObject.AddComponent<SC_MousePivot>();
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
            depthofField.focusDistance.Override(DofFocusDistance * 0.1f);
            depthofField.aperture.Override(DofAperture * 0.1f);
            depthofField.focalLength.Override(DofFocalLength);

            if (AllowDepthOfField)
            {
                if (DepthOfFieldStartingState)
                    volume.enabled = true;
                else
                    volume.enabled = false;
            }
            else
                volume.enabled = false;
        }

        public GameObject SpawnPaperDoll(Vector3 position)
        {
            GameObject go = new GameObject("Selfie Cam Paper Doll");
            float yOffset = (SelfieModeYOffset - 50) * 0.01f;
            go.transform.position = position + new Vector3(0, yOffset, 0);

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

                Texture2D texture = new Texture2D(GreenScreenWidth, GreenScreenHeight);
                Color32[] screenColors = new Color32[GreenScreenWidth * GreenScreenHeight];
                Color32 screenColor = GreenScreenColor;

                for (int i = 0; i < screenColors.Length; i++)
                {
                    screenColors[i] = screenColor;
                }

                texture.SetPixels32(screenColors);
                texture.Apply();

                billboard.SetMaterial(texture, new Vector2(GreenScreenWidth, GreenScreenHeight));

                Shader shader = Shader.Find("Unlit/Texture");
                Material material = new Material(shader);
                material.mainTexture = texture;

                MeshRenderer meshRenderer = greenScreenObject.GetComponent<MeshRenderer>();

                if (meshRenderer != null)
                    meshRenderer.sharedMaterial = material;

                if (DaggerfallUnity.Settings.BloomEnable)
                {
                    DaggerfallUnity.Settings.BloomEnable = false;
                    GameManager.Instance.StartGameBehaviour.DeployCoreGameEffectSettings(CoreGameEffectSettingsGroups.Bloom);
                    Debug.Log("Selfie Cam: Disabling Bloom effect for Green Screen Mode.");
                }
            }
            else
            {
                if (initialBloomState)
                {
                    DaggerfallUnity.Settings.BloomEnable = true;
                    GameManager.Instance.StartGameBehaviour.DeployCoreGameEffectSettings(CoreGameEffectSettingsGroups.Bloom);
                    Debug.Log("Selfie Cam: Enabling Bloom effect after exiting Green Screen Mode.");
                }

                Destroy(greenScreenObject);
            }
        }
    }
}

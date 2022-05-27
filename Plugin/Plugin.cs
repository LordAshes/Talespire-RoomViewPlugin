using BepInEx;
using BepInEx.Configuration;
using Bounce.Unmanaged;
using UnityEngine;

using System.Collections.Generic;
using System.Linq;
using System;
using System.Collections;

namespace LordAshes
{
    [BepInPlugin(Guid, Name, Version)]
    [BepInDependency(LordAshes.AssetDataPlugin.Guid, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(LordAshes.HideVolumeMenuPlugin.Guid, BepInDependency.DependencyFlags.HardDependency)]
    public partial class RoomViewPlugin : BaseUnityPlugin
    {
        // Plugin info
        public const string Name = "Room View Plug-In";              
        public const string Guid = "org.lordashes.plugins.roomview";
        public const string Version = "2.0.0.0";                    

        // Configuration
        private ConfigEntry<KeyboardShortcut> triggerKey { get; set; }
        private float liftDelay = 0.1f;
        private List<CreatureBoardAsset> revealers = new List<CreatureBoardAsset>();

        void Awake()
        {
            UnityEngine.Debug.Log("Room View Plugin: "+this.GetType().AssemblyQualifiedName+" Active.");

            triggerKey = Config.Bind("Shortcuts", "Trigger To Initialize Room View Plufgin", new KeyboardShortcut(KeyCode.H, KeyCode.RightControl));
            
            liftDelay = Config.Bind("Settings", "Room Entry Lift Delay", 0.1f).Value;

            Debug.Log("Room View Plugin: Subscribing To Location Changes");
            AssetDataPlugin.Subscribe("AssetLocation", UpdateHideVolumes);

            Utility.PostOnMainPage(this.GetType());
        }

        void Update()
        {
            if(Utility.StrictKeyCheck(triggerKey.Value))
            {
                string[] revealerNames = Config.Bind("Settings", "Names Of Minis That Reval Rooms", "Jon,Jane").Value.Split(',');

                revealers.Clear();

                foreach (CreatureBoardAsset asset in CreaturePresenter.AllCreatureAssets)
                {
                    if (revealerNames.Contains(AssetDataPlugin.Legacy.GetCreatureName(asset)))
                    {
                        Debug.Log("Room View Plugin: Adding '" + AssetDataPlugin.Legacy.GetCreatureName(asset) + "' (" + asset.CreatureId + ") As A Room Revealer.");
                        revealers.Add(asset);
                    }
                }

                if(HideVolumeMenuPlugin.Instance().CurrentHideVolumeStates.Count==0)
                {
                    SystemMessage.DisplayInfoText("Hide Volume Menu Not Opened Or\r\nHide Volumes Not Defined");
                }
            }
        }

        private void UpdateHideVolumes(AssetDataPlugin.DatumChange change)
        {
            Debug.Log("Room View Plugin: Location Change Update");

            CreatureBoardAsset source;
            CreaturePresenter.TryGetAsset(new CreatureGuid(change.source), out source);
            if(!revealers.Contains(source))
            {
                Debug.Log("Room View Plugin: Non-Revealer Location Change. Abort Location Change Update");
            }
            List<string> locations = new List<string>();
            foreach(CreatureBoardAsset asset in revealers)
            {
                string location = AssetDataPlugin.ReadInfo(asset.CreatureId.ToString(), "AssetLocation");
                if (!locations.Contains(location)) { locations.Add(location); }
            }
            Debug.Log("Room View Plugin: Occupied Location(s): "+String.Join(",",locations));
            NGuid[] hvs = HideVolumeMenuPlugin.Instance().CurrentHideVolumeStates.Keys.ToArray<NGuid>();
            foreach(NGuid hv in hvs)
            {
                string nameHV = HideVolumeMenuPlugin.Instance().CurrentHideVolumeStates[hv].Name;
                if (locations.Contains(nameHV)) 
                {
                    Debug.Log("Room View Plugin: Room '" + nameHV + "' Is Occupied. Revaling.");
                    HideVolumeMenuPlugin.Instance().CurrentHideVolumeStates[hv].State = false; 
                } 
                else 
                {
                    Debug.Log("Room View Plugin: Room '" + nameHV + "' Is Not Occupied. Hiding.");
                    HideVolumeMenuPlugin.Instance().CurrentHideVolumeStates[hv].State = true; 
                }
            }
            if (source != null) { StartCoroutine("LiftDelay", new object[] { source }); }
        }

        IEnumerator LiftDelay(object[] inputs)
        {
            Debug.Log("Room View Plugin: Waiting To Lift Asset "+AssetDataPlugin.Legacy.GetCreatureName((CreatureBoardAsset)inputs[0]));
            yield return new WaitForSeconds(liftDelay);
            Debug.Log("Room View Plugin: Lifting Asset " + inputs[0].ToString());
            ((CreatureBoardAsset)inputs[0]).Pickup();
            ((CreatureBoardAsset)inputs[0]).DropAtCurrentLocation();
        }
    }
}

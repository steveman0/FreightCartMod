using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FortressCraft.Community.Utilities;

public class FreightSystemMonitor : MachineEntity
{
    private bool mbLinkedToGO = false;
    private GameObject HoloPreview;
    //private SystemMonitorWindow MonitorPanel = new SystemMonitorWindow();

    public FreightSystemMonitor(ModCreateSegmentEntityParameters parameters)
        : base(parameters)
    {
        this.mbNeedsLowFrequencyUpdate = true;
        this.mbNeedsUnityUpdate = true;
    }

    public override string GetPopupText()
    {
        //Hold down right alt and press left alt for debug printed to log
        if (Input.GetKeyDown(KeyCode.LeftAlt) && Input.GetKey(KeyCode.RightAlt))
        {
            FreightCartManager.instance.DebugFreight();
            FloatingCombatTextManager.instance.QueueText(this.mnX, this.mnY + 1L, this.mnZ, 1f, "Freight Registry Written to Output Log.", Color.yellow, 2f, 64f);
        }
        if (Input.GetKeyDown(KeyCode.LeftControl) && Input.GetKey(KeyCode.RightControl) || Input.GetButtonDown("Extract"))
        {
            Debug.LogWarning("------------FREIGHT CART DEBUG---------------\nTotal Freight carts spawned: " + ManagerSync.instance.CartCounter.ToString());
            FloatingCombatTextManager.instance.QueueText(this.mnX, this.mnY + 1L, this.mnZ, 1f, "Total Freight Carts Spawned: " + ManagerSync.instance.CartCounter.ToString(), Color.yellow, 2f, 64f);
            FreightCartMod.LiveUpdateTime = FreightCartMod.UpdateCounter;
            FreightCartMod.monitor = this;
        }
        string str1 = "Freight System Monitor\n";
        string str2 = "Press E to access Freight System Status\n";
        string str3 = "Press Q to display global cart status";

        return str1 + str2 + str3;
    }

    public override void DropGameObject()
    {
        base.DropGameObject();
        this.mbLinkedToGO = false;
    }

    public override void UnitySuspended()
    {
        if (this.HoloPreview != null)
            Object.Destroy(this.HoloPreview);
        this.HoloPreview = null;
    }

    public override void UnityUpdate()
    {
        //UIUtil.DisconnectUI(this);
        if (!this.mbLinkedToGO)
        {
            if (this.mWrapper == null || this.mWrapper.mGameObjectList == null)
                return;
            if (this.mWrapper.mGameObjectList[0].gameObject == null)
                Debug.LogError("AutoBuilder missing game object #0 (GO)?");

            GameObject builder = Extensions.Search(this.mWrapper.mGameObjectList[0].gameObject.transform, "Builder").gameObject;
            Light WorkLight = Extensions.Search(this.mWrapper.mGameObjectList[0].gameObject.transform, "WorkLight").gameObject.GetComponent<Light>();
            builder.SetActive(false);
            WorkLight.enabled = false;

            Component[] obj = this.mWrapper.mGameObjectList[0].gameObject.GetComponentsInChildren(typeof(Component));
            foreach (Component x in obj)
            {
                //Debug.Log("mWrapper Object: " + x + " name: " + x.name);
                if (x.name != "Static_Base" && x.name != "ForwardHolder" && x.name != "_AutoBuilder(Clone)")
                    GameObject.Destroy(x.gameObject); //Instead of x.gameObject.SetActive(false);
            }

            this.mbLinkedToGO = true;

            int index = (int)ItemEntry.mEntries[110].Object;
            this.HoloPreview = (GameObject)Object.Instantiate(SpawnableObjectManagerScript.instance.maSpawnableObjects[index], this.mWrapper.mGameObjectList[0].gameObject.transform.position + new Vector3(0.0f, 0.75f, 0.0f), Quaternion.identity);
            this.HoloPreview.transform.parent = this.mWrapper.mGameObjectList[0].gameObject.transform;
            if ((Object)this.HoloPreview.GetComponent<Renderer>() != (Object)null)
            {
                this.HoloPreview.GetComponent<Renderer>().material = PrefabHolder.instance.HoloPreviewMaterial;
                this.HoloPreview.GetComponent<Renderer>().castShadows = false;
                this.HoloPreview.GetComponent<Renderer>().receiveShadows = false;
            }
            this.HoloPreview.gameObject.AddComponent<RotateConstantlyScript>();
            this.HoloPreview.gameObject.GetComponent<RotateConstantlyScript>().YRot = 1f;
            this.HoloPreview.gameObject.GetComponent<RotateConstantlyScript>().XRot = 0.35f;
            this.HoloPreview.SetActive(true);
        }
    }
}


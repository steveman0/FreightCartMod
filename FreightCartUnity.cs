// Decompiled with JetBrains decompiler
// Type: Minecart_Unity
// Assembly: Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: F8E46F69-4375-423A-BBF9-6D21B582CAE3
// Assembly location: C:\Games\Steam\steamapps\common\FortressCraft\64\FC_64_Data\Managed\Assembly-CSharp.dll

using System;
using UnityEngine;
using UnityEngine.Rendering;
using System.Reflection;

public class FreightCartUnity : MonoBehaviour
{
    private Vector3 mCurrentPos = Vector3.zero;
    private FreightCartMob mob;
    private Animation mAnim;
    private Renderer[] mRenderer;
    private GameObject AttackBlob;
    public bool DestroyCalled;
    private bool mbRenderer;
    private bool mbDoorRenderer;
    private ParticleSystem LoadParticles;
    private ParticleSystem LoadParticlesDust;
    public static Material T1_Mat;
    public static Material T2_Mat;
    public static Material T3_Mat;
    public static Material T4_Mat;
    private GameObject Door_L;
    private GameObject Door_R;
    private GameObject Body;
    private Light mHoverLight;
    public static int MinecartNumber;
    private AudioSource mAudio;
    private float mrNotLoadingTimer;
    private float mrLoadingTimer;
    private int mnHealth;
    private float mrSwingTimer;
    private float GravityRate;
    private int mnDeadFrames;
    private bool mbConfigured;
    private int mnUpdates;
    private Vector3 mCurrentMobPos;
    private float mrCurrentMobPosUpdateDelay;
    private float mrCurrentMobSpeed;
    private float mrCurrentBlockOffset;
    private Vector3 mLook;
    private float mrDoorAngle;
    private float mrTimeUntilNextVisualPositionUpdate;
    private float mrGlow;
    private MaterialPropertyBlock mMPB;
    private float mrPosUpdateTimer;
    private Vector3 mLerp;
    private Vector3 mPreviousMCPosition;

    private void Start()
    {
        //this.gameObject.AddComponent<Rideable>();
        GameObjectWrapper gameObjectWrapper = this.GetComponent<SpawnableObjectScript>().wrapper;
        if (gameObjectWrapper == null)
            return;
        this.gameObject.name = this.gameObject.name + ".ID : " + FreightCartUnity.MinecartNumber;
        ++FreightCartUnity.MinecartNumber;
        this.mHoverLight = this.transform.Find("HoverLight").GetComponent<Light>();
        if (this.mHoverLight == null)
            Debug.LogError("Error, Minecart had no hover light?");
        this.mob = gameObjectWrapper.mPayload as FreightCartMob;
        this.Body = Extensions.Search(this.transform, "Minecart GFX").gameObject;
        this.mnHealth = -999999;
        this.mRenderer = this.GetComponentsInChildren<Renderer>();
        this.mbRenderer = true;
        this.mbDoorRenderer = true;
        this.LoadParticles = Extensions.Search(this.transform, "Loading Particles").GetComponent<ParticleSystem>();
        this.LoadParticlesDust = Extensions.Search(this.transform, "Loading Particles Dust").GetComponent<ParticleSystem>();
        this.Door_L = Extensions.Search(this.transform, "Minecart Door L").gameObject;
        this.Door_R = Extensions.Search(this.transform, "Minecart Door R").gameObject;
        if (DateTime.Now.Month != 4 || DateTime.Now.Day > 1)
            Extensions.Search(this.transform, "AprilFool").gameObject.SetActive(false);
        this.mAudio = this.GetComponent<AudioSource>();
        Material material = (Material)null;
        if (this.mob.mObjectType == SpawnableObjectEnum.Minecart_T1)
        {
            if ((UnityEngine.Object)Minecart_Unity.T1_Mat == (UnityEngine.Object)null)
                Minecart_Unity.T1_Mat = Resources.Load("DynamicTextures/Minecart_dif") as Material;
            material = Minecart_Unity.T1_Mat;
        }
        if (this.mob.mObjectType == SpawnableObjectEnum.Minecart_T2)
        {
            if ((UnityEngine.Object)Minecart_Unity.T2_Mat == (UnityEngine.Object)null)
                Minecart_Unity.T2_Mat = Resources.Load("DynamicTextures/Minecart_dif T2") as Material;
            material = Minecart_Unity.T2_Mat;
        }
        if (this.mob.mObjectType == SpawnableObjectEnum.Minecart_T3)
        {
            if ((UnityEngine.Object)Minecart_Unity.T3_Mat == (UnityEngine.Object)null)
                Minecart_Unity.T3_Mat = Resources.Load("DynamicTextures/Minecart_dif T3") as Material;
            material = Minecart_Unity.T3_Mat;
        }
        if (this.mob.mObjectType == SpawnableObjectEnum.Minecart_T4)
        {
            if ((UnityEngine.Object)Minecart_Unity.T4_Mat == (UnityEngine.Object)null)
                Minecart_Unity.T4_Mat = Resources.Load("DynamicTextures/Minecart_dif T4") as Material;
            material = Minecart_Unity.T4_Mat;
        }
        if (material != null)
        {
            this.Door_L.GetComponent<Renderer>().material = material;
            this.Door_R.GetComponent<Renderer>().material = material;
            this.Body.GetComponent<Renderer>().material = material;
        }
        this.mCurrentPos = this.transform.position;
        this.mMPB = new MaterialPropertyBlock();
    }

    private void UpdateAudio()
    {
        Profiler.BeginSample("UpdateAudio");
        if ((double)this.mob.mDistanceToPlayer > 12.0)
        {
            if (this.mAudio.isPlaying)
                this.mAudio.Stop();
        }
        else
        {
            if (!this.mAudio.isPlaying)
            {
                this.mAudio.volume = 0.0f;
                this.mAudio.Play();
            }
            float num = this.mrCurrentMobSpeed;
            if (this.mob.meLoadState == FreightCartMob.eLoadState.eLoading)
                num = 0.0f;
            if (this.mob.meLoadState == FreightCartMob.eLoadState.eUnloading)
                num = 0.0f;
            this.mAudio.volume += (float)(((double)num - (double)this.mAudio.volume) * (double)Time.deltaTime / 10.0);
            this.mAudio.pitch += (float)(((double)(this.mrCurrentMobSpeed * 1f) - (double)this.mAudio.pitch) * (double)Time.deltaTime / 1.0);
            this.mAudio.priority = 64 + (int)this.mob.mDistanceToPlayer * 4;
        }
        Profiler.EndSample();
    }

    private void Update()
    {
        this.mrTimeUntilNextVisualPositionUpdate -= Time.deltaTime;
        if (DestroyCalled)
            GetComponent<Rideable>().DestroyCalled = true;
        if (this.mob == null)
            return;
        if (this.mob.mnHealth <= 0 && this.mbConfigured || !this.mob.mbActive)
        {
            ++this.mnDeadFrames;
            if (this.mnDeadFrames <= 600)
                return;
            Debug.LogWarning(("Warning, Minecart " + this.mob.mnServerID + " should have died 10 seconds ago! Destroy() was " + (!this.DestroyCalled ? "NOT " : string.Empty) + "called!"));
            Destroy(this.gameObject);
        }
        else
        {
            Profiler.BeginSample("Minecart Update");
            ++this.mnUpdates;
            if (this.mnUpdates % 60 != 0)
                ;
            if (!this.mbConfigured)
            {
                this.mbConfigured = true;
                this.transform.forward = this.mob.mLook;
                this.mnHealth = this.mob.mnHealth;
            }
            else if (this.mob.mnHealth < this.mnHealth)
            {
                this.mnHealth = this.mob.mnHealth;
                AudioSoundEffectManager.instance.DoSplatImpact(this.transform.position, 1f);
            }
            this.UpdateAudio();
            Profiler.BeginSample("Particles");
            if ((double)this.mob.mDistanceToPlayer < 32.0)
            {
                if (this.mob.meLoadState == FreightCartMob.eLoadState.eLoading)
                {
                    this.LoadParticles.transform.localPosition = new Vector3(0.0f, 0.7131882f, 0.0f);
                    this.LoadParticlesDust.transform.localPosition = new Vector3(0.0f, 0.7131882f, 0.0f);
                }
                else
                {
                    this.LoadParticles.transform.localPosition = new Vector3(0.0f, -0.4f, 0.0f);
                    this.LoadParticlesDust.transform.localPosition = new Vector3(0.0f, -0.4f, 0.0f);
                }
                float num = (float)(1.0 - (double)this.mob.mDistanceToPlayer / 32.0);
                if ((double)num < 0.0)
                    num = 0.0f;
                this.LoadParticles.emissionRate = this.mob.mrVisualLoadTimer * 100f * num;
                this.LoadParticlesDust.emissionRate = this.mob.mrVisualLoadTimer * 100f * num;
            }
            else
            {
                this.LoadParticles.emissionRate = 0.0f;
                this.LoadParticlesDust.emissionRate = 0.0f;
            }
            this.mob.mrVisualLoadTimer -= Time.deltaTime;
            this.mrPosUpdateTimer += Time.deltaTime;
            Profiler.EndSample();
            Profiler.BeginSample("UpdateDoors");
            if ((double)this.mob.mDistanceToPlayer < 16.0)
            {
                if (this.mob.meLoadState == FreightCartMob.eLoadState.eLoading)
                {
                    if ((double)this.mrDoorAngle < 90.0)
                        ++this.mrDoorAngle;
                    this.Door_L.transform.localRotation = Quaternion.Euler(0.0f, 0.0f + this.mrDoorAngle, 180f);
                    this.Door_R.transform.localRotation = Quaternion.Euler(0.0f, 360f - this.mrDoorAngle, 180f);
                }
                else
                {
                    this.mrDoorAngle *= 0.75f;
                    this.Door_L.transform.localRotation = Quaternion.Euler(0.0f, 0.0f + this.mrDoorAngle, 180f);
                    this.Door_R.transform.localRotation = Quaternion.Euler(0.0f, 360f - this.mrDoorAngle, 180f);
                }
            }
            Profiler.EndSample();
            this.mrCurrentMobPosUpdateDelay -= Time.deltaTime;
            Profiler.BeginSample("DequeuePath");
            Debug.DrawRay(this.mCurrentMobPos, this.mLook * 5f, Color.magenta, 1f);
            Vector8 vector8;
            if (this.mob.maCartPositions.Count > 0 && (double)this.mrCurrentMobPosUpdateDelay < 0.0 && this.mob.maCartPositions.TryDequeue(out vector8))
            {
                this.mrCurrentMobPosUpdateDelay += 1f / vector8.w;
                this.mrCurrentMobPosUpdateDelay = 0.0f;
                this.mCurrentMobPos.x = vector8.x;
                this.mCurrentMobPos.y = vector8.y;
                this.mCurrentMobPos.z = vector8.z;
                this.mrCurrentMobSpeed = vector8.w;
                this.mLook.x = vector8.a;
                this.mLook.y = vector8.b;
                this.mLook.z = vector8.c;
                if (this.mob.meLoadState == FreightCartMob.eLoadState.eLoading)
                    this.mLook.y = 0.0f;
                if (this.mob.meLoadState == FreightCartMob.eLoadState.eUnloading)
                    this.mLook.y = 0.0f;
                Debug.DrawRay(this.mCurrentMobPos, this.mLook * 5f, Color.cyan, 5f);
                if (this.mCurrentPos == Vector3.zero)
                    this.mCurrentPos = this.mCurrentMobPos;
                if ((double)(this.mCurrentMobPos - this.mCurrentPos).sqrMagnitude > 16.0)
                    this.mCurrentPos = this.mCurrentMobPos;
                if (this.mob.maCartPositions.Count != 0)
                    ;
            }
            Profiler.EndSample();
            Vector3 vector3_1 = this.mCurrentMobPos;
            if ((double)this.mLook.y > 0.0)
                vector3_1.y += this.mLook.y;
            Profiler.BeginSample("UpdatePosition");
            if (this.mPreviousMCPosition != vector3_1)
            {
                this.mrPosUpdateTimer = 0.0f;
                if (this.mCurrentPos != Vector3.zero)
                {
                    this.mLerp = vector3_1 - this.mCurrentPos;
                    this.mLerp.Normalize();
                    this.mLerp *= this.mrCurrentMobSpeed;
                }
                this.mPreviousMCPosition = vector3_1;
            }
            if ((double)this.mrPosUpdateTimer > 0.349999994039536)
            {
                Vector3 vector3_2 = this.mCurrentMobPos;
                vector3_2.x = (float)Mathf.FloorToInt(vector3_2.x);
                vector3_2.y = (float)Mathf.FloorToInt(vector3_2.y);
                vector3_2.z = (float)Mathf.FloorToInt(vector3_2.z);
                vector3_2.x += 0.5f;
                vector3_2.y += 0.5f;
                vector3_2.z += 0.5f;
                this.mCurrentPos += (vector3_2 - this.mCurrentPos) * Time.deltaTime * 4f;
                if ((double)this.mrTimeUntilNextVisualPositionUpdate <= 0.0)
                    this.transform.forward += (this.mob.mLook - this.transform.forward) * Time.deltaTime * 4f;
            }
            else if ((double)this.mrTimeUntilNextVisualPositionUpdate <= 0.0)
            {
                float num1 = 1f;
                if (this.mob.meLoadState != FreightCartMob.eLoadState.eLoading && this.mob.meLoadState != FreightCartMob.eLoadState.eUnloading)
                {
                    float num2 = Vector3.Dot(this.mLerp, this.transform.forward);
                    if ((double)num2 < 0.0)
                        num1 += -num2;
                    this.transform.forward += (this.mLerp - this.transform.forward) * Time.deltaTime * (float)((double)this.mrCurrentMobSpeed * 1.5 + 0.5) * num1;
                }
            }
            if (this.mLerp != Vector3.zero)
            {
                this.mCurrentPos += this.mLerp * Time.deltaTime;
                if ((double)this.mrTimeUntilNextVisualPositionUpdate <= 0.0)
                {
                    this.transform.position = (this.mCurrentPos + this.transform.position) / 2f;
                    this.transform.position = this.mCurrentPos;
                }
            }
            else if ((double)this.mrTimeUntilNextVisualPositionUpdate <= 0.0)
                this.transform.position = (this.mCurrentPos + this.transform.position) / 2f;
            Profiler.EndSample();
            if ((double)this.mrTimeUntilNextVisualPositionUpdate <= 0.0)
            {
                float num1 = this.mob.mDistanceToPlayer - 10f;
                if ((double)num1 < 0.0)
                    num1 = 0.0f;
                float num2 = num1 / 256f - CamDetail.FPS * 0.002083f;
                if (!this.mbRenderer)
                    num2 = 1f;
                this.mrTimeUntilNextVisualPositionUpdate += num2;
            }
            this.ConfigLOD();
            Profiler.EndSample();
        }
    }

    private void ConfigLOD()
    {
        Profiler.BeginSample("Minecart LOD");
        bool flag1 = true;
        if (this.mob.mSegment.mbOutOfView)
            flag1 = false;
        if ((double)this.mob.mDistanceToPlayer > (double)CamDetail.SegmentDrawDistance)
            flag1 = false;
        if ((double)this.mob.mVectorToPlayer.y > 32.0)
            flag1 = false;
        if ((double)this.mob.mVectorToPlayer.y < -32.0)
            flag1 = false;
        if ((double)this.mob.mDotWithPlayerForwards < -4.0)
            flag1 = false;
        if (!this.mob.mbRaycastVisible)
            flag1 = false;
        bool flag2 = flag1;
        if (this.mbRenderer != flag1)
        {
            this.mbRenderer = flag1;
            for (int index = 0; index < this.mRenderer.Length; ++index)
                this.mRenderer[index].enabled = flag1;
            if (this.mbRenderer)
                this.mrTimeUntilNextVisualPositionUpdate = 0.0f;
        }
        if ((double)this.mob.mDistanceToPlayer > 24.0)
            flag2 = false;
        if (flag2 != this.mbDoorRenderer)
        {
            this.mbDoorRenderer = flag2;
            this.Door_L.GetComponent<Renderer>().enabled = this.mbDoorRenderer;
            this.Door_R.GetComponent<Renderer>().enabled = this.mbDoorRenderer;
        }
        if ((double)this.mob.mDistanceToPlayer > 16.0)
        {
            this.Door_L.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            this.Door_R.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        }
        else
        {
            this.Door_L.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.On;
            this.Door_R.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.On;
        }
        if ((UnityEngine.Object)this.mHoverLight == (UnityEngine.Object)null)
        {
            Debug.LogWarning((object)("Warning, we lost the hoverlight?" + this.gameObject.name));
            this.mHoverLight = GameObject.Find("HoverLight").GetComponent<Light>();
            if ((UnityEngine.Object)this.mHoverLight == (UnityEngine.Object)null)
                Debug.LogError((object)"Error, Minecart had no hover light?");
        }
        if (this.mbRenderer)
        {
            float num = 8f;
            if (this.mob.mObjectType != SpawnableObjectEnum.Minecart_T10)
                num = (float)((double)this.mob.mnUsedStorage / (double)this.mob.mnMaxStorage * 8.0);
            if ((double)this.mob.mDistanceToPlayer < (double)CamDetail.FPS)
            {
                this.mHoverLight.intensity += this.mHoverLight.intensity - num;
                if ((double)num < 0.100000001490116)
                    this.mHoverLight.enabled = false;
                else
                    this.mHoverLight.enabled = true;
            }
            else if (this.mHoverLight.enabled)
            {
                this.mHoverLight.intensity *= 0.9f;
                if ((double)this.mHoverLight.intensity < 0.100000001490116)
                    this.mHoverLight.enabled = false;
            }
            if ((double)this.mob.mDistanceToPlayer < 64.0 && this.mob.mObjectType != SpawnableObjectEnum.Minecart_T10)
            {
                this.mrGlow += (num - this.mrGlow) * Time.deltaTime;
                this.mMPB.SetFloat("_GlowMult", this.mrGlow);
                this.Door_L.GetComponent<Renderer>().SetPropertyBlock(this.mMPB);
                this.Door_R.GetComponent<Renderer>().SetPropertyBlock(this.mMPB);
                this.Body.GetComponent<Renderer>().SetPropertyBlock(this.mMPB);
            }
        }
        else
        {
            Profiler.BeginSample("No Render");
            this.mHoverLight.enabled = false;
            Profiler.EndSample();
        }
        Profiler.EndSample();
    }
}

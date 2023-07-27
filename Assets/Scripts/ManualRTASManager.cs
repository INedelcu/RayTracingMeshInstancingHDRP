using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UI;

[ExecuteInEditMode]
public class ManualRTASManager : MonoBehaviour
{
    public Toggle enableInstancingToggle;
    
    public Text fpsText;

    public Mesh mesh = null;
    public Material material1 = null;
    public Material material2 = null;
    public Material material3 = null;

    RayTracingAccelerationStructure rtas = null;
    List<Matrix4x4> matrices1 = new List<Matrix4x4>();
    List<Matrix4x4> matrices2 = new List<Matrix4x4>();
    List<Matrix4x4> matrices3 = new List<Matrix4x4>();

    private float lastRealtimeSinceStartup = 0;
    private float updateFPSTimer = 0.2f;

    void Update()
    {
        if (fpsText)
        {
            float deltaTime = Time.realtimeSinceStartup - lastRealtimeSinceStartup;
            updateFPSTimer += deltaTime;

            if (updateFPSTimer >= 0.2f)
            {
                float fps = 1.0f / Mathf.Max(deltaTime, 0.0001f);
                fpsText.text = "FPS: " + Mathf.Ceil(fps).ToString();
                updateFPSTimer = 0.0f;
            }

            lastRealtimeSinceStartup = Time.realtimeSinceStartup;
        }

        HDRenderPipeline hdrp = RenderPipelineManager.currentPipeline is HDRenderPipeline ? (HDRenderPipeline)RenderPipelineManager.currentPipeline : null;
        if (hdrp != null)
        {
            var hdCamera = HDCamera.GetOrCreate(GetComponent<Camera>());

            if (rtas == null)
                rtas = new RayTracingAccelerationStructure();
            
            rtas.ClearInstances();

            RayTracingInstanceCullingConfig cullingConfig = new RayTracingInstanceCullingConfig();

            cullingConfig.subMeshFlagsConfig.opaqueMaterials = RayTracingSubMeshFlags.Enabled | RayTracingSubMeshFlags.ClosestHitOnly;
            cullingConfig.subMeshFlagsConfig.alphaTestedMaterials = RayTracingSubMeshFlags.Enabled;
            cullingConfig.subMeshFlagsConfig.transparentMaterials = RayTracingSubMeshFlags.Disabled;

            RayTracingInstanceCullingTest cullingTest = new RayTracingInstanceCullingTest();
            cullingTest.allowAlphaTestedMaterials = true;
            cullingTest.allowOpaqueMaterials = true;
            cullingTest.allowTransparentMaterials = false;
            cullingTest.instanceMask = 255;
            cullingTest.layerMask = -1;
            cullingTest.shadowCastingModeMask = -1;

            cullingConfig.instanceTests = new RayTracingInstanceCullingTest[1];
            cullingConfig.instanceTests[0] = cullingTest;

            rtas.CullInstances(ref cullingConfig);

            try
            {
                bool enableInstancing = !enableInstancingToggle || enableInstancingToggle.isOn;

                if (mesh != null && material1 != null)
                {
                    RayTracingMeshInstanceConfig config = new RayTracingMeshInstanceConfig(mesh, 0, material1);

                    config.subMeshFlags = RayTracingSubMeshFlags.Enabled | RayTracingSubMeshFlags.ClosestHitOnly;

                    // Not providing SH coeffs at all.
                    config.lightProbeUsage = LightProbeUsage.CustomProvided;

                    if (enableInstancing)
                    {
                        rtas.AddInstances(config, matrices1);
                    }
                    else
                    {
                        for (int i = 0; i < matrices1.Count; i++)
                        {
                            rtas.AddInstance(config, matrices1[i]);
                        }
                    }
                }

                if (mesh != null && material2 != null)
                {
                    RayTracingMeshInstanceConfig config = new RayTracingMeshInstanceConfig(mesh, 0, material2);

                    config.subMeshFlags = RayTracingSubMeshFlags.Enabled | RayTracingSubMeshFlags.ClosestHitOnly;

                    // Not providing SH coeffs at all.
                    config.lightProbeUsage = LightProbeUsage.CustomProvided;

                    if (enableInstancing)
                    {
                        rtas.AddInstances(config, matrices2);
                    }
                    else
                    {
                        for (int i = 0; i < matrices2.Count; i++)
                        {
                            rtas.AddInstance(config, matrices2[i]);
                        }
                    }
                }

                if (mesh != null && material3 != null)
                {
                    RayTracingMeshInstanceConfig config = new RayTracingMeshInstanceConfig(mesh, 0, material3);

                    config.subMeshFlags = RayTracingSubMeshFlags.Enabled | RayTracingSubMeshFlags.ClosestHitOnly;

                    // Not providing SH coeffs at all.
                    config.lightProbeUsage = LightProbeUsage.CustomProvided;

                    if (enableInstancing)
                    {
                        rtas.AddInstances(config, matrices3);
                    }
                    else
                    {
                        for (int i = 0; i < matrices3.Count; i++)
                        {
                            rtas.AddInstance(config, matrices3[i]);
                        }
                    }
                }

            }
            catch (Exception e)
            {
                Debug.Log("An exception occurred: " + e.Message);
            }
         
            // Build the RTAS
            rtas.Build(transform.position);

            // Assign it to the camera
            hdCamera.rayTracingAccelerationStructure = rtas;
        }
    }

    private void GenerateInstanceMatrices(in Vector3 origin, out List<Matrix4x4> matrices)
    {
        Matrix4x4 m = Matrix4x4.identity;
        Vector3 pos = Vector3.zero;

        matrices = new List<Matrix4x4>(32*32*32);

        for (int k = 0; k < 32; k++) //radial
        {
            for (int j = 0; j < 32; j++) //vertical
            {
                for (int i = 0; i < 32; i++) //circular
                {
                    float angle = j * Mathf.Pow(k * 0.004f, 2) + 2 * Mathf.PI * i / 31;
                    float radius = 5.0f + k * (1 + Mathf.Pow(j * 0.02f, 1.6f));
                    pos.x = radius * Mathf.Cos(angle);
                    pos.y = j;
                    pos.z = radius * Mathf.Sin(angle);
                    m.SetTRS(pos + origin, Quaternion.identity, Vector3.one);
                    matrices.Add(m);
                }
            }
        }
    }

    private void OnEnable()
    {
        GenerateInstanceMatrices(new Vector3(-55.0f, 0, -50.0f), out matrices1);
        GenerateInstanceMatrices(new Vector3(55.0f, 0, -50.0f), out matrices2);
        GenerateInstanceMatrices(new Vector3(0.0f, 0, 50.0f), out matrices3);
    }

    void OnDestroy()
    {
        if (rtas != null)
            rtas.Dispose();
    }
}

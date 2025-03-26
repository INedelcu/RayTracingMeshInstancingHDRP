using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UI;
using static ManualRTASManager;

[ExecuteInEditMode]
public class ManualRTASManager : MonoBehaviour
{
    public Text fpsText;

    public Material material = null;

    RayTracingAccelerationStructure rtas = null;
    List<Matrix4x4> matrices = new List<Matrix4x4>();

    GraphicsBuffer aabbBuffer = null;

    private float lastRealtimeSinceStartup = 0;
    private float updateFPSTimer = 0.2f;

    public struct AABB
    {
        public Vector3 min;
        public Vector3 max;
    }

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

            RayTracingSubMeshFlagsConfig subMeshFlagsConfig = new RayTracingSubMeshFlagsConfig();

            subMeshFlagsConfig.opaqueMaterials = RayTracingSubMeshFlags.Enabled | RayTracingSubMeshFlags.ClosestHitOnly;
            subMeshFlagsConfig.alphaTestedMaterials = RayTracingSubMeshFlags.Enabled;
            subMeshFlagsConfig.transparentMaterials = RayTracingSubMeshFlags.Disabled;

            cullingConfig.subMeshFlagsConfig = subMeshFlagsConfig;

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
                if (material != null)
                {
                    RayTracingAABBsInstanceConfig config = new RayTracingAABBsInstanceConfig();

                    config.aabbBuffer = aabbBuffer;
                    config.aabbCount = aabbBuffer.count;
                    config.aabbOffset = 0;
                    config.dynamicGeometry = false;
                    config.material = material;
                    config.materialProperties = new MaterialPropertyBlock();
                    config.materialProperties.SetBuffer("AABBs", aabbBuffer);

                    rtas.AddInstance(config, Matrix4x4.identity);
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
        GenerateInstanceMatrices(new Vector3(0, 0, 0), out matrices);

        {
            aabbBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, matrices.Count, 6 * sizeof(float));

            AABB[] aabbs = new AABB[matrices.Count];
            for (int i = 0; i < matrices.Count; i++)
            {
                AABB aabb = new AABB();

                Vector3 center = matrices[i].GetPosition();
                Vector3 size = new Vector3(0.3f, 0.3f, 0.3f);

                aabb.min = center - size;
                aabb.max = center + size;

                aabbs[i] = aabb;
            }
            aabbBuffer.SetData(aabbs);
        }
    }

    void OnDestroy()
    {
        matrices.Clear();
        rtas?.Dispose();
        aabbBuffer?.Dispose();
    }
}

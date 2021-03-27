using UnityEngine;
using UnityEngine.Rendering;

public class HiksPipeline : RenderPipeline
{
    private const string CommandName = "Render camera";
    
    private CommandBuffer _buffer = new CommandBuffer()
    {
        name = CommandName
    };
    
    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        foreach (var camera in cameras)
        {
            Render(context, camera);
        }
    }

    void Render(ScriptableRenderContext context, Camera camera)
    {
        ScriptableCullingParameters cullingParameters;
        if (!camera.TryGetCullingParameters(out cullingParameters))
        {
            Debug.LogWarning($"Couldn't get culling parameters for camera {camera.name}");
            return;
        }
        CullingResults cull = context.Cull(ref cullingParameters);
        

        context.SetupCameraProperties(camera);
        
        var clearFlags = camera.clearFlags;
        _buffer.ClearRenderTarget(
            (clearFlags & CameraClearFlags.Depth) != 0,
            (clearFlags & CameraClearFlags.Color) != 0,
            camera.backgroundColor
        );
        //_buffer.BeginSample(CommandName);
        context.ExecuteCommandBuffer(_buffer);

        // TODO: Optimize those allocations
        var opaqueSortingSettings = new SortingSettings(camera)
        {
            criteria = SortingCriteria.CommonOpaque
        };
        var opaqueDrawSettings = new DrawingSettings(new ShaderTagId("SRPDefaultUnlit"), opaqueSortingSettings);
        var opaqueFilterSettings = new FilteringSettings(RenderQueueRange.opaque);
        context.DrawRenderers(cull, ref opaqueDrawSettings, ref opaqueFilterSettings);

        context.DrawSkybox(camera);
        
        var transparentSortingSettings = new SortingSettings(camera)
        {
            criteria = SortingCriteria.CommonTransparent
        };
        var transparentDrawSettings = new DrawingSettings(new ShaderTagId("SRPDefaultUnlit"), transparentSortingSettings);
        var transparentFilterSettings = new FilteringSettings(RenderQueueRange.transparent);
        context.DrawRenderers(cull, ref transparentDrawSettings, ref transparentFilterSettings);
        
        // TODO: Optimize cull argument pass
        //DrawDefaultPipeline(context, camera, cull);
        
        //_buffer.EndSample(CommandName);
        //context.ExecuteCommandBuffer(_buffer);
        _buffer.Clear();

        context.Submit();
    }

    void DrawDefaultPipeline(ScriptableRenderContext context, Camera camera, CullingResults cull)
    {
        var sortingSettings = new SortingSettings(camera);
        var drawSettings = new DrawingSettings(new ShaderTagId("ForwardBase"), sortingSettings);
        var filterSettings = new FilteringSettings(RenderQueueRange.all);
        context.DrawRenderers(cull, ref drawSettings, ref filterSettings);
    }
}

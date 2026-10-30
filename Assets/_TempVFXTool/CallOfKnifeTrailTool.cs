using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.VFX;
using UnityEditor.VFX.Block;
using UnityEngine;
using UnityEngine.VFX;

// 임시 도구: CallOfKnife.vfx 에 떨어지는 검 트레일 시스템을 추가한다. 작업 후 삭제.
public static class CallOfKnifeTrailTool
{
    const string VfxPath = "Assets/CallOfKnife.vfx";
    const string ShaderPath = "Assets/Effects/CallOfKnife/Shaders/TrailShader.shadergraph";

    static VFXGraph Graph(out VisualEffectResource res)
    {
        res = VisualEffectResource.GetResourceAtPath(VfxPath);
        return res.GetOrCreateGraph();
    }

    static void DumpSlot(StringBuilder sb, VFXSlot s, string ind)
    {
        var links = string.Join(",", s.LinkedSlots.Select(l => l.owner + "." + l.name));
        sb.AppendLine(ind + s.name + "=" + s.value + (links.Length > 0 ? " <- " + links : ""));
        foreach (var c in s.children) DumpSlot(sb, c, ind + "  ");
    }

    public static string Dump()
    {
        var graph = Graph(out _);
        var sb = new StringBuilder();
        foreach (var m in graph.children)
        {
            sb.AppendLine(m.GetType().Name + " '" + m.name + "' pos=" + m.position);
            if (m is VFXContext ctx)
            {
                sb.AppendLine("  data=" + ctx.GetData()?.GetType().Name);
                foreach (var b in ctx.children)
                {
                    sb.AppendLine("  - " + b.name);
                    foreach (var s in b.inputSlots) DumpSlot(sb, s, "      ");
                }
            }
            else if (m is IVFXSlotContainer sc)
                foreach (var s in sc.inputSlots) DumpSlot(sb, s, "    ");
        }
        var mesh = graph.children.OfType<VFXMeshOutput>().First().inputSlots.First(s => s.name == "mesh").value as Mesh;
        if (mesh != null) sb.AppendLine("mesh bounds=" + mesh.bounds);
        sb.AppendLine("sg=" + AssetDatabase.LoadAssetAtPath<ShaderGraphVfxAsset>(ShaderPath));
        return sb.ToString();
    }

    static T AddBlock<T>(VFXContext ctx, params (string name, object value)[] settings) where T : VFXBlock
    {
        var b = ScriptableObject.CreateInstance<T>();
        foreach (var s in settings) b.SetSettingValue(s.name, s.value);
        ctx.AddChild(b);
        return b;
    }

    public static string Errors()
    {
        var graph = Graph(out _);
        var sb = new StringBuilder();
        System.Action<VFXModel> visit = null;
        visit = m =>
        {
            var rep = new VFXErrorReporter(VFXErrorOrigin.Compilation);
            m.GenerateErrors(rep);
            foreach (var e in rep.GetDirtyModelErrors(m)) sb.AppendLine(m.name + ": [" + e.type + "] " + e.error + " " + e.description);
            if (m is VFXContext c) foreach (var b in c.children) visit(b);
        };
        foreach (var m in graph.children) visit(m);
        return sb.ToString();
    }

    public static string Tweak(float lifetime, float width)
    {
        var graph = Graph(out var res);
        var init = graph.children.OfType<VFXBasicInitialize>().First(c => c.GetData().type == VFXDataType.ParticleStrip);
        init.children.OfType<SetAttribute>().First(b => b.attribute == "lifetime").inputSlots[0].value = lifetime;
        var output = graph.children.OfType<VFXComposedParticleStripOutput>().First();
        output.children.OfType<AttributeFromCurve>().First().inputSlots[0].value = AnimationCurve.Linear(0, width, 1, 0);
        graph.SetExpressionGraphDirty();
        graph.RecompileIfNeeded();
        res.WriteAssetWithSubAssets();
        var sb = new StringBuilder();
        var data = (VFXDataParticle)init.GetData();
        sb.AppendLine("bounds mode=" + data.boundsMode + " cap=" + data.GetSetting("capacity").value + " strip=" + data.GetSetting("stripCapacity").value + " per=" + data.GetSetting("particlePerStripCount").value);
        foreach (var s in output.GetSettings(true, (VFXSettingAttribute.VisibleFlags)~0)) sb.AppendLine("  " + s.name + "=" + s.value);
        return sb.ToString();
    }

    public static string DebugRandomPos(bool on)
    {
        var graph = Graph(out var res);
        var init = graph.children.OfType<VFXBasicInitialize>().First(c => c.GetData().type == VFXDataType.ParticleStrip);
        var existing = init.children.OfType<SetAttribute>().FirstOrDefault(b => b.attribute == "position" && b.Random != RandomMode.Off);
        if (on && existing == null)
        {
            var b = AddBlock<SetAttribute>(init, ("attribute", "position"), ("Random", RandomMode.PerComponent), ("Composition", AttributeCompositionMode.Add));
            b.inputSlots[0].value = new Position { position = new Vector3(-0.3f, -0.3f, -0.3f) };
            b.inputSlots[1].value = new Position { position = new Vector3(0.3f, 0.3f, 0.3f) };
        }
        else if (!on && existing != null) init.RemoveChild(existing);
        graph.SetExpressionGraphDirty();
        graph.RecompileIfNeeded();
        res.WriteAssetWithSubAssets();
        return string.Join(",", init.children.Select(b => b.name));
    }

    public static string SetStripShader(bool useTrail)
    {
        var graph = Graph(out var res);
        var output = graph.children.OfType<VFXComposedParticleStripOutput>().First();
        output.SetSettingValue("shaderGraph", useTrail ? AssetDatabase.LoadAssetAtPath<ShaderGraphVfxAsset>(ShaderPath) : VFXResources.defaultResources.shaderGraphVfx);
        graph.SetExpressionGraphDirty();
        graph.RecompileIfNeeded();
        res.WriteAssetWithSubAssets();
        return output.name;
    }

    // Update 의 Multiply Size over Life 는 매 프레임 누적되므로 Output 의 Overwrite 커브로 옮긴다
    public static string FixSize()
    {
        var graph = Graph(out var res);
        var init = graph.children.OfType<VFXBasicInitialize>().First(c => c.GetData() is VFXDataParticle d && d.type == VFXDataType.ParticleStrip);
        var update = (VFXContext)init.outputContexts.First();
        var output = graph.children.OfType<VFXComposedParticleStripOutput>().First();

        foreach (var b in update.children.OfType<AttributeFromCurve>().ToArray()) update.RemoveChild(b);
        foreach (var b in init.children.OfType<SetAttribute>().Where(b => b.attribute == "size").ToArray()) init.RemoveChild(b);

        var sizeOverLife = AddBlock<AttributeFromCurve>(output, ("attribute", "size"));
        sizeOverLife.inputSlots[0].value = AnimationCurve.Linear(0, 0.15f, 1, 0);

        output.SetSettingValue("shaderGraph", AssetDatabase.LoadAssetAtPath<ShaderGraphVfxAsset>(ShaderPath));
        graph.SetExpressionGraphDirty();
        graph.RecompileIfNeeded();
        res.WriteAssetWithSubAssets();
        return Dump();
    }

    public static string Build()
    {
        var graph = Graph(out var res);
        if (graph.children.OfType<VFXBasicGPUEvent>().Any()) return "already built";

        var swordUpdate = graph.children.OfType<VFXBasicUpdate>().First();
        var basePos = swordUpdate.position + new Vector2(600, -550);

        // 검 Update: 매 프레임 GPU 이벤트 1개 (Set Position 뒤라 현재 위치를 넘겨준다)
        var trigger = AddBlock<TriggerEvent>(swordUpdate, ("mode", TriggerEvent.Mode.Always));

        var gpuEvent = ScriptableObject.CreateInstance<VFXBasicGPUEvent>();
        gpuEvent.position = basePos;
        graph.AddChild(gpuEvent);
        trigger.outputSlots[0].Link(gpuEvent.inputSlots[0]);

        // Initialize Particle Strip
        var init = ScriptableObject.CreateInstance<VFXBasicInitialize>();
        init.SetSettingValue("dataType", VFXDataParticle.DataType.ParticleStrip);
        init.position = basePos + new Vector2(0, 150);
        graph.AddChild(init);
        gpuEvent.LinkTo(init);
        var data = init.GetData();
        data.SetSettingValue("stripCapacity", 1u);
        data.SetSettingValue("particlePerStripCount", 64u);
        init.inputSlots.First(s => s.name == "bounds").value = new AABox { center = new Vector3(0, 1.5f, 0), size = new Vector3(3, 5, 3) };

        AddBlock<SetAttribute>(init, ("attribute", "position"), ("Source", SetAttribute.ValueSource.Source));
        var life = AddBlock<SetAttribute>(init, ("attribute", "lifetime"));
        life.inputSlots[0].value = 0.3f;
        var size = AddBlock<SetAttribute>(init, ("attribute", "size"));
        size.inputSlots[0].value = 0.15f;

        // Update Particle Strip: 오래된 쪽으로 갈수록 가늘어지게
        var update = ScriptableObject.CreateInstance<VFXBasicUpdate>();
        update.position = basePos + new Vector2(0, 650);
        graph.AddChild(update);
        init.LinkTo(update);
        var sizeOverLife = AddBlock<AttributeFromCurve>(update, ("attribute", "size"), ("Composition", AttributeCompositionMode.Multiply));
        sizeOverLife.inputSlots[0].value = AnimationCurve.Linear(0, 1, 1, 0);

        // Output ParticleStrip (Shader Graph: TrailShader)
        var output = (VFXContext)new VFXStripTopologyProvider().GetVariants().First().CreateInstance();
        output.SetSettingValue("shaderGraph", AssetDatabase.LoadAssetAtPath<ShaderGraphVfxAsset>(ShaderPath));
        output.position = basePos + new Vector2(0, 900);
        graph.AddChild(output);
        update.LinkTo(output);

        graph.SetExpressionGraphDirty();
        graph.RecompileIfNeeded();
        res.WriteAssetWithSubAssets();
        AssetDatabase.SaveAssets();
        return Dump();
    }
}

using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.VFX;
using UnityEngine;
using UnityEngine.VFX;

// 임시 도구: DaggerThrust.vfx 를 만든다. 작업 후 삭제.
public static class DaggerThrustTool
{
    static void DumpSlot(StringBuilder sb, VFXSlot s, string ind)
    {
        var links = string.Join(",", s.LinkedSlots.Select(l => l.owner + "." + l.name));
        sb.AppendLine(ind + s.name + "=" + s.value + (links.Length > 0 ? " <- " + links : ""));
        foreach (var c in s.children) DumpSlot(sb, c, ind + "  ");
    }

    public const string VfxPath = "Assets/Effects/DaggerThrust/VFX/DaggerThrust.vfx";
    const string MeshPath = "Assets/Effects/DaggerThrust/Meshes/ThrustSweep.asset";
    const string ShaderPath = "Assets/Effects/DaggerThrust/Shaders/ThrustSweep.shadergraph";

    static T AddBlock<T>(VFXContext ctx, params (string name, object value)[] settings) where T : VFXBlock
    {
        var b = ScriptableObject.CreateInstance<T>();
        foreach (var s in settings) b.SetSettingValue(s.name, s.value);
        ctx.AddChild(b);
        return b;
    }

    static T AddOperator<T>(VFXGraph graph, Vector2 pos) where T : VFXOperator
    {
        var op = ScriptableObject.CreateInstance<T>();
        op.position = pos;
        graph.AddChild(op);
        return op;
    }

    // 수명 비율(0..1)을 커브로 샘플해 출력의 셰이더 프로퍼티에 연결
    static void DriveByCurve(VFXGraph graph, VFXContext output, VFXOperator age, string prop, AnimationCurve curve, Vector2 pos)
    {
        var sample = AddOperator<UnityEditor.VFX.Operator.SampleCurve>(graph, pos);
        sample.inputSlots[0].value = curve;
        age.outputSlots[0].Link(sample.inputSlots[1]);
        sample.outputSlots[0].Link(output.inputSlots.First(s => s.name == prop));
    }

    public static string Build()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Effects/DaggerThrust/VFX"))
            AssetDatabase.CreateFolder("Assets/Effects/DaggerThrust", "VFX");
        if (AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(VfxPath) == null)
            VisualEffectAssetEditorUtility.CreateNewAsset(VfxPath);

        var res = VisualEffectResource.GetResourceAtPath(VfxPath);
        var graph = res.GetOrCreateGraph();
        foreach (var c in graph.children.ToArray()) graph.RemoveChild(c);

        // Spawn: 한 번에 1개
        var spawn = ScriptableObject.CreateInstance<VFXBasicSpawner>();
        spawn.position = new Vector2(400, 0);
        graph.AddChild(spawn);
        var burst = AddBlock<VFXSpawnerBurst>(spawn);
        burst.inputSlots.First(s => s.name == "Count").value = 1f;
        burst.inputSlots.First(s => s.name == "Delay").value = 0f;

        // Initialize: 수명 0.35초, 크기 1 (메시를 원래 크기로)
        var init = ScriptableObject.CreateInstance<VFXBasicInitialize>();
        init.position = new Vector2(400, 350);
        graph.AddChild(init);
        spawn.LinkTo(init);
        init.GetData().SetSettingValue("capacity", 1u);
        init.inputSlots.First(s => s.name == "bounds").value = new AABox { center = new Vector3(0.45f, 0, -0.35f), size = new Vector3(2, 1, 6) };
        AddBlock<UnityEditor.VFX.Block.SetAttribute>(init, ("attribute", "lifetime")).inputSlots[0].value = 0.35f;
        AddBlock<UnityEditor.VFX.Block.SetAttribute>(init, ("attribute", "size")).inputSlots[0].value = 1f;

        var update = ScriptableObject.CreateInstance<VFXBasicUpdate>();
        update.position = new Vector2(400, 700);
        graph.AddChild(update);
        init.LinkTo(update);

        // Output Particle Shader Graph Mesh: ThrustSweep 메시 + ThrustSweep 셰이더
        var variant = new VFXTopologyProvider().GetVariants().First(v => v.name.EndsWith("|Mesh"));
        var output = (VFXContext)variant.CreateInstance();
        output.SetSettingValue("shaderGraph", AssetDatabase.LoadAssetAtPath<UnityEditor.ShaderGraph.Internal.ShaderGraphVfxAsset>(ShaderPath));
        output.position = new Vector2(400, 1000);
        graph.AddChild(output);
        update.LinkTo(output);
        output.inputSlots.First(s => s.name == "mesh").value = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);

        // Progress: 앞 1/3(약 0.12초) 동안 칼날이 휩쓸며 나타남
        // Dissolve: 그 뒤 꼬리(예비동작 쪽)부터 사라짐
        var age = AddOperator<UnityEditor.VFX.Operator.AgeOverLifetime>(graph, new Vector2(-150, 1100));
        DriveByCurve(graph, output, age, "_Progress",
            new AnimationCurve(new Keyframe(0, 0, 0, 5f), new Keyframe(0.35f, 1.1f, 0, 0), new Keyframe(1, 1.1f, 0, 0)),
            new Vector2(100, 1050));
        DriveByCurve(graph, output, age, "_Dissolve",
            new AnimationCurve(new Keyframe(0, -0.25f, 0, 0), new Keyframe(0.35f, -0.25f, 0, 0), new Keyframe(1, 1f, 2.5f, 0)),
            new Vector2(100, 1200));

        graph.SetExpressionGraphDirty();
        graph.RecompileIfNeeded();
        res.WriteAssetWithSubAssets();
        AssetDatabase.SaveAssets();
        return Dump(VfxPath);
    }

    public static string Dump(string path)
    {
        var graph = VisualEffectResource.GetResourceAtPath(path).GetOrCreateGraph();
        var sb = new StringBuilder();
        foreach (var m in graph.children)
        {
            sb.AppendLine(m.GetType().FullName + " '" + m.name + "' pos=" + m.position);
            if (m is VFXModel vm)
                foreach (var s in vm.GetSettings(true, (VFXSettingAttribute.VisibleFlags)~0))
                    sb.AppendLine("    [setting] " + s.name + "=" + s.value);
            if (m is VFXContext ctx)
            {
                sb.AppendLine("  data=" + ctx.GetData()?.GetType().Name);
                foreach (var s in ctx.inputSlots) DumpSlot(sb, s, "    ");
                foreach (var b in ctx.children)
                {
                    sb.AppendLine("  - " + b.GetType().FullName + " " + b.name);
                    foreach (var s in b.GetSettings(true, (VFXSettingAttribute.VisibleFlags)~0)) sb.AppendLine("      [setting] " + s.name + "=" + s.value);
                    foreach (var s in b.inputSlots) DumpSlot(sb, s, "      ");
                }
            }
            else if (m is IVFXSlotContainer sc)
                foreach (var s in sc.inputSlots) DumpSlot(sb, s, "    ");
        }
        return sb.ToString();
    }
}

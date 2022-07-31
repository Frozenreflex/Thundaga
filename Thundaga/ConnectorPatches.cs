using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BaseX;
using FrooxEngine;
using HarmonyLib;
using UnityEngine;
using UnityNeos;
using Component = FrooxEngine.Component;
using MeshRenderer = FrooxEngine.MeshRenderer;
using SkinnedMeshRenderer = FrooxEngine.SkinnedMeshRenderer;

namespace Thundaga
{
    [HarmonyPatch(typeof(ComponentBase<Component>))]
    public static class ComponentBasePatch
    {
        [HarmonyPatch("InternalRunStartup", MethodType.Normal)]
        [HarmonyReversePatch]
        public static void InternalRunStartup(ComponentBase<Component> instance) => 
            throw new NotImplementedException();

        [HarmonyPatch("InternalRunDestruction", MethodType.Normal)]
        [HarmonyReversePatch]
        public static void InternalRunDestruction(ComponentBase<Component> instance) =>
            throw new NotImplementedException();
    }
    [HarmonyPatch(typeof(MeshRendererConnectorBase<MeshRenderer, UnityEngine.MeshRenderer>))]
    public class MeshRendererConnectorPatch
    {
        [HarmonyPatch("set_meshWasChanged")]
        [HarmonyReversePatch]
        public static void set_meshWasChanged(
            MeshRendererConnectorBase<MeshRenderer, UnityEngine.MeshRenderer> instance, bool value) =>
            throw new NotImplementedException();
        [HarmonyPatch("ApplyChanges")]
        [HarmonyTranspiler]
        [HarmonyDebug]
        public static IEnumerable<CodeInstruction> ApplyChangesTranspiler(
            IEnumerable<CodeInstruction> instructions)
        {
            //remove GetWasChangedAndClear methods to prevent thread errors
            var codes = new List<CodeInstruction>(instructions);
            codes.Reverse();
            for (var a = 0; a < 3; a++)
            {
                for (var i = 0; i < codes.Count; i++)
                    if (codes[i].opcode == OpCodes.Brfalse_S)
                    {
                        for (var h = 0; h < 6; h++)
                        {
                            var code = codes[i + h];
                            code.opcode = OpCodes.Nop;
                            code.operand = null;
                        }
                        break;
                    }
            }
            for (var i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode != OpCodes.Beq_S) continue;
                for (var h = 0; h < 6; h++)
                {
                    var code = codes[i + h];
                    code.opcode = OpCodes.Nop;
                    code.operand = null;
                }
                break;
            }
            for (var i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode != OpCodes.Call ||
                    !codes[i].operand.ToString().Contains("set_meshWasChanged")) continue;
                for (var h = 0; h < 7; h++)
                {
                    var code = codes[i + h];
                    code.opcode = OpCodes.Nop;
                    code.operand = null;
                }
                break;
            }
            codes.Reverse();
            //replace generic set with our method
            var index = codes.IndexOf(codes.Where(i =>
                i.opcode == OpCodes.Callvirt && i.operand.ToString().Contains("AddComponent")).ToList()[1]);
            codes[index].operand = typeof(MeshGenericFix).GetMethod("SetMeshRendererPatch");
            codes[index].opcode = OpCodes.Call;
            codes.Insert(index, new CodeInstruction(OpCodes.Ldarg_0));
            return codes;
        }
    }

    public static class MeshGenericFix
    {
        public static Renderer SetMeshRendererPatch(GameObject gameObject, IConnector obj)
        {
            if (obj is MeshRendererConnector)
                return gameObject.AddComponent<UnityEngine.MeshRenderer>();
            return gameObject.AddComponent<UnityEngine.SkinnedMeshRenderer>();
        }
    }
    [HarmonyPatch(typeof(SkinnedMeshRendererConnector))]
    public static class SkinnedMeshRendererConnectorPatchA
    {
        [HarmonyPatch("ApplyChanges")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> ApplyChangesTranspiler(
            IEnumerable<CodeInstruction> instructions)
        {
            //remove WasChanged set to prevent thread errors
            var codes = new List<CodeInstruction>(instructions);
            codes.Reverse();
            for (var a = 0; a < 2; a++)
            {
                for (var i = 0; i < codes.Count; i++)
                    if (codes[i].opcode == OpCodes.Callvirt && codes[i].operand.ToString().Contains("set_WasChanged"))
                    {
                        for (var h = 0; h < 5; h++)
                        {
                            codes[i+h].opcode = OpCodes.Nop;
                            codes[i+h].operand = null;
                        }
                        break;
                    }
            }
            codes.Reverse();
            //get actual blendshape count to prevent errors
            for (var i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode != OpCodes.Ldloc_S ||
                    !codes[i].operand.ToString().Contains("14")) continue;
                codes[i].opcode = OpCodes.Ldarg_0;
                codes[i].operand = null;
                codes[i + 1].operand = typeof(SkinnedMeshRendererConnectorPatchA).GetMethod("GetBlendShapeCount");
                /*
                var insertCodes = new CodeInstruction[]
                {
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Callvirt,
                        typeof(SkinnedMeshRendererConnectorPatchA).GetMethod("GetBlendShapeCount")),
                    new CodeInstruction(OpCodes.Stloc_S, (byte)20)
                };
                codes.InsertRange(i, insertCodes);
                */
                break;
            }
            return codes;
        }
        public static int GetBlendShapeCount(SkinnedMeshRendererConnector instance) =>
            instance.MeshRenderer.sharedMesh.blendShapeCount;
    }
    [HarmonyPatch(typeof(MeshRendererConnectorBase<SkinnedMeshRenderer, UnityEngine.SkinnedMeshRenderer>))]
    public static class SkinnedMeshRendererConnectorPatchB
    {
        [HarmonyPatch("set_meshWasChanged")]
        [HarmonyReversePatch]
        public static void set_meshWasChanged(
            MeshRendererConnectorBase<SkinnedMeshRenderer, UnityEngine.SkinnedMeshRenderer> instance, bool value) =>
            throw new NotImplementedException();
        [HarmonyPatch("ApplyChanges")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> ApplyChangesTranspiler(
            IEnumerable<CodeInstruction> instructions)
        {
            //remove GetWasChangedAndClear methods to prevent thread errors
            var codes = new List<CodeInstruction>(instructions);
            codes.Reverse();
            for (var a = 0; a < 3; a++)
            {
                for (var i = 0; i < codes.Count; i++)
                {
                    if (codes[i].opcode != OpCodes.Brfalse_S) continue;
                    for (var h = 0; h < 6; h++)
                    {
                        var code = codes[i + h];
                        code.opcode = OpCodes.Nop;
                        code.operand = null;
                    }
                    break;
                }
            }
            for (var i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode != OpCodes.Beq_S) continue;
                for (var h = 0; h < 6; h++)
                {
                    var code = codes[i + h];
                    code.opcode = OpCodes.Nop;
                    code.operand = null;
                }
                break;
            }
            for (var i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode != OpCodes.Call ||
                    !codes[i].operand.ToString().Contains("set_meshWasChanged")) continue;
                for (var h = 0; h < 7; h++)
                {
                    var code = codes[i + h];
                    code.opcode = OpCodes.Nop;
                    code.operand = null;
                }
                break;
            }
            codes.Reverse();
            //replace generic set with our method
            var index = codes.IndexOf(codes.Where(i =>
                i.opcode == OpCodes.Callvirt && i.operand.ToString().Contains("AddComponent")).ToList()[1]);
            codes[index].operand = typeof(MeshGenericFix).GetMethod("SetMeshRendererPatch");
            codes[index].opcode = OpCodes.Call;
            codes.Insert(index, new CodeInstruction(OpCodes.Ldarg_0));
            return codes;
        }
        public static UnityEngine.SkinnedMeshRenderer SetMeshRendererPatch(GameObject gameObject)
        {
            UniLog.Log("hello world skinned");
            return gameObject.AddComponent<UnityEngine.SkinnedMeshRenderer>();
        }
    }
    [HarmonyPatch(typeof(MeshConnector))]
    public static class MeshConnectorPatch
    {
        [HarmonyPatch("Upload")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> UploadTranspiler(
            IEnumerable<CodeInstruction> instructions) =>
            instructions.RemoveDestroyImmediate();
        
        [HarmonyPatch("Destroy")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> DestroyTranspiler(
            IEnumerable<CodeInstruction> instructions) =>
            instructions.RemoveDestroyImmediate();
    }
    [HarmonyPatch(typeof(TextureConnector))]
    public static class TextureConnectorPatch
    {
        [HarmonyPatch("Destroy")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> DestroyTranspiler(
            IEnumerable<CodeInstruction> instructions) =>
            instructions.RemoveDestroyImmediate();
    }
    [HarmonyPatch(typeof(Texture3DConnector))]
    public static class Texture3DConnectorPatch
    {
        [HarmonyPatch("Destroy")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> DestroyTranspiler(
            IEnumerable<CodeInstruction> instructions) =>
            instructions.RemoveDestroyImmediate();
    }
    [HarmonyPatch(typeof(MaterialConnector))]
    public static class MaterialConnectorPatch
    {
        [HarmonyPatch("CleanupMaterial")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> CleanupMaterialTranspiler(
            IEnumerable<CodeInstruction> instructions) =>
            instructions.RemoveDestroyImmediate();
    }
    [HarmonyPatch(typeof(CubemapConnector))]
    public static class CubemapConnectorPatch
    {
        [HarmonyPatch("Destroy")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> DestroyTranspiler(
            IEnumerable<CodeInstruction> instructions) =>
            instructions.RemoveDestroyImmediate();
    }
    public static class DestroyImmediateRemover
    {
        public static IEnumerable<CodeInstruction> OnReadyTranspiler(
            IEnumerable<CodeInstruction> instructions) =>
            instructions.RemoveDestroyImmediate(false).RemoveDestroyImmediate().RemoveDestroyImmediate();

        public static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions) =>
            instructions.RemoveDestroyImmediate();
        public static IEnumerable<CodeInstruction> TranspilerTwice(
            IEnumerable<CodeInstruction> instructions) =>
            instructions.RemoveDestroyImmediate(false).RemoveDestroyImmediate(false);
        public static List<CodeInstruction> RemoveDestroyImmediate(this IEnumerable<CodeInstruction> instructions, bool hasOperand = true)
        {
            var codes = new List<CodeInstruction>(instructions);
            for (var i = 0; i < codes.Count; i++)
            {
                var code = codes[i];
                if (code.opcode != OpCodes.Call || !code.operand.ToString().Contains("DestroyImmediate")) continue;
                var newMethod = typeof(UnityEngine.Object).GetMethod("Destroy", new[] {typeof(UnityEngine.Object)});
                code.operand = newMethod;
                if (hasOperand) codes[i-1].opcode = OpCodes.Nop;
                break;
            }
            return codes;
        }
    }
}
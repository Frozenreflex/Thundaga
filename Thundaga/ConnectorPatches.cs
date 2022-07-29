using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using BaseX;
using FrooxEngine;
using HarmonyLib;
using UnityNeos;

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
                if (codes[i].opcode == OpCodes.Beq_S)
                {
                    for (var h = 0; h < 6; h++)
                    {
                        var code = codes[i + h];
                        code.opcode = OpCodes.Nop;
                        code.operand = null;
                    }
                    break;
                }
            for (var i = 0; i < codes.Count; i++) 
                if (codes[i].opcode == OpCodes.Call && codes[i].operand.ToString().Contains("set_meshWasChanged"))
                {
                    for (var h = 0; h < 7; h++)
                    {
                        var code = codes[i + h];
                        code.opcode = OpCodes.Nop;
                        code.operand = null;
                    }
                    break;
                }
            codes.Reverse();
            return codes;
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
            return codes;
        }
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
                if (codes[i].opcode == OpCodes.Beq_S)
                {
                    for (var h = 0; h < 6; h++)
                    {
                        var code = codes[i + h];
                        code.opcode = OpCodes.Nop;
                        code.operand = null;
                    }
                    break;
                }
            for (var i = 0; i < codes.Count; i++) 
                if (codes[i].opcode == OpCodes.Call && codes[i].operand.ToString().Contains("set_meshWasChanged"))
                {
                    for (var h = 0; h < 7; h++)
                    {
                        var code = codes[i + h];
                        code.opcode = OpCodes.Nop;
                        code.operand = null;
                    }
                    break;
                }
            codes.Reverse();
            return codes;
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
using System;
using HarmonyLib;
using UnityNeos;

namespace Thundaga
{
    public class SkinnedMeshRendererConnectorPacket : ConnectorPacket<SkinnedMeshRendererConnector>
    {
        public SkinnedMeshRendererConnectorPacket(SkinnedMeshRendererConnector connector) => _connector = connector;
        public override void ApplyChange() => SkinnedMeshRendererConnectorPatches.ApplyChangesOriginal(_connector);
    }
    public class SkinnedMeshRendererConnectorDestroyPacket : ConnectorPacket<SkinnedMeshRendererConnector>
    {
        private bool _destroyingWorld;
        public SkinnedMeshRendererConnectorDestroyPacket(SkinnedMeshRendererConnector connector, bool destroyingWorld)
        {
            _connector = connector;
            _destroyingWorld = destroyingWorld;
        }
        public override void ApplyChange() =>
            SkinnedMeshRendererConnectorPatches.DestroyOriginal(_connector, _destroyingWorld);
    }
    public class SkinnedMeshRendererConnectorInitializePacket : ConnectorPacket<SkinnedMeshRendererConnector>
    {
        public SkinnedMeshRendererConnectorInitializePacket(SkinnedMeshRendererConnector connector) =>
            _connector = connector;
        public override void ApplyChange() => SkinnedMeshRendererConnectorPatches.InitializeOriginal(_connector);
    }
    [HarmonyPatch(typeof(SkinnedMeshRendererConnector))]
    public static class SkinnedMeshRendererConnectorPatches
    {
        [HarmonyPatch("ApplyChanges")]
        [HarmonyPrefix]
        private static bool ApplyChanges(SkinnedMeshRendererConnector __instance)
        {
            PacketManager.Enqueue(__instance.GetPacket());
            return false;
        }
        [HarmonyPatch("ApplyChanges")]
        [HarmonyReversePatch]
        public static void ApplyChangesOriginal(SkinnedMeshRendererConnector instance) =>
            throw new NotImplementedException();
        [HarmonyPatch("Destroy")]
        [HarmonyPrefix]
        private static bool Destroy(SkinnedMeshRendererConnector __instance, bool destroyingWorld)
        {
            PacketManager.Enqueue(__instance.GetDestroyPacket(destroyingWorld));
            return false;
        }
        [HarmonyPatch("Destroy")]
        [HarmonyReversePatch]
        public static void DestroyOriginal(SkinnedMeshRendererConnector instance, bool destroyingWorld) =>
            throw new NotImplementedException();

        [HarmonyPatch("Initialize")]
        [HarmonyPrefix]
        private static bool Initialize(SkinnedMeshRendererConnector __instance)
        {
            PacketManager.Enqueue(__instance.GetInitializePacket());
            return false;
        }
        [HarmonyPatch("Initialize")]
        [HarmonyReversePatch]
        public static void InitializeOriginal(SkinnedMeshRendererConnector instance) =>
            throw new NotImplementedException();
    }
}
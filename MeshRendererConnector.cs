using System;
using HarmonyLib;
using UnityNeos;

namespace Thundaga
{
    public class MeshRendererConnectorPacket : ConnectorPacket<MeshRendererConnector>
    {
        public MeshRendererConnectorPacket(MeshRendererConnector connector) => _connector = connector;
        public override void ApplyChange() => MeshRendererConnectorPatches.ApplyChangesOriginal(_connector);
    }
    public class MeshRendererConnectorDestroyPacket : ConnectorPacket<MeshRendererConnector>
    {
        private bool _destroyingWorld;
        public MeshRendererConnectorDestroyPacket(MeshRendererConnector connector, bool destroyingWorld)
        {
            _connector = connector;
            _destroyingWorld = destroyingWorld;
        }
        public override void ApplyChange() =>
            MeshRendererConnectorPatches.DestroyOriginal(_connector, _destroyingWorld);
    }
    public class MeshRendererConnectorInitializePacket : ConnectorPacket<MeshRendererConnector>
    {
        public MeshRendererConnectorInitializePacket(MeshRendererConnector connector) =>
            _connector = connector;
        public override void ApplyChange() => MeshRendererConnectorPatches.InitializeOriginal(_connector);
    }
    [HarmonyPatch(typeof(MeshRendererConnector))]
    public static class MeshRendererConnectorPatches
    {
        [HarmonyPatch("ApplyChanges")]
        [HarmonyPrefix]
        private static bool ApplyChanges(MeshRendererConnector __instance)
        {
            PacketManager.Enqueue(__instance.GetPacket());
            return false;
        }
        [HarmonyPatch("ApplyChanges")]
        [HarmonyReversePatch]
        public static void ApplyChangesOriginal(MeshRendererConnector instance) =>
            throw new NotImplementedException();
        [HarmonyPatch("Destroy")]
        [HarmonyPrefix]
        private static bool Destroy(MeshRendererConnector __instance, bool destroyingWorld)
        {
            PacketManager.Enqueue(__instance.GetDestroyPacket(destroyingWorld));
            return false;
        }
        [HarmonyPatch("Destroy")]
        [HarmonyReversePatch]
        public static void DestroyOriginal(MeshRendererConnector instance, bool destroyingWorld) =>
            throw new NotImplementedException();

        [HarmonyPatch("Initialize")]
        [HarmonyPrefix]
        private static bool Initialize(MeshRendererConnector __instance)
        {
            PacketManager.Enqueue(__instance.GetInitializePacket());
            return false;
        }
        [HarmonyPatch("Initialize")]
        [HarmonyReversePatch]
        public static void InitializeOriginal(MeshRendererConnector instance) =>
            throw new NotImplementedException();
    }
}
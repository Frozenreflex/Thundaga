using System;
using System.Reflection;
using HarmonyLib;
using UnityNeos;

namespace Thundaga
{
    public class AudioOutputConnectorPacket : ConnectorPacket<AudioOutputConnector>
    {
        public AudioOutputConnectorPacket(AudioOutputConnector connector) => _connector = connector;
        public override void ApplyChange() => AudioOutputConnectorPatches.ApplyChangesOriginal(_connector);
    }
    public class AudioOutputConnectorDestroyPacket : ConnectorPacket<AudioOutputConnector>
    {
        private bool _destroyingWorld;
        public AudioOutputConnectorDestroyPacket(AudioOutputConnector connector, bool destroyingWorld)
        {
            _connector = connector;
            _destroyingWorld = destroyingWorld;
        }
        public override void ApplyChange() => AudioOutputConnectorPatches.DestroyOriginal(_connector, _destroyingWorld);
    }
    public class AudioOutputConnectorInitializePacket : ConnectorPacket<AudioOutputConnector>
    {
        public AudioOutputConnectorInitializePacket(AudioOutputConnector connector) => _connector = connector;
        public override void ApplyChange() => AudioOutputConnectorPatches.InitializeOriginal(_connector);
    }
    [HarmonyPatch(typeof(AudioOutputConnector))]
    public static class AudioOutputConnectorPatches
    {
        [HarmonyPatch("ApplyChanges")]
        [HarmonyPrefix]
        private static bool ApplyChanges(AudioOutputConnector __instance)
        {
            PacketManager.Enqueue(__instance.GetPacket());
            return false;
        }
        [HarmonyPatch("ApplyChanges")]
        [HarmonyReversePatch]
        public static void ApplyChangesOriginal(AudioOutputConnector instance) => throw new NotImplementedException();
        [HarmonyPatch("Destroy")]
        [HarmonyPrefix]
        private static bool Destroy(AudioOutputConnector __instance, bool destroyingWorld)
        {
            PacketManager.Enqueue(__instance.GetDestroyPacket(destroyingWorld));
            return false;
        }
        [HarmonyPatch("Destroy")]
        [HarmonyReversePatch]
        public static void DestroyOriginal(AudioOutputConnector instance, bool destroyingWorld) =>
            throw new NotImplementedException();
        [HarmonyPatch("Initialize")]
        [HarmonyPrefix]
        private static bool Initialize(AudioOutputConnector __instance)
        {
            PacketManager.Enqueue(__instance.GetInitializePacket());
            return false;
        }
        [HarmonyPatch("Initialize")]
        [HarmonyReversePatch]
        public static void InitializeOriginal(AudioOutputConnector instance) => throw new NotImplementedException();
    }
}
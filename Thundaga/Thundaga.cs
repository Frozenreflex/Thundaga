using System;
using System.Collections.Generic;
using HarmonyLib;
using NeosModLoader;
using System.Linq;
using System.Reflection;
using B83.Win32;
using BaseX;
using FrooxEngine;
using Leap;
using Thundaga.Packets;
using UnityNeos;

namespace Thundaga
{
    public class Thundaga : NeosMod
    {
        public override string Name => "Thundaga";
        public override string Author => "Fro Zen";
        public override string Version => "1.0.0";

        private Assembly _unityNeos;
        private static bool _startedNewUpdateLoop = false;

        private static bool _first_trigger = false;

        public override void OnEngineInit()
        {
            var harmony = new Harmony("Thundaga");
            /*
            var original = typeof(ReversePatcher).GetMethod("Patch", AccessTools.all);
            harmony.Patch(original, new HarmonyMethod(typeof(HarmonyPatcherPatcher).GetMethod("Patch", AccessTools.all)));
            Msg("patched harmony lmao");
            Msg(AccessTools.DeclaredMethod(typeof(SkinnedMeshRendererConnector), "Initialize") != null);
            */
            var patches = typeof(ImplementableComponentPatches);
            var a = typeof(ImplementableComponent<>);
            var update = a.GetMethod("InternalUpdateConnector", AccessTools.all);
            var destroy = a.GetMethod("InternalRunDestruction", AccessTools.all);
            var initialize = a.GetMethod("InternalRunStartup", AccessTools.all);

            harmony.Patch(update, new HarmonyMethod(patches.GetMethod("InternalUpdateConnector")));
            harmony.Patch(destroy, new HarmonyMethod(patches.GetMethod("InternalRunDestruction")));
            harmony.Patch(initialize, new HarmonyMethod(patches.GetMethod("InternalRunStartup")));
            
            harmony.PatchAll();
            //do this if we need patches for platform specific connectors
            /*
            switch (Engine.Current.Platform)
            {
                case Platform.Windows:
                    //run windows specific patches
                    //viseme analyzer
                    //not sure if viseme analyzer works with the default generic packet
                    break;
                case Platform.Linux:
                    //run linux specific patches
                    break;
                case Platform.Android:
                    //run android specific patches
                    break;
            }
            */
        }
    }
    public interface IConnectorPacket
    {
        void ApplyChange();
    }

    public class ConnectorPacket<T> : IConnectorPacket where T : IConnector
    {
        protected T _connector;
        public virtual void ApplyChange()
        {
        }
    }

    public static class PacketExtensions
    {
        public static SlotConnectorPacket GetPacket(this SlotConnector connector) => new SlotConnectorPacket(connector);
        public static SlotConnectorDestroyPacket GetDestroyPacket(this SlotConnector connector, bool destroyingWorld) =>
            new SlotConnectorDestroyPacket(connector, destroyingWorld);
    }

    public static class PacketManager
    {
        public static List<IConnectorPacket> NeosPacketQueue = new List<IConnectorPacket>();
        public static List<IConnectorPacket> IntermittentPacketQueue = new List<IConnectorPacket>();
        public static void Enqueue(IConnectorPacket packet) => NeosPacketQueue.Add(packet);
        public static void FinishNeosQueue()
        {
            lock (IntermittentPacketQueue)
            {
                IntermittentPacketQueue.AddRange(NeosPacketQueue);
                NeosPacketQueue.Clear();
            }
        }
        public static List<IConnectorPacket> GetQueuedPackets()
        {
            lock (IntermittentPacketQueue)
            {
                var packets = new List<IConnectorPacket>(IntermittentPacketQueue);
                IntermittentPacketQueue.Clear();
                return packets;
            }
        }
    }
    
    public static class ImplementableComponentPatches
    {
        public static bool InternalUpdateConnector(ImplementableComponent<IConnector> __instance)
        {
            PacketManager.Enqueue(new GenericComponentPacket(__instance.Connector));
            return false;
        }
        public static bool InternalRunStartup(ImplementableComponent<IConnector> __instance)
        {
            ComponentBasePatch.InternalRunStartup(__instance);
            PacketManager.Enqueue(new GenericComponentInitializePacket(__instance.Connector));
            return false;
        }
        public static bool InternalRunDestruction(ImplementableComponent<IConnector> __instance)
        {
            ComponentBasePatch.InternalRunDestruction(__instance);
            PacketManager.Enqueue(new GenericComponentDestroyPacket(__instance.Connector, __instance.World.IsDestroyed));
            return false;
        }
    }

    [HarmonyPatch(typeof(ComponentBase<>))]
    public static class ComponentBasePatch
    {
        [HarmonyPatch("InternalRunStartup")]
        [HarmonyReversePatch]
        public static void InternalRunStartup(object instance)
        {
            throw new NotImplementedException();
        }
        [HarmonyPatch("InternalRunDestruction")]
        [HarmonyReversePatch]
        public static void InternalRunDestruction(object instance)
        {
            throw new NotImplementedException();
        }
    }
    /*
    public static class HarmonyPatcherPatcher
    {
        private static FieldInfo standin = typeof(ReversePatcher).GetField("standin", AccessTools.all);
        public static bool Patch(ReversePatcher __instance)
        {
            UniLog.Log("-----");
            UniLog.Log(((HarmonyMethod)standin.GetValue(__instance)).methodName);
            UniLog.Log(((HarmonyMethod)standin.GetValue(__instance)).method.DeclaringType.FullName);
            return true;
        }
    }
    */
}
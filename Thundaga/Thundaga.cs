using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BaseX;
using HarmonyLib;
using NeosModLoader;
using FrooxEngine;
using Thundaga.Packets;
using UnityNeos;

namespace Thundaga
{
    public class Thundaga : NeosMod
    {
        public override string Name => "Thundaga";
        public override string Author => "Fro Zen";
        public override string Version => "1.0.0";

        private static bool _first_trigger = false;

        public override void OnEngineInit()
        {
            var harmony = new Harmony("Thundaga");

            var patches = typeof(ImplementableComponentPatches);
            var a = typeof(ImplementableComponent<>).MakeGenericType(typeof(IConnector));
            var update = a.GetMethod("InternalUpdateConnector", AccessTools.all);
            var destroy = a.GetMethod("DisposeConnector", AccessTools.all);
            var initialize = a.GetMethod("InternalRunStartup", AccessTools.all);

            harmony.Patch(update, new HarmonyMethod(patches.GetMethod("InternalUpdateConnector")));
            harmony.Patch(destroy, new HarmonyMethod(patches.GetMethod("DisposeConnector")));
            harmony.Patch(initialize, new HarmonyMethod(patches.GetMethod("InternalRunStartup")));
            
            var ambiguitySolver =
                typeof(UnityAssetIntegrator).GetMethods(AccessTools.all)
                    .First(i => i.Name.Contains("ProcessQueue") && i.GetParameters().Length == 1);
            harmony.Patch(ambiguitySolver,
                new HarmonyMethod(
                    typeof(ExtraPatches).GetMethod(nameof(ExtraPatches.ProcessQueue))));

            var destroy1 = AccessTools.AllTypes()
                .First(i => i.Name.Contains("<>c__DisplayClass14_0") &&
                            i.DeclaringType == typeof(RenderTextureConnector))
                .GetMethod("<Unload>b__0", AccessTools.all);
            var destroy2 = AccessTools.AllTypes()
                .First(i => i.Name.Contains("<>c__DisplayClass39_1") &&
                            i.DeclaringType == typeof(TextureConnector))
                .GetMethod("<SetTextureFormatDX11Native>b__2", AccessTools.all);
            var destroy3 = AccessTools.AllTypes()
                .First(i => i.Name.Contains("<>c__DisplayClass49_1") &&
                            i.DeclaringType == typeof(TextureConnector))
                .GetMethod("<SetTextureFormatOpenGLNative>b__2", AccessTools.all);
            //the startup logo
            var destroy4 = AccessTools.AllTypes()
                .First(i => i.Name.Contains("<>c__DisplayClass38_0") &&
                            i.DeclaringType == typeof(FrooxEngineRunner))
                .GetMethod("<Start>b__6", AccessTools.all);
            
            var transpiler = new HarmonyMethod(typeof(DestroyImmediateRemover).GetMethod(nameof(DestroyImmediateRemover.Transpiler)));
            var transpilerTwice = new HarmonyMethod(typeof(DestroyImmediateRemover).GetMethod(nameof(DestroyImmediateRemover.TranspilerTwice)));
            var transpilerLogo = new HarmonyMethod(typeof(DestroyImmediateRemover).GetMethod(nameof(DestroyImmediateRemover.OnReadyTranspiler)));
            
            harmony.Patch(destroy1, transpiler: transpiler);
            harmony.Patch(destroy2, transpiler: transpilerTwice);
            harmony.Patch(destroy3, transpiler: transpilerTwice);
            harmony.Patch(destroy4, transpiler: transpilerLogo);
            
            harmony.PatchAll();
            Msg("Patched methods");
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
        public static List<IConnectorPacket> NeosHighPriorityPacketQueue = new List<IConnectorPacket>();
        public static List<IConnectorPacket> IntermittentPacketQueue = new List<IConnectorPacket>();
        public static List<Action> AssetTaskQueue = new List<Action>();

        public static void Enqueue(IConnectorPacket packet) => NeosPacketQueue.Add(packet);
        public static void EnqueueHigh(IConnectorPacket packet) => NeosHighPriorityPacketQueue.Add(packet);
        public static void FinishNeosQueue()
        {
            lock (IntermittentPacketQueue)
            {
                IntermittentPacketQueue.AddRange(NeosHighPriorityPacketQueue);
                IntermittentPacketQueue.AddRange(NeosPacketQueue);
                NeosPacketQueue.Clear();
                NeosHighPriorityPacketQueue.Clear();
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
        public static List<Action> GetQueuedAssetTasks()
        {
            lock (AssetTaskQueue)
            {
                var packets = new List<Action>(AssetTaskQueue);
                AssetTaskQueue.Clear();
                return packets;
            }
        }
    }
    [HarmonyPatch(typeof(ImplementableComponent<IConnector>))]
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
            PacketManager.Enqueue(new GenericComponentInitializePacket(__instance.Connector, 0, __instance));
            return false;
        }
        public static bool DisposeConnector(ImplementableComponent<IConnector> __instance)
        {
            var destroyed = __instance.World == null || __instance.World.IsDisposed;
            PacketManager.Enqueue(new GenericComponentDestroyPacket(__instance.Connector, destroyed));
            __instance.Connector?.RemoveOwner();
            set_Connector(__instance, null);
            return false;
        }
        [HarmonyPatch("set_Connector")]
        [HarmonyReversePatch]
        public static void set_Connector(ImplementableComponent<IConnector> instance, IConnector connector) =>
            throw new NotImplementedException();
        [HarmonyPatch("InitializeConnector")]
        [HarmonyReversePatch]
        public static void InitializeConnector(ImplementableComponent<IConnector> instance) =>
            throw new NotImplementedException();

        [HarmonyPatch("InitializeConnector")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> InitializeConnectorTranspiler(
            IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            for (var i = 0; i < codes.Count; i++)
            {
                var code = codes[i];
                if (code.opcode != OpCodes.Box) continue;
                for (var h = 0; h < 5; h++)
                {
                    codes[i + h].opcode = OpCodes.Nop;
                    codes[i + h].operand = null;
                }

                return codes;
            }
            return codes;
        }
    }
    public static class ExtraPatches
    {
        public static bool ProcessQueue(UnityAssetIntegrator __instance, ref int __result,
            ref SpinQueue<Action> ___taskQueue)
        {
            lock (PacketManager.AssetTaskQueue)
                while (___taskQueue.TryDequeue(out var val))
                    PacketManager.AssetTaskQueue.Add(val);
            __result = 0;
            return false;
        }
    }
}
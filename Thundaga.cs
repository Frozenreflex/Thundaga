using System.Collections.Generic;
using HarmonyLib;
using NeosModLoader;
using System.Linq;
using System.Reflection;
using FrooxEngine;
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
            _unityNeos = Assembly.GetAssembly(typeof(SkinnedMeshRendererConnector));
            Msg(_unityNeos.FullName);
            var needsChecked = _unityNeos.GetTypes().ToList();
            var methodNames = new[]
            {
                "Update", "OnPreCull", "OnWillRenderObject", "OnBecameVisible", "OnBecameInvisible", "OnPreRender",
                "OnRenderObject", "OnPostRender", "OnRenderImage", "ApplyChanges", "Destroy", "Initialize"
            };
            foreach (var n in methodNames)
            {
                Msg($"{n}:");
                var valid = needsChecked.Where(i => i.GetMethod(n) != null);
                foreach (var v in valid)
                {
                    Msg(v.FullName);
                }
            }
            harmony.PatchAll();
            switch (Engine.Current.Platform)
            {
                case Platform.Windows:
                    //run windows specific patches
                    //viseme analyzer
                    break;
                case Platform.Linux:
                    //run linux specific patches
                    break;
                case Platform.Android:
                    //run android specific patches
                    break;
            }
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
        public static MeshRendererConnectorPacket GetPacket(this MeshRendererConnector connector) =>
            new MeshRendererConnectorPacket(connector);
        public static MeshRendererConnectorDestroyPacket GetDestroyPacket(this MeshRendererConnector connector, bool destroyingWorld) =>
            new MeshRendererConnectorDestroyPacket(connector, destroyingWorld);
        public static MeshRendererConnectorInitializePacket GetInitializePacket(this MeshRendererConnector connector) =>
            new MeshRendererConnectorInitializePacket(connector);
        public static SkinnedMeshRendererConnectorPacket GetPacket(this SkinnedMeshRendererConnector connector) =>
            new SkinnedMeshRendererConnectorPacket(connector);
        public static SkinnedMeshRendererConnectorDestroyPacket GetDestroyPacket(this SkinnedMeshRendererConnector connector, bool destroyingWorld) =>
            new SkinnedMeshRendererConnectorDestroyPacket(connector, destroyingWorld);
        public static SkinnedMeshRendererConnectorInitializePacket GetInitializePacket(this SkinnedMeshRendererConnector connector) =>
            new SkinnedMeshRendererConnectorInitializePacket(connector);
        public static AudioOutputConnectorPacket GetPacket(this AudioOutputConnector connector) =>
            new AudioOutputConnectorPacket(connector);
        public static AudioOutputConnectorDestroyPacket GetDestroyPacket(this AudioOutputConnector connector, bool destroyingWorld) =>
            new AudioOutputConnectorDestroyPacket(connector, destroyingWorld);
        public static AudioOutputConnectorInitializePacket GetInitializePacket(this AudioOutputConnector connector) =>
            new AudioOutputConnectorInitializePacket(connector);
    }

    public static class PacketManager
    {
        public static List<IConnectorPacket> NeosPacketQueue = new List<IConnectorPacket>();
        public static List<IConnectorPacket> IntermittentPacketQueue = new List<IConnectorPacket>();
        public static void Enqueue(IConnectorPacket packet) => NeosPacketQueue.Add(packet);

        //call this at the end of the neos update loop
        public static void FinishNeosQueue()
        {
            lock (IntermittentPacketQueue)
            {
                IntermittentPacketQueue.AddRange(NeosPacketQueue);
                NeosPacketQueue.Clear();
            }
        }

        //call this in place of frooxengine's update method within the unity update loop
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
}
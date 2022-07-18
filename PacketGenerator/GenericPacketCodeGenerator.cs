using Microsoft.CodeAnalysis;

namespace PacketGenerator
{
    [Generator]
    public class GenericPacketCodeGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            
        }
        public void Execute(GeneratorExecutionContext context)
        {
            var GenericConnectors = new[] {
                "SkinnedMeshRendererConnector",
                "MeshRendererConnector",
                "AudioOutputConnector",
                "AudioReverbZoneConnector",
                "CameraPortalConnector",
                "HiddenLayerConnector",
                "LightConnector",
                "LODGroupConnector",
                "ParticleSystemConnector",
                "SkyboxConnector",
            };
            
            var extensions = $@"//auto generated packet code
using System;
using System.Reflection;
using HarmonyLib;
using UnityNeos;

namespace Thundaga.Packets
{{
    public static class GeneratedPacketExtensions
    {{
";
            foreach (var connector in GenericConnectors)
            {
                var source = $@"//auto generated packet code
using System;
using System.Reflection;
using HarmonyLib;
using UnityNeos;

namespace Thundaga.Packets
{{
    public class {connector}Packet : ConnectorPacket<{connector}>
    {{
        public {connector}Packet({connector} connector) => _connector = connector;
        public override void ApplyChange() => {connector}Patches.ApplyChangesOriginal(_connector);
    }}
    public class {connector}DestroyPacket : ConnectorPacket<{connector}>
    {{
        private bool _destroyingWorld;
        public {connector}DestroyPacket({connector} connector, bool destroyingWorld)
        {{
            _connector = connector;
            _destroyingWorld = destroyingWorld;
        }}
        public override void ApplyChange() => {connector}Patches.DestroyOriginal(_connector, _destroyingWorld);
    }}
    public class {connector}InitializePacket : ConnectorPacket<{connector}>
    {{
        public {connector}InitializePacket({connector} connector) => _connector = connector;
        public override void ApplyChange() => {connector}Patches.InitializeOriginal(_connector);
    }}
    [HarmonyPatch(typeof({connector}))]
    public static class {connector}Patches
    {{
        [HarmonyPatch(""ApplyChanges"")]
        [HarmonyPrefix]
        private static bool ApplyChanges(AudioOutputConnector __instance)
        {{
            PacketManager.Enqueue(__instance.GetPacket());
            return false;
        }}
        [HarmonyPatch(""ApplyChanges"")]
        [HarmonyReversePatch]
        public static void ApplyChangesOriginal({connector} instance) => throw new NotImplementedException();
        [HarmonyPatch(""Destroy"")]
        [HarmonyPrefix]
        private static bool Destroy({connector} __instance, bool destroyingWorld)
        {{
            PacketManager.Enqueue(__instance.GetDestroyPacket(destroyingWorld));
            return false;
        }}
        [HarmonyPatch(""Destroy"")]
        [HarmonyReversePatch]
        public static void DestroyOriginal({connector} instance, bool destroyingWorld) =>
            throw new NotImplementedException();
        [HarmonyPatch(""Initialize"")]
        [HarmonyPrefix]
        private static bool Initialize({connector} __instance)
        {{
            PacketManager.Enqueue(__instance.GetInitializePacket());
            return false;
        }}
        [HarmonyPatch(""Initialize"")]
        [HarmonyReversePatch]
        public static void InitializeOriginal({connector} instance) => throw new NotImplementedException();
    }}
}}
";
                context.AddSource($"{connector}.g.cs", source);
                extensions += $@"//{connector}
        public static {connector}Packet GetPacket(this {connector} connector) => new {connector}Packet(connector);
        public static {connector}DestroyPacket GetDestroyPacket(this {connector} connector, bool destroyingWorld) => new {connector}DestroyPacket(connector, destroyingWorld);
        public static {connector}InitializePacket GetInitializePacket(this {connector} connector) => new {connector}InitializePacket(connector);";
            }

            extensions += $@"//end
    }}
}}";
            context.AddSource("Extensions.g.cs", extensions);
        }
    }
}
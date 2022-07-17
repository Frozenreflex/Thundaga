using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using BaseX;
using FrooxEngine;
using HarmonyLib;
using UnityEngine;
using UnityNeos;

namespace Thundaga
{
    public class SlotConnectorPacket : ConnectorPacket<SlotConnector>
    {
        private ISlotConnector _parentConnector;
        private bool _shouldUpdateParent;
        private bool? _active;
        private Vector3? _position;
        private Quaternion? _rotation;
        private Vector3? _scale;
        
        public SlotConnectorPacket(SlotConnector connector)
        {
            _connector = connector;
            var owner = connector.Owner;
            var parent = owner.Parent;
            _parentConnector = parent?.Connector;
            _shouldUpdateParent = parent?.Connector != SlotConnectorInfo.ParentConnector.GetValue(connector) && parent != null;
            if (owner.ActiveSelf_Field.GetWasChangedAndClear()) _active = owner.ActiveSelf_Field.Value;
            if (owner.Position_Field.GetWasChangedAndClear()) _position = owner.Position_Field.Value.ToUnity();
            if (owner.Rotation_Field.GetWasChangedAndClear()) _rotation = owner.Rotation_Field.Value.ToUnity();
            if (owner.Scale_Field.GetWasChangedAndClear()) _scale = owner.Scale_Field.Value.ToUnity();
        }
        public override void ApplyChange()
        {
            if (_connector.GeneratedGameObject == null) return;
            if (_shouldUpdateParent) SlotConnectorPatches.UpdateParent(_connector);
            SlotConnectorPatches.UpdateLayer(_connector);
            UpdateData();
        }
        private void UpdateData()
        {
            if (_active.HasValue) _connector.GeneratedGameObject.SetActive(_active.Value);
            var transform = (Transform)SlotConnectorInfo.Transform.GetValue(_connector);
            if (_position.HasValue) transform.localPosition = _position.Value;
            if (_rotation.HasValue) transform.localRotation = _rotation.Value;
            if (_scale.HasValue) transform.localScale = _scale.Value;
        }
    }
    public class SlotConnectorDestroyPacket : IConnectorPacket
    {
        private SlotConnector _connector;
        private bool _destroyingWorld;
        
        public SlotConnectorDestroyPacket(SlotConnector connector, bool destroyingWorld)
        {
            _connector = connector;
            _destroyingWorld = destroyingWorld;
        }
        public void ApplyChange()
        {
            SlotConnectorPatches.DestroyOriginal(_connector, _destroyingWorld);
        }
    }
    public static class SlotConnectorInfo
    {
        public static readonly FieldInfo ParentConnector;
        public static readonly FieldInfo Transform;
        static SlotConnectorInfo()
        {
            ParentConnector = typeof(SlotConnector).GetField("parentConnector", AccessTools.all);
            Transform = typeof(SlotConnector).GetField("_transform", AccessTools.all);
        }
    }
    [HarmonyPatch(typeof(SlotConnector))]
    public class SlotConnectorPatches
    {
        [HarmonyPatch("ApplyChanges")]
        [HarmonyPrefix]
        private static bool ApplyChanges(SlotConnector __instance)
        {
            PacketManager.Enqueue(__instance.GetPacket());
            return false;
        }
        [HarmonyPatch("Destroy")]
        [HarmonyPrefix]
        private static bool Destroy(SlotConnector __instance, bool destroyingWorld)
        {
            PacketManager.Enqueue(__instance.GetDestroyPacket(destroyingWorld));
            return false;
        }
        [HarmonyPatch("UpdateLayer")]
        [HarmonyReversePatch]
        public static void UpdateLayer(SlotConnector instance)
        {
            throw new NotImplementedException();
        }
        [HarmonyPatch("UpdateParent")]
        [HarmonyReversePatch]
        public static void UpdateParent(SlotConnector instance)
        {
            throw new NotImplementedException();
        }
        [HarmonyPatch("Destroy")]
        [HarmonyReversePatch]
        public static void DestroyOriginal(SlotConnector instance, bool destroyingWorld)
        {
            throw new NotImplementedException();
        }
    }
}
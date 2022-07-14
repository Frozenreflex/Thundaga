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
        //as far as i can tell, grabbing data from neos within the unity thread shouldn't cause crashes or errors,
        //only firing functions from a separate thread to unity, as unity isn't thread safe.
        //however, we still want full updates to occur before rendering most things, otherwise an object could be
        //moved multiple times in one frame and get caught in one of those movements, causing unintended behavior.
        //to compromise, when an object is initialized, it'll grab whatever it is at that moment.
        //that way, newly created and actively updating objects will only do unintended behavior for one frame before
        //correcting and staying correct, and that way i don't have to completely rewrite the connector
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
            if (owner.ActiveSelf_Field.GetWasChangedAndClear()) _active = owner.ActiveSelf_Field.Value;
            if (owner.Position_Field.GetWasChangedAndClear()) _position = owner.Position_Field.Value.ToUnity();
            if (owner.Rotation_Field.GetWasChangedAndClear()) _rotation = owner.Rotation_Field.Value.ToUnity();
            if (owner.Scale_Field.GetWasChangedAndClear()) _scale = owner.Scale_Field.Value.ToUnity();
            _shouldUpdateParent = 
                _parentConnector != SlotConnectorInfo.ParentConnector.GetValue(_connector) && parent != null &&
                !(SlotConnectorInfo.LastParent.GetValue(_connector) == parent && !owner.IsRootSlot);
        }
        public override void ApplyChange()
        {
            if (_connector.GeneratedGameObject == null) return;
            if (_shouldUpdateParent) UpdateParent();
            SlotConnectorPatches.UpdateLayer(_connector);
            UpdateData();
        }

        private void UpdateParent()
        {
            //changes: remove the gameobject deletion nonsense, don't do null checking
            //for root since parent initialization was moved
            var transform = (Transform)SlotConnectorInfo.Transform.GetValue(_connector);
            SlotConnectorInfo.LastParent.SetValue(_connector, _parentConnector);
            var parent = ((SlotConnector)_parentConnector).RequestGameObject();
            transform.SetParent(parent.transform, false);
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
            SlotConnectorInfo.ShouldDestroy.SetValue(_connector, true);
            SlotConnectorPatches.TryDestroy(_connector, _destroyingWorld);
        }
    }

    public static class SlotConnectorInfo
    {
        //todo: find a way to convert these to delegates for better performance, packets are created and deleted constantly
        //so we can't store the delegates inside of them
        public static readonly FieldInfo ParentConnector;
        public static readonly FieldInfo Transform;
        public static readonly FieldInfo LastParent;
        public static readonly FieldInfo ShouldDestroy;

        static SlotConnectorInfo()
        {
            ParentConnector = typeof(SlotConnector).GetField("parentConnector", AccessTools.all);
            Transform = typeof(SlotConnector).GetField("_transform", AccessTools.all);
            LastParent = typeof(SlotConnector).GetField("_lastParent", AccessTools.all);
            ShouldDestroy = typeof(SlotConnector).GetField("shouldDestroy", AccessTools.all);
        }
    }
    
    [HarmonyPatch(typeof(SlotConnector))]
    public class SlotConnectorPatches
    {
        [HarmonyPatch("GenerateGameObject")]
        [HarmonyPrefix]
        private bool GenerateGameObject(SlotConnector __instance)
        {
            var go = new GameObject("");
            set_GeneratedGameObject(__instance, go);
            SlotConnectorInfo.Transform.SetValue(__instance, go.transform);
            SlotConnectorInfo.LastParent.SetValue(__instance, __instance.Owner.Parent);
            
            //replacement for updateparent
            GameObject gameObject;
            if (__instance.Owner.Parent != null)
            {
                var connector = (SlotConnector) __instance.Owner.Parent.Connector;
                SlotConnectorInfo.ParentConnector.SetValue(__instance, connector);
                gameObject = connector.RequestGameObject();
            }
            else
                gameObject = ((WorldConnector) __instance.Owner.World.Connector).WorldRoot;
            go.transform.SetParent(gameObject.transform, false);
            
            UpdateLayer(__instance);
            SetData(__instance);
            return false;
        }
        
        [HarmonyPatch("ApplyChanges")]
        [HarmonyPrefix]
        private bool ApplyChanges(SlotConnector __instance)
        {
            PacketManager.Enqueue(__instance.GetPacket());
            return false;
        }
        
        [HarmonyPatch("Destroy")]
        [HarmonyPrefix]
        private bool Destroy(SlotConnector __instance, bool destroyingWorld)
        {
            PacketManager.Enqueue(__instance.GetDestroyPacket(destroyingWorld));
            return false;
        }
        
        [HarmonyPatch("TryDestroy")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> TryDestroyTranspiler(
            IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            for (var i = 0; i < codes.Count; i++)
            {
                var code = codes[i];
                if (code.opcode != OpCodes.Ldfld || !code.operand.ToString().Contains("parentConnector")) continue;
                //this.parentConnector?.FreeGameObject();
                //we dont want to delete the parent
                codes.RemoveRange(i-1, 7);
                break;
            }
            return codes;
        }

        [HarmonyPatch("set_GeneratedGameObject")]
        [HarmonyReversePatch]
        public static void set_GeneratedGameObject(SlotConnector instance, GameObject value)
        {
            throw new NotImplementedException();
        }
        [HarmonyPatch("UpdateLayer")]
        [HarmonyReversePatch]
        public static void UpdateLayer(SlotConnector instance)
        {
            throw new NotImplementedException();
        }
        [HarmonyPatch("SetData")]
        [HarmonyReversePatch]
        public static void SetData(SlotConnector instance)
        {
            throw new NotImplementedException();
        }
        [HarmonyPatch("TryDestroy")]
        [HarmonyReversePatch(HarmonyReversePatchType.Snapshot)]
        public static void TryDestroy(SlotConnector instance, bool destroyingWorld)
        {
            throw new NotImplementedException();
        }
    }
}
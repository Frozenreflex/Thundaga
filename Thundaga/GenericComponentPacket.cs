using System;
using System.Linq;
using BaseX;
using FrooxEngine;
using Thundaga.Packets;
using UnityNeos;

namespace Thundaga
{
    public class GenericComponentPacket : ConnectorPacket<IConnector>
    {
        public override void ApplyChange()
        {
            if (_connector?.Owner != null) _connector.ApplyChanges();
        }

        public GenericComponentPacket(IConnector connector, bool refresh = false)
        {
            _connector = connector;
            if (refresh) return;
            switch (connector)
            {
                //TODO: is this heavy on performance?
                case MeshRendererConnector meshConnector:
                {
                    var owner = meshConnector.Owner;
                    MeshRendererConnectorPatch.set_meshWasChanged(meshConnector,
                        owner.Mesh.GetWasChangedAndClear());
                    owner.SortingOrder.GetWasChangedAndClear();
                    owner.ShadowCastMode.GetWasChangedAndClear();
                    owner.MotionVectorMode.GetWasChangedAndClear();
                    break;
                }
                case SkinnedMeshRendererConnector skinnedMeshRendererConnector:
                    var owner2 = skinnedMeshRendererConnector.Owner;
                    SkinnedMeshRendererConnectorPatchB.set_meshWasChanged(skinnedMeshRendererConnector,
                        owner2.Mesh.GetWasChangedAndClear());
                    owner2.SortingOrder.GetWasChangedAndClear();
                    owner2.ShadowCastMode.GetWasChangedAndClear();
                    owner2.MotionVectorMode.GetWasChangedAndClear();
                    owner2.ProxyBoundsSource.GetWasChangedAndClear();
                    owner2.ExplicitLocalBounds.GetWasChangedAndClear();
                    break;
            }
        }
    }
    public class GenericComponentDestroyPacket : ConnectorPacket<IConnector>
    {
        private bool _destroyingWorld;
        public override void ApplyChange()
        {
            if (_connector == null) return;
            _connector.Destroy(_destroyingWorld);
            _connector.RemoveOwner();
        }
        public GenericComponentDestroyPacket(IConnector connector, bool destroyingWorld)
        {
            _connector = connector;
            _destroyingWorld = destroyingWorld;
        }
    }
    public class GenericComponentInitializePacket : ConnectorPacket<IConnector>
    {
        private readonly int _queuedInitializations;
        private readonly ImplementableComponent<IConnector> _initializing;
        public override void ApplyChange()
        {
            /*
            if (_connector.Owner == null)
            {
                UniLog.Log($"Connector has no owner: {(_connector != null ? _connector.GetType().ToString() : "Null Connector")}");
                if (_queuedInitializations < 20)
                {
                    if (_initializing.Connector != _connector)
                    {
                        UniLog.Log($"Component {_initializing.GetType()} somehow managed to get a new connector");
                    }
                    //retry next update because something has gone horribly wrong
                    UniLog.Log($"Connector packet being re-queued: {(_connector != null ? _connector.GetType().ToString() : "Null Connector")}");
                    _connector.AssignOwner(_initializing);
                    PacketManager.Enqueue(new GenericComponentInitializePacket(_connector, _queuedInitializations + 1, _initializing));
                    return;
                }
                UniLog.Log($"Connector has reached initialization threshold and is being ignored: {(_connector != null ? _connector.GetType().ToString() : "Null Connector")}");
                return;
            }
            if (_initializing.Slot.Connector == null)
            {
                if (_queuedInitializations < 20)
                {
                    UniLog.Log($"Component {_initializing.GetType()} has no slot connector, creating one and waiting...");
                    var connector = new SlotConnector();
                    connector.AssignOwner(_initializing.Slot);
                    SlotPatches.set_Connector(_initializing.Slot, connector);
                    PacketManager.Enqueue(new GenericComponentInitializePacket(_connector, _queuedInitializations + 1, _initializing));
                    return;
                }
                UniLog.Log($"Connector has reached initialization threshold and is being ignored: {(_connector != null ? _connector.GetType().ToString() : "Null Connector")}");
                return;
            }
            */
            _connector.Initialize();
        }
        public GenericComponentInitializePacket(IConnector connector, int depth, ImplementableComponent<IConnector> owner = null)
        {
            _connector = connector;
            _queuedInitializations = depth;
            _initializing = owner;
        }
    }
}
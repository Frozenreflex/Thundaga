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
        private readonly ImplementableComponent<IConnector> _initializing;
        public override void ApplyChange()
        {
            //this connector has likely been replaced by a refresh, ignore
            if (_connector == null || _initializing.Slot.IsDisposed || _initializing.Connector != _connector) return;
            _connector.Initialize();
        }
        public GenericComponentInitializePacket(IConnector connector, ImplementableComponent<IConnector> owner = null)
        {
            _connector = connector;
            _initializing = owner;
        }
    }
}
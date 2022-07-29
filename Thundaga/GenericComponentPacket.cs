using FrooxEngine;
using UnityNeos;

namespace Thundaga
{
    public class GenericComponentPacket : ConnectorPacket<IConnector>
    {
        public override void ApplyChange()
        {
            if (_connector?.Owner != null) _connector.ApplyChanges();
        }
        public GenericComponentPacket(IConnector connector)
        {
            _connector = connector;
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
        public override void ApplyChange()
        {
            _connector?.Initialize();
        }
        public GenericComponentInitializePacket(IConnector connector)
        {
            _connector = connector;
        }
    }
}
using System;
using FrooxEngine;

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
using FrooxEngine;

namespace Thundaga
{
    public class HeadsetPositionPacket : IConnectorPacket
    {
        public void ApplyChange()
        {
            var focusedWorld = Engine.Current.WorldManager.FocusedWorld;
            HeadOutputPatch.GlobalPosition = focusedWorld.LocalUserGlobalPosition;
            HeadOutputPatch.ViewPosition = focusedWorld.LocalUserViewPosition;
            HeadOutputPatch.GlobalRotation = focusedWorld.LocalUserGlobalRotation;
            HeadOutputPatch.ViewRotation = focusedWorld.LocalUserViewRotation;
        }
    }
}
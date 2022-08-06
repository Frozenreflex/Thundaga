# Thundaga
An experimental mod for NeosModloader that increases performance by splitting the Unity update loop and the Neos update loop into separate threads.

Any mod that doesn't touch the Unity connectors should be compatible with this mod. PhotonicFreedom is NOT compatible due to thread safety errors, but MotionBlurDisable and SsaoDisable should work.

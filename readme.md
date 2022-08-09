# Thundaga
An experimental mod for NeosModloader that increases performance by splitting the Unity update loop and the Neos update loop into separate threads.

This mod is unstable! It will have bugs, crashes, incompatibilities with other mods, and other issues. This repository is public for testing purposes, but the mod is not ready for regular usage. If you encounter bugs or issues, check if a Github issue has been created, and attach logs. Create issues if a bug has not been reported.

Any mod that doesn't touch the Unity connectors should be compatible with this mod. PhotonicFreedom is NOT compatible due to thread safety errors, but MotionBlurDisable and SsaoDisable should work.

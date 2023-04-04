I should've updated the readme awhile ago to mention this...

I have not had the motivation to work on this for a long time. I intentionally never gave out built binaries myself because the project was very much incomplete, but needed testing from people who knew that it was unstable and could add helpful github issues for me (or others) to work on. Building the project was an intentional barrier of entry. Instead, one person built it themselves and gave it to everyone else who asked for it, without telling them that IT WAS VERY INCOMPLETE, UNSTABLE, AND BROKEN, and then those people started whining and complaining that stuff was broken, without adding helpful github issues. That, Neos bleeding out and becoming more dead over time, being caught up in college work, and the rumored platform split and rendering engine update that was supposed to happen in December, then March, now who knows when anymore, killed my will to work on this.

If you are brave enough to continue working on this, fork it and let me know if you get any significant progress, I might want to work on it again if someone else has the will and the smarts to as well. I've had a couple of ideas to fix a couple of things and make stuff more stable that have lingered in the back of my head for awhile.

# Thundaga
An experimental mod for NeosModloader that increases performance by splitting the Unity update loop and the Neos update loop into separate threads.

This mod is unstable! It will have bugs, crashes, incompatibilities with other mods, and other issues. This repository is public for testing purposes, but the mod is not ready for regular usage. If you encounter bugs or issues, check if a Github issue has been created, and attach logs. Create issues if a bug has not been reported.

Any mod that doesn't touch the Unity connectors should be compatible with this mod. PhotonicFreedom is NOT compatible due to thread safety errors, but MotionBlurDisable and SsaoDisable should work.

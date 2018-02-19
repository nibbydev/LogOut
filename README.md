# LogOut app for Path of Exile

## What does it do?
* Closes all Path of Exile connections and logs the user out of the game when a hotkey is pressed
* Tracks health status and logs user out when health reaches set limit

## How to get it running?
1. Compile it yourself or download a compiled file from the [releases](https://github.com/siegrest/LogOut/releases/latest) page
2. Run the program and Path of Exile (admin privileges are required to close Path of Exile's connections)

## Set up hotkeys
1. Press the "Set hotkey" button and then whatever key. That key will now log you out of the game instantly
2. Optionally, you may click on "Settings" and check "Work minimized", which will make the hotkey log you out of the game even if Path of Exile is not the focused window

## Set up health tracking
1. Make sure the health bar above your character's head is visible
2. Click on "Settings" and enable the "Track health" checkbox
3. Set the poll rate (lower = faster) and health limit
4. Input your character's maximum life and energy shield
5. Enable "TCP disconnect" checkbox (make sure the program is running with elevated privileges)
6. Test with Righteous Fire gem or with Sapping Mana Flask

## Words of warning
* This has not been tested for suitability on HC
* Checking "Work minimized" is not advised
* Probably does not work with full-screen
* Health tracking is not always 100% accurate as the health bar moves around a lot

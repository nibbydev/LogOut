# Path of Exile TCP disconnect

## What does it do?
* Closes all Path of Exile connections and logs the user out of the game when a hotkey is pressed
* Tracks health status and plays a sound when health is low

## How to get it running?
1. Compile it yourself or download a compiled file from the [releases](https://github.com/siegrest/LogOut/releases/latest) page
2. Run the program and Path of Exile (admin privileges are required to close Path of Exile's connections) (there's no difference which one is launched first)

## Set up hotkeys
1. Press the "Set hotkey" button and then whatever key. That key will now log you out of the game instantly
2. Optionally, you may click on "Settings" and check "Work minimized", which will make the hotkey log you out of the game even if Path of Exile is not the focused window

## Set up health globe tracking
1. Click on "Settings" and enable the "Track health globe" checkbox
2. Set the poll rate (lower = faster) and the health percentage (30 = 30% of health left)
3. Make sure your game is running and the health globe is visible
4. Press the "Save health" button which will spawn an overlay over your health bar
5. Make sure the square overlay is more or less on top of the health globe
5. Click on the overlay to take a screenshot of your current health globe (this will be considered 100%)
6. Test with Righteous Fire gem or with Sapping Mana Flask

## Words of warning
* This has not been tested for suitability on HC
* When you screenshot your health globe, make sure nothing is obstructing it
* Checking "Work minimized" is not advised
* Probably does not work with full-screen
* Health tracking is not 100% accurate and probably won't track under ~5% as the game overlay obstructs the view
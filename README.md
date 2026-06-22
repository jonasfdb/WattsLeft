## Watts Left

Watts Left overhauls RTGs by letting you choose from a selection of isotopes, each with its own power output, half-life, and long-term behavior.

The mod adds persistent decay simulation for the isotopes, including for unloaded vessels. As time passes, your RTGs gradually lose output, and once they can no longer produce usable power, they die. Long-duration mission planning becomes a very different challenge this way!

## Features

In short, Watts Left adds...
- ...selectable RTG isotopes with different power outputs and half-lives
- ...persistent decay simulation, even for unloaded vessels
- ...comfortable B9PartSwitch-based isotope selection in the editor
- ...configs for most modern part mods
- ...the ability for RTGs to die once below usable output
- ...optional real isotope names and real half-life patches

## Compatible Part Mods

I want to include most modern part mods and I think this is a solid starting set. If you want more supported, let me know (see contact info below!). A supported mod in this case means that all RTGs included in it will use Watts Left.

| Part Mod              | Status | Details                     |
|-----------------------|--------|-----------------------------|
| Bluedog Design Bureau | ✅      | All RTGs supported          |
| Near Future Electrics | ✅      | All RTGs supported          |
| Tundra Exploration    | ✅      | All RTGs supported          |
| Sterling Systems      | ⚠️      | Only betavoltaic generators for now, more is planned |

## Other Compatible Stuff

#### Sol/RSS/RO? ✅

Watts Left is compatible with Sol, RSS and RO. It ships with optional patches for real isotope names and real-world half-lives, which are both recommended for a good experience with these mods. 

The patches can be found in ```GameData/WattsLeft/Patches```, and are ```realnames.cfg.disabled``` and ```realhalflives.cfg.disabled```. To enable them, rename them and remove the extension so that they become ```.cfg``` files.

Names and half-lives are handled by separate patches, so stock players can use real-world isotope names without switching to Sol, RSS, or RO-style balance.

#### Planet Packs? ✅

Watts Left is compatible with any other planet pack too, even those that change the Kerbin year length like Sol/RSS. The mod was specifically built with OPM players in mind, and the isotopes are balanced such that they provide a pleasant experience both in stock and with OPM.

#### Kerbalism? ⚠️

Watts Left can be used in a Kerbalism installation, but will not output any radiation or use Kerbalism RTG efficiency. There may be incompatibilities, but it should be safe to play. No guarantees, though!

#### Existing saves? ⚠️

Watts Left is safe to add to existing saves, but any already existing RTGs will not have a selected isotope. The game will continue to treat them as unmodded and undying.

## Absolutely Incompatible mods
- JDiminishingRTG

## Installation

**A CKAN download is planned! Until then, you have to do the manual way:**

First, get the dependencies. There's only a few:
- B9PartSwitch
- Module Manager

When you have these installed, installation is fairly standard! Download the latest Watts Left release from here or Spacedock, unzip it, copy the GameData folder, paste it into your KSP root directory, and overwrite files if asked to. Done!

## License and Contact

This project is licensed under the **Mozilla Public License 2.0 (MPL-2.0)**. See [`LICENSE`](./LICENSE) for the full text of the license. For a summary of the MPL-2.0, visit [choosealicense.com](https://choosealicense.com/licenses/mpl-2.0/).

Watts Left is developed and maintained with love and care by me, jonasfdb <3. I can best be reached through Discord under the username **jonasfdb** if it is urgent. Otherwise, keep contact to Issues, Discussions and pull requests here.

## Acknowledgements

Watts Left is a spiritual successor to the original JDiminishingRTG by KwirkyJ, later maintained by linuxgurugamer. Since I built this one from the ground up, I can't really give credit under license, but the inspiration definitely came from there and Watts Left is a more modernized equivalent. Thanks!

Thanks to my tester(s), Dwldjon!
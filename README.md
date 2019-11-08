# <img src="https://raw.githubusercontent.com/ho0ber/NK2Tray/master/NK2Tray/nk2tray.ico" height="24"> NK2Tray
A simple system tray utility to control the Windows Audio Mixer with a Korg nanoKONTROL2 and other similar control surfaces.

(A rewrite of [nk2-audio](https://github.com/ho0ber/nk2-audio))

## Download
To download the latest version of NK2Tray, go to this project's [**Releases page**](https://github.com/ho0ber/NK2Tray/releases).

## Getting Involved
To help test or contribute, I encourage you to join our discord: https://discord.gg/BtVTYxp

Current development work is being tracked in this repository's [Github Project](https://github.com/ho0ber/NK2Tray/projects/1).

## Disclaimer
I'm not a Windows developer. There's a lot in here that's far from best practices, and I welcome you to help improve the code. This project is an attempt to significantly improve the stability and extensibility of a project that was first written in AutoHotkey, so even my most clumsy attempts are already much better than the old codebase. Please reach out to me if you have experience fighting with Windows in code and want to take an active role in this project.

Also, I'm pretty sure I have to release this under the Ms-PL license if I want it to be open source, because that's what [NAudio](https://github.com/naudio/NAudio) is licensed under.

## Project Goals
* Improved stability over nk2-audio by means of better maintainability
* Reduced conflicts with games by not using AutoHotkey (which is rejected by some anti-cheat measures)
* Additional device support with minimal additional coding/configuration
* Greatly improved potential for features in the long term
* Improved performance and precision over nk2-audio when setting volumes

## Planned Features
- [x] Tray menu configuration of faders to volume mixer sessions
- [x] Configurable Master(device) volume and system sounds support
- [x] Midi feedback to light controls upon activation
- [x] On the fly binding of sessions using "S" buttons
- [x] Media button support
- [x] Persistence of configuration across sessions (restoring on reopening NK2Tray)
- [x] Multi-session differentiation in persistence (2 instances of Discord, for example)
- [x] Support for processes changing (closing/reopening application) and retaining a handle to its session
- [ ] Detection of midi device connect/disconnect
- [ ] Selectable input/output midi devices (defaulting to nano)
- [ ] Support for common amazon clones of nanokontrol2
- [ ] (Eventual) support for advanced control surface features (motorized faders, volume meters, scribble strips)

##Currently Supported Devices
At the moment the support for the MIDI devices is limited. It will be extended in the future.

The currently supported MIDI devices are:
* Korg nanoKONTROL2
* Behringer Xouch Mini (only in MC Mode)

## Attribution & Acknowledgements
 * <img src="https://raw.githubusercontent.com/ho0ber/NK2Tray/master/NK2Tray/nk2tray.ico" height="24"> [NK2 Tray icon](NK2Tray/nk2tray.ico) courtesy of [Dave Savic](https://www.iconfinder.com/icons/2001872/blue_level_levels_mixer_settings_shadow_volume_icon)
 * Midi and Core Audio thanks to [NAudio](https://github.com/naudio/NAudio)


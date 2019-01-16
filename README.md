# NK2Tray
A simple system tray utility to control the Windows Audio Mixer with a Korg nanoKONTROL2 and other similar control surfaces.

(A rewrite of [nk2-audio](https://github.com/ho0ber/nk2-audio))

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
- [ ] On the fly binding of sessions using "S" buttons
- [ ] Media button support
- [x] Persistence of configuration across sessions (restoring on reopening NK2Tray)
- [ ] Multi-session differentiation in persistence (2 instances of Discord, for example)
- [x] Support for processes changing (closing/reopening application) and retaining a handle to its session
- [ ] Detection of midi device connect/disconnect
- [ ] Selectable input/output midi devices (defaulting to nano)
- [ ] Support for common amazon clones of nanokontrol2
- [ ] (Eventual) support for advanced control surface features (motorized faders, volume meters, scribble strips)

# RAWSim-O demonstrator

This directory contains the real-life demonstrator of RAWSim-O, i.e., the
applications and hardware drivers that were used to control physical robots
(vacuum cleaning robots with a line follower), stations and further hardware
with RAWSim-O. A video of the demonstrator can be found
[here](https://youtu.be/bZHIXCxpXyc).

| Project                    | Purpose                                                          |
| -------------------------- | ---------------------------------------------------------------- |
| `RAWSimO.AppServer`        | Server application connecting the robots / stations and the simulation (controller) |
| `RAWSimO.AppRobot`         | Application running on the robots                                 |
| `RAWSimO.AppInputStation`  | Replenishment (input) station application                         |
| `RAWSimO.AppOutputStation` | Picking (output) station application                              |
| `RAWSimO.Hardware`         | Hardware drivers (camera / line follower, barcode, RFID, Blink(1)) |
| `Lib`                      | Third-party binaries used by the projects above                   |

The demonstrator uses the shared communication layer `RAWSimO.CommFramework`
from the main project. All demonstrator projects are Windows only.

## License

In contrast to the rest of RAWSim-O (MIT licensed), the contents of this
directory are licensed under the **GNU General Public License v3.0** (see
[LICENSE](LICENSE)), since they depend on [Emgu CV](https://www.emgu.com/),
which is GPL-3.0 licensed. Licenses of the third-party components can be found
in [ThirdPartyLicenses.txt](ThirdPartyLicenses.txt).

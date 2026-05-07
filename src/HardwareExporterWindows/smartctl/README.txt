smartmontools (https://www.smartmontools.org/) — bundled with HardwareExporterWindows
======================================================================================

This directory contains a redistribution of smartmontools binaries used by
HardwareExporterWindows to read SMART data from disks attached behind storage
controllers (SAS HBA, RAID) where LibreHardwareMonitor's ATA pass-through path
does not work.

Files in this directory:

  smartctl.exe   The smartctl utility, statically linked (MinGW-w64).
  drivedb.h      Device-specific SMART attribute database used by smartctl.
  COPYING.txt    GNU General Public License v2 — the smartmontools license.
  AUTHORS.txt    smartmontools authors and contributors.

License
-------

smartmontools is licensed under the GNU General Public License v2 or later.
See COPYING.txt in this directory for the full text.

HardwareExporterWindows itself is licensed under the MIT License (see the
project root LICENSE file). smartmontools is included here as an aggregated
work — HardwareExporterWindows invokes smartctl.exe as a separate process
and does not statically or dynamically link against smartmontools code.

Source code and changes
-----------------------

Upstream source is available at:

  https://github.com/smartmontools/smartmontools
  https://www.smartmontools.org/wiki/Download

This redistribution is unmodified from the upstream Windows release of
smartmontools 7.5 (https://www.smartmontools.org/), packaged with this
project for end-user convenience.

To replace the bundled smartctl with a different version, set
`SmartMonitor:SmartctlPath` in appsettings.json to the absolute path of
your preferred smartctl.exe.

# RAWSim-O

RAWSim-O is a discrete event-based simulation for Robotic Mobile Fulfillment Systems. The intention of the simulation framework is to provide a tool for researching effects of multiple decision problems that occur when running such a system. For this, the framework enables easy extensibility for implementing new decision methods for the different decision problems.
Copyright (C) 2017 Marius Merschformann

## Quick start

Open RAWSimO.sln with Visual Studio and select RAWSimO.Visualization as the project to execute. Under the "Instances" press the checkmark button to generate a default instance. Then go to the "Base Controls" tab and press the play button. Depending on the instance size and simulation settings instance generation and simulation initialization may take longer.

## Controller implementation quick start

In the following video you can find a very short tutorial for implementing your own controller logic: https://youtu.be/ClN7NZL930w

## Screenshots

Image showing a larger instance being simulated:

![Larger instance](Material/Screenshots/larger-instance-3d.png)

Image showing a multi-level instance being simulated:

![Multi floor instance](Material/Screenshots/multi-floor-3d.png)

Image showing more detailed information for path planning engines:

![Path sample](Material/Screenshots/paths-2d.png)

Image showing a heatmap rendered using RAWSim-O that shows the locations robots were at over time:

![Heatmap](Material/Screenshots/heatmap-sample-small.png)

## Demonstrator video

A short video of the demonstrator application done with RAWSim-O and vacuum cleaning robots can be found here: https://youtu.be/bZHIXCxpXyc

## GnuPlot

Some of the functionality of RAWSim-O requires an installation of GnuPlot (see http://www.gnuplot.info/). GnuPlot's bin path must be added to the environment path variable.

# License

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.

# Credits

This work was created as a part of the RMFS project initiated by Prof. Lin Xie. More information about the project can be found here https://www.researchgate.net/project/automated-robotic-mobile-fulfillment-systems-RMFS and here http://www.leuphana.de/universitaet/personen/lin-xie.html
The work was financed by a scholarship of the International Graduate School - Dynamic Intelligent Systems of the University of Paderborn (https://pace.uni-paderborn.de/pace-phd-programs/igs/)

## Contributers of the project

Marius Merschformann, Lin Xie, Hanyi Li, Tim Lamballais Tessensohn, Daniel Erdmann, Lena Knickmeier, Jonas König, Maik Herbort, Marcel Grawe

# Alphabet Soup

Special thanks go to the authors of the Alphabet Soup framework that was used as a basis for implementation of RAWSim-O:

Hazard, Christopher J., Peter R. Wurman, and Raffaello D’Andrea. "Alphabet soup: A testbed for studying resource allocation in multi-vehicle systems." Proceedings of the 2006 AAAI Workshop on Auction Mechanisms for Robot Coordination. 2006.

# Used software:
Thanks go out to the developers of the following software for enabling a more easy implementation of RAWSim-O.
## Helix Toolkit: ##
https://github.com/helix-toolkit/helix-toolkit
## WriteableBitmapEx ##
https://writeablebitmapex.codeplex.com/
## Emgu CV ##
http://www.emgu.com/
## Open CV ##
http://opencv.org/
## HidLibrary ##
https://github.com/mikeobrien/HidLibrary
## directshow.net library ##
http://directshownet.sourceforge.net/
## Blink(1) library ##
https://github.com/todbot/blink1
## ZXing.Net ##
https://zxingnet.codeplex.com/

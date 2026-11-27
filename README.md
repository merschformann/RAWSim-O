# RAWSim-O

RAWSim-O is a discrete event-based simulation for Robotic Mobile Fulfillment Systems. The intention of the simulation framework is to provide a tool for researching effects of multiple decision problems that occur when running such a system. For this, the framework enables easy extensibility for implementing new decision methods for the different decision problems.
Copyright (C) 2017 Marius Merschformann

> [!NOTE]
> The modernization of RAWSim-O (.NET 10, cross-platform visualization, 3D view) was implemented with the support of AI coding tools. The simulation itself predates them.

## First impressions

2D view in action:

![Larger instance](Material/Wiki/rawsim-o-2d.gif)

3D view in action:

![Larger instance](Material/Wiki/rawsim-o-3d.gif)

## Quick start

Download the visualization for your platform (Linux, Windows, macOS) from the [releases](https://github.com/merschformann/RAWSim-O/releases) and run `RAWSimO.Visualization.Avalonia` in the extracted folder. Alternatively, run it from source with the [.NET 10 SDK](https://dotnet.microsoft.com/download):

```bash
dotnet run --project RAWSimO.Visualization.Avalonia -c XPlat
```

Press "Generate" to generate a default instance (its parameters can be changed in the "Instance" tab of the setup panel), then press "▶ Start". Depending on the instance size and simulation settings, instance generation and simulation initialization may take a while. You should see a first simulation running now. "3D" switches to a 3D view of all tiers.

The `XPlat` configuration builds all cross-platform projects. The Windows-only [demonstrator](demo/) applications are built by the `Debug` / `Release` configurations.

The following video gives a short tutorial of RAWSim-O's core functionality (recorded with the previous, Windows-only visualization):
[![Quick tutorial](Material/Wiki/youtube-thumbnails/quick-intro-player.PNG)](https://youtu.be/l3qwDnbEPhI "Quick intro")

## Controller implementation quick start

In the following video you can find a very short tutorial for implementing your own controller logic:
[![Controller implementation tutorial](Material/Wiki/youtube-thumbnails/controller-implementation-player.PNG)](https://youtu.be/ClN7NZL930w "Controller implementation tutorial")

## Screenshots

Image showing a larger instance being simulated:

![Larger instance](Material/Wiki/larger-instance-3d.png)

Image showing a multi-level instance being simulated:

![Multi floor instance](Material/Wiki/multi-floor-3d.png)

Image showing more detailed information for path planning engines:

![Path sample](Material/Wiki/paths-2d.png)

Image showing a heatmap rendered using RAWSim-O that shows the locations robots were at over time:

![Heatmap](Material/Wiki/heatmap-sample-small.png)

## Demonstrator video

The demonstrator applications can be found in [demo/](demo/).

A short video of the demonstrator application done with RAWSim-O and vacuum cleaning robots can be found here: 
[![Demonstrator](Material/Wiki/youtube-thumbnails/demonstrator-player.PNG)](https://youtu.be/bZHIXCxpXyc "Demonstrator")

## GnuPlot

Some of the functionality of RAWSim-O requires an installation of GnuPlot (see http://www.gnuplot.info/). GnuPlot's bin path must be added to the environment path variable.

# License

RAWSim-O is licensed under the [MIT License](LICENSE).

The real-life demonstrator in [demo/](demo/) (robot, server and station applications as well as hardware drivers) is an exception: it depends on the GPL-3.0 licensed Emgu CV library and is therefore licensed under the [GNU General Public License v3.0](demo/LICENSE). The simulation itself (core, visualization, CLI, etc.) does not depend on the demonstrator.

Licenses of third-party components of the demonstrator can be found in [demo/ThirdPartyLicenses.txt](demo/ThirdPartyLicenses.txt).

# Publications

RAWSim-O has been published here:
- Marius Merschformann, Lin Xie, Hanyi Li: RAWSim-O: A Simulation Framework for Robotic Mobile Fulfillment Systems, Logistics Research (2018), Volume 11, Issue 1, doi:10.23773/2018_8, [link](https://www.bvl.de/lore/all-volumes--issues/volume-11/issue-1/rawsim-o-a-simulation-framework-for-robotic-mobile-fulfillment-systems)

Further reading on some projects using RAWSim-O can be found here (let me know, if yours is missing):
- Marius Merschformann, Lin Xie, Daniel Erdmann: Multi-Agent Path Finding with Kinematic Constraints for Robotic Mobile Fulfillment Systems, working paper preprint available at [link](https://arxiv.org/abs/1706.09347)
- Marius Merschformann, Tim Lamballais, René de Koster, Leena Suhl: Decision Rules for Robotic Mobile Fulfillment Systems, Operations Research Perspectives, Volume 6 (2019), doi:10.1016/j.orp.2019.100128, available at [link](https://doi.org/10.1016/j.orp.2019.100128)
- Marius Merschformann: Active repositioning of storage units in Robotic Mobile Fulfillment Systems, Selected Papers of the Annual International Conference of the German Operations Research Society (GOR), Freie Universität Berlin, Germany, September 6-8, 2017, doi:10.1007/978-3-319-89920-6_51, [link](https://link.springer.com/chapter/10.1007/978-3-319-89920-6_51)

# Credits

This work was created as a part of the RMFS project initiated by Prof. Lin Xie. More information about the project can be found here https://www.researchgate.net/project/automated-robotic-mobile-fulfillment-systems-RMFS and here https://www.leuphana.de/institute/iis/personen/lin-xie.html

The work was supported by the Ecopti GmbH (Beijing Hanning ZN Tech) and a scholarship of the International Graduate School - Dynamic Intelligent Systems of the University of Paderborn (https://pace.uni-paderborn.de/pace-phd-programs/igs/)

## Contributers of the project

Marius Merschformann, Lin Xie, Hanyi Li, Tim Lamballais Tessensohn, Daniel Erdmann, Lena Knickmeier, Jonas König, Maik Herbort, Marcel Grawe

# Alphabet Soup

Special thanks go to the authors of the Alphabet Soup framework that was used as a basis for implementation of RAWSim-O:

Hazard, Christopher J., Peter R. Wurman, and Raffaello D’Andrea. "Alphabet soup: A testbed for studying resource allocation in multi-vehicle systems." Proceedings of the 2006 AAAI Workshop on Auction Mechanisms for Robot Coordination. 2006.

# Used software:
Thanks go out to the developers of the following software for enabling a more easy implementation of RAWSim-O.
## Avalonia ##
https://avaloniaui.net/
## SkiaSharp ##
https://github.com/mono/SkiaSharp
## Aquarium ##
https://github.com/merschformann/aquarium
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

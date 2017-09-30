#!/bin/bash

Bots=$1
SKU=$2
FilteredFootprintFile=footprints-Bots${Bots}-SKU${SKU}.csv

if [ "$#" -ne 2 ]; then
    echo "Illegal number of parameters"
else
    head -n 1 footprints.csv > $FilteredFootprintFile ; cat footprints.csv | perl -wne "print if /MaTi-SKU"${SKU}";/ && (/MaTiLarge-"${Bots}"/ || /MaTiMedium-"${Bots}"/ || /MaTiSmall-"${Bots}"/)" >> $FilteredFootprintFile
fi


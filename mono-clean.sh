#!/bin/bash

echo Calling: xbuild /t:Clean /p:Configuration=Mono /p:Platform=x64 RAWSimO.sln
xbuild /t:Clean /p:Configuration=Mono /p:Platform=x64 RAWSimO.sln

read word

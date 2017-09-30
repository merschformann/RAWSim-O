#!/bin/bash

echo Calling: xbuild /p:Configuration=Mono /p:Platform=x64 RAWSimO.sln
xbuild /p:Configuration=Mono /p:Platform=x64 RAWSimO.sln

read word

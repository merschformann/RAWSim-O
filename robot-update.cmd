::SET repopath=%~dp0
::cd %repopath:~0,-1%

hg pull

hg update

"C:\Program Files\MSBuild\14.0\Bin\msbuild" /t:Clean /p:Configuration=Debug /p:Platform=x86 RAWSimO.sln

"C:\Program Files\MSBuild\14.0\Bin\msbuild" /p:Configuration=Debug /p:Platform=x86 RAWSimO.sln

pause
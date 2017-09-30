#!/usr/bin/env python

import datetime
import os.path
import random
import sys
import time

if len(sys.argv) != 5:
    print("Usage: ./tarscript_generator.py <executeOption> <dirToCompress> <outputFileName> <timeLimitInSeconds>")
    sys.exit(1)

# ---> Define additional functions
# --> Some global fields
global tarscriptname
tarscriptname = "tarscript.sh"
# --> Tar script generation
def generatetarscript(workingdir, outputfilename, timelimitinsec):
	# Write intermediate script for compressing
	with open(tarscriptname, 'w') as fp:
		fp.write("#!/bin/bash \n")
		fp.write("#PBS -N tarscript\n")
		fp.write("#PBS -o tarscript.$PBS_JOBID.log\n")
		fp.write("#PBS -j oe\n")
		fp.write("#PBS -q batch\n")
		fp.write("#PBS -m a\n")
		fp.write("#PBS -M mmarius@mail.upb.de\n")
		fp.write("#PBS -l walltime={0}\n".format(timelimitinsec))
		fp.write("WORKDIR={0}\n".format(workingdir.replace("&", "$")))
		fp.write("cd $WORKDIR \n")
		fp.write("# Output info\n")
		fp.write("echo ------------------------------------------------------\n")
		fp.write("echo -n 'Job is running on node '; cat $PBS_NODEFILE\n")
		fp.write("echo ------------------------------------------------------\n")
		fp.write("echo PBS: job identifier is $PBS_JOBID\n")
		fp.write("echo PBS: job name is $PBS_JOBNAME\n")
		fp.write("echo PBS: node home directory is $PBS_O_HOME\n")
		fp.write("echo PBS: node working directory is $WORKDIR\n")
		fp.write("echo PBS: frontend working directory is $PBS_O_WORKDIR\n")
		fp.write("echo PBS: PATH = $PBS_O_PATH\n")
		fp.write("echo PBS: execution mode is $PBS_ENVIRONMENT\n")
		fp.write("echo PBS: qsub is running on $PBS_O_HOST\n")
		fp.write("echo PBS: originating queue is $PBS_O_QUEUE\n")
		fp.write("echo PBS: executing queue is $PBS_QUEUE\n")
		fp.write("echo PBS: array id is $PBS_ARRAYID\n")
		fp.write("echo ' '\n")
		fp.write("echo ------------------------------------------------------\n")
		fp.write("# Output warning\n")
		fp.write("early()\n")
		fp.write("{\n")
		fp.write(" echo ' '\n")
		fp.write(" echo ' ############ WARNING:  EARLY TERMINATION #############'\n")
		fp.write(" echo ' '\n")
		fp.write("}\n")
		fp.write("trap 'early' 2 9 15\n")
		fp.write("# Compress\n")
		fp.write("tar -zcvf {0} *\n".format(outputfilename))
		fp.write("exit\n")

print "Generating tar script ..."

# Get input information
directCall = True if sys.argv[1] == "direct" else False # indicates whether to execute immediately
torqueMode = True if sys.argv[1] == "torque" else False # indicates that a torque scheduler run will be prepared
ccsMode = True if not directCall and not torqueMode else False # Indicate that an ccs run will be prepared
dirToCompress = sys.argv[2] # the directory to compress
outputFileName = sys.argv[3] # the name of the output file
timeLimitInSeconds = sys.argv[4] # the time limit in seconds for the job

# Prepare subscript
generatetarscript(dirToCompress, outputFileName, timeLimitInSeconds)

# Check mode and prepare call
call = ""
if directCall:
	call = tarscriptname
elif torqueMode:
	call = "qsub {0}".format(tarscriptname)
elif ccsMode:
	call = "ccsalloc -t {1} --name tarscript --group=hpc-prf-dsor --join=oe --quiet --output=tarscript.%reqid.out bash {0}".format(tarscriptname, timeLimitInSeconds)
else:
	print "No suitable mode detected!"

# Execute call
if call != "":
	print "Calling: {0}".format(call)
	os.system(call)

# Finish
print ".Fin."

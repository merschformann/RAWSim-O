#!/usr/bin/env python

import datetime
import distutils.dir_util
import glob
import os
import os.path
import random
import socket
import subprocess
import sys
import thread
import time
from itertools import product
from threading import Thread

if len(sys.argv) != 20:
    print("Usage: ./experiment_main.py <executeOption> <monoORnet> <exe> <instance dir> <setting dir> <config dir> <repo dir> <output dir> <log dir> <timeout per run in s - ccs, torque> <firstSeed> <seedCount> <group - ccs> <ncpus - ccs> <memGB - ccs> <worker timeout in hours - desktop> <torque working dir (& instead $) - torque> <torque queue name - torque> <batchsize - ccs, torque>")
    sys.exit(1)

# ---> Define additional functions
# --> Some global fields
global exitrequested
exitrequested = False
global joblistdelimiter
joblistdelimiter = ';'
global torquewrapperfilename
torquewrapperfilename = "torquerelay.sh"
global ccswrapperfilename
ccswrapperfilename = "ccsrelay.sh"
global joblistcallhandlerfilename
joblistcallhandlerfilename = "listjobcallhandler.py"
global relayscriptname
relayscriptname = "relay.sh"
# --> Logging
global logfilename
logfilename = "experiment.log"
if os.path.isfile(logfilename):
	os.remove(logfilename)
def log(message):
	timestampedmessage = "{0}: {1}".format(datetime.datetime.now().strftime("%Y-%m-%d %H:%M:%S"), message)
	print(timestampedmessage)
	with open(logfilename, "a") as logfile:
		logfile.write("{0}\n".format(timestampedmessage))
# --> Get last commit message
def getrevisioninfo(repodir):
	revisioninfo = subprocess.Popen(["hg", "log", "-R", repodir, "-r", "tip"], stdout=subprocess.PIPE).communicate()[0]
	return revisioninfo
# --> Timeout monitoring
lock = thread.allocate_lock()
def timeoutmonitor(arg):
	requeststarttimes = arg[0]
	requstidentsperjob = arg[1]
	jobidentsinprogress = arg[2]
	jobidentstodo = arg[3]
	jobidentsdone = arg[4]
	while not exitrequested:
		# Acquire lock before accessing the lists
		lock.acquire()
		# See whether a run timed out
		for jobident in jobidentsinprogress.keys():
			if datetime.datetime.now() - requeststarttimes[requstidentsperjob[jobident]] > runtimeout:
				log("Job timed out: {0}".format(jobident))
				jobidentstodo[jobident] = jobidentsinprogress.pop(jobident)
		# Release lock for the lists
		lock.release()
		# Sleep before checking the next time
		time.sleep(60)
# --> Make a file executable
def makeexecuteable(path):
	mode = os.stat(path).st_mode
	mode |= (mode & 0o444) >> 2 # copy R bits to X
	os.chmod(path, mode)
# --> Ident generation
def generateident(currentset):
	while True:
		nextident = random.randint(0, 1000000)
		if nextident not in currentset:
			return nextident
# --> List of jobs preparation
def savejoblist(jobs):
	# Write job list
	jobid = 0
	with open("joblist.csv", 'w') as fp:
		for job in jobs:
			fp.write("{0}\n".format(joblistdelimiter.join(str(e) for e in [jobid, job[0], job[1], job[2], job[3]])))
			jobid += 1
	# Return job count
	return jobid
# --> Torque script generation
def generatetorquewrapperscript(workingdir, logdir, timelimitinsec, torquequeue, ncpus, mem):
	# Write intermediate script for the job
	with open(torquewrapperfilename, 'w') as fp:
		fp.write("#!/bin/bash \n")
		fp.write("#PBS -N awsimopt\n")
		fp.write("#PBS -o {0}/awsimopt.$PBS_ARRAYID.log\n".format(logdir.replace("&", "$")))
		fp.write("#PBS -j oe\n")
		fp.write("#PBS -q {0}\n".format(torquequeue))
		fp.write("#PBS -m a\n")
		fp.write("#PBS -M mmarius@mail.upb.de\n")
		fp.write("#PBS -l nodes=1:cx1000:ppn={0},mem={1}g,walltime={2}\n".format(ncpus, mem, timelimitinsec))
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
		fp.write("# Call script\n")
		fp.write("if [ $# -ne 0 ]; then\n")
		fp.write("  python {0} $PBS_ARRAYID $1\n".format(joblistcallhandlerfilename))
		fp.write("else\n")
		fp.write("  python {0} $PBS_ARRAYID\n".format(joblistcallhandlerfilename))
		fp.write("fi\n")
		fp.write("exit\n")
	makeexecuteable(torquewrapperfilename)
# --> CCS script generation
def generateccswrapperscript(logdir, timelimitinsec, ncpus, mem, group, cx1000):
	# Write intermediate script for the job
	with open(ccswrapperfilename, 'w') as fp:
		fp.write("#!/bin/bash \n")
		fp.write("\n")
		fp.write("#CCS --res=rset=1:ncpus={0}:mem={1}g{2}\n".format(ncpus, mem, ":cx1000=true" if cx1000 else ""))
		fp.write("#CCS -t {0}\n".format(timelimitinsec))
		fp.write("#CCS --name=AWSO\n")
		fp.write("#CCS --group={0}\n".format(group))
		fp.write("#CCS --join=oe\n")
		fp.write("#CCS --output={0}/awsimopt.%A_%a.out\n".format(logdir))
		fp.write("#CCS --tracefile={0}/awsimopt.%A.trace\n".format(logdir))
		fp.write("\n")
		fp.write("# Call script \n")
		fp.write("if [ $# -ne 0 ]; then\n")
		fp.write("  python {0} $CCS_ARRAY_INDEX $1\n".format(joblistcallhandlerfilename))
		fp.write("else\n")
		fp.write("  python {0} $CCS_ARRAY_INDEX\n".format(joblistcallhandlerfilename))
		fp.write("fi\n")
		fp.write("exit\n")
	makeexecuteable(ccswrapperfilename)
# A lightweight wrapper that parses the list of jobs and executes the one with the given ID
def generatejoblistcallwrapper(outputdir, execall):
	# Write intermediate script for the job
	with open(joblistcallhandlerfilename, 'w') as fp:
		fp.write("#!/usr/bin/python \n")
		fp.write("import sys\n")
		fp.write("import os\n")
		fp.write("# --> Set some parameters\n")
		fp.write("startJobID = int(sys.argv[1])\n")
		fp.write("batchsize = 1\n")
		fp.write("if len(sys.argv) > 2:\n")
		fp.write("\tbatchsize = int(sys.argv[2])\n")
		fp.write("joblistdelimiter = \'{0}\'\n".format(joblistdelimiter))
		fp.write("outputdir = \"{0}\"\n".format(outputdir).replace("\\", "\\\\"))
		fp.write("# --> Execute the job(s) given by retrieving it/them from the joblist\n")
		fp.write("for jobID in xrange(startJobID, startJobID + batchsize):\n")
		fp.write("\tprint(r\"JOBLISTHANDLER: looking for job with ID: {0}\".format(jobID))\n")
		fp.write("\tsys.stdout.flush()\n")
		fp.write("\tjob = []\n")
		fp.write("\tcurrentjob = []\n")
		fp.write("\twith open(\"joblist.csv\", \"r\") as joblist:\n")
		fp.write("\t\tfor line in joblist:\n")
		fp.write("\t\t\t# Parse line\n")
		fp.write("\t\t\tcurrentjob = line.strip().split(joblistdelimiter)\n")
		fp.write("\t\t\tif int(currentjob[0]) == jobID:\n")
		fp.write("\t\t\t\t# We found the job - quit looking for it\n")
		fp.write("\t\t\t\tprint(r\"JOBLISTHANDLER: found the job!\")\n")
		fp.write("\t\t\t\tsys.stdout.flush()\n")
		fp.write("\t\t\t\tjob = currentjob\n")
		fp.write("\t\t\t\tbreak\n")
		fp.write("\tif(job != []):\n")
		fp.write("\t\tcall = \"{0} {{0}} {{1}} {{2}} {{3}} {{4}} {{5}}\".format(job[1], job[2], job[3], outputdir, job[4], jobID)\n".format(execall.replace("\\", "\\\\")))
		fp.write("\t\tprint(r\"JOBLISTHANDLER: calling: {0}\".format(call))\n")
		fp.write("\t\tsys.stdout.flush()\n")
		fp.write("\t\tos.system(call)\n")
		fp.write("\telse:\n")
		fp.write("\t\tprint(r\"JOBLISTHANDLER: could not find job with ID: {0}\".format(jobID))\n")
		fp.write("\t\tsys.stdout.flush()\n")
# Another wrapper that limits memory and other things
def generaterelayscript(mem):
	# Generate a relay file to limit memory and other things
	with open(relayscriptname, 'w') as fp:
		fp.write("#!/bin/bash\n")
		fp.write("ulimit -v {0} # in kb ({1} GB)\n".format(mem * 1024 * 1024, mem))
		fp.write("ulimit -c 0 # disable core dumps\n")
		fp.write("$@\n")

log(">>> Starting new experiment")
log("Fetching information ...")

# Set some values
requestjobsignal = "1"
submitfinishedsignal = "2"
sleepmodesignal = "3"
executesignal = "4"
socketcommunicationdelimiter = ';'
port = 31353

experimentstarttime = datetime.datetime.now()

# Seed random value generation
random.seed(1)

# Check operating system
useRelayScript = sys.platform == "linux" or sys.platform == "linux2"

# Get input information
setuptest = True if sys.argv[1] == "test" else False # Indicates that only the preliminaries before the individual submission will be tested
directMode = True if sys.argv[1] == "direct" else False # indicates whether to execute immediately
hostServer = True if sys.argv[1] == "server" else False # indicates that a server has to be hosted
torqueMode = True if sys.argv[1] == "torque" else False # indicates that a torque scheduler run will be prepared
ccsModeHTC = True if sys.argv[1] == "ccshtc" else False # indicates that a ccs scheduler run on HTC will be prepared
ccsModeArminius = True if sys.argv[1] == "ccsarminius" else False # indicates that a ccs scheduler run on Arminius will be prepared
mono = sys.argv[2] == "mono" # specifies whether mono is used for calling the exe
exe = sys.argv[3] # the executable
repopath = os.path.normpath(sys.argv[7]) # the repository path
resourcedir = os.path.join(repopath, "Material", "Resources") # the directory containing additional resources which is copied to the working dir before submitting jobs
out_dir = sys.argv[8] # the output directory
logdir = os.path.normpath(sys.argv[9]) # the log dir
cpu_time_algorithm = int(sys.argv[10]) # the timeout per algorithm run
firstSeed = int(sys.argv[11]) # the first seed to start with
seedCount = int(sys.argv[12]) # the number of seeds to run
group = sys.argv[13] # the group to use when allocating the jobs
ncpus = sys.argv[14] # the number of cpus per job
mem = int(sys.argv[15]) # the amount of memory per job
runtimeout = datetime.timedelta(hours=float(sys.argv[16])) # the timeout per when running server mode
torqueworkingdir = os.path.normpath(sys.argv[17]) # the working directory to use when running on a torque cluster
torquequeue = sys.argv[18] # the torque queue to submit the jobs to
batchsize = int(sys.argv[19]) # the number of jobs in one batch

# Output last revision info to better identify the code version
log("Last revision info of repo:\n{0}".format(getrevisioninfo(repopath)))

# Filter and build the list of instances
instance_filter = os.path.join(sys.argv[4],"*.xinst")
layout_filter = os.path.join(sys.argv[4],"*.xlayo")
log("Finding instances by filtering with {0} and {1}".format(instance_filter, layout_filter))
instanceListing = [os.path.abspath(path) for path in (glob.glob(instance_filter) + glob.glob(layout_filter))]

# Filter and build the list of settings
setting_filter = os.path.join(sys.argv[5],"*.xsett")
log("Finding settings by filtering with {0}".format(setting_filter))
settingListing = [os.path.abspath(path) for path in glob.glob(setting_filter)]

# Filter and build the list of configurations
config_filter = os.path.join(sys.argv[6],"*.xconf")
log("Finding configurations by filtering with {0}".format(config_filter))
configListing = [os.path.abspath(path) for path in glob.glob(config_filter)]

# Build a list of seeds to use
seedListing = xrange(firstSeed, firstSeed + seedCount)

# Build the list of jobs
instanceConfigSeedListing = []
for job in product(instanceListing, settingListing, configListing, seedListing):
    instanceConfigSeedListing.append(job)
random.shuffle(instanceConfigSeedListing) # shuffle the jobs

# Log some info
log("Found {0} jobs for {1} instances {2} settings {3} configs and {4} seeds".format(len(instanceConfigSeedListing),len(instanceListing),len(settingListing),len(configListing),len(seedListing)))

# --> Check whether it was just a test
if setuptest:
	# This was just a test
	log("Setup test finished!")
# --> Check whether it's direct call mode
if directMode:
	# Copy resource dir content to working dir
	log("Copying resources to current directory ...")
	distutils.dir_util.copy_tree(resourcedir, "./Resources")
	# Prepare torque mode
	log("Preparing files for direct call ...")
	log("Saving jobs to file ...")
	joblistcount = savejoblist(instanceConfigSeedListing)
	log("Generating memory limiting relay script ...")
	generaterelayscript(mem)
	log("Generating call wrapper ...")
	generatejoblistcallwrapper(out_dir, "{0}{1}{2}".format("bash {0} ".format(relayscriptname) if mono else "", "mono " if mono else "", os.path.normpath(exe)))
	# Directly call the jobs
	log("Calling the jobs ...")
	for jobID in xrange(0, joblistcount):
		log("Calling Job with ID: {0}".format(jobID))
		call = "python {0} {1}".format(joblistcallhandlerfilename, jobID)
		log(call)
		os.system(call)
# --> Check whether it's CCS mode
if ccsModeHTC or ccsModeArminius:
	# Copy resource dir content to working dir
	log("Copying resources to current directory ...")
	distutils.dir_util.copy_tree(resourcedir, "./Resources")
	# Prepare ccs mode
	log("Preparing files for ccs ...")
	log("Saving jobs to file ...")
	joblistcount = savejoblist(instanceConfigSeedListing)
	log("Generating ccs relay script ...")
	generateccswrapperscript(logdir, cpu_time_algorithm * batchsize, ncpus, mem, group, True if ccsModeHTC else False)
	log("Generating memory limiting relay script ...")
	generaterelayscript(mem)
	log("Generating call wrapper ...")
	generatejoblistcallwrapper(out_dir, "bash {0} {1}{2}".format(relayscriptname, "mono " if mono else "", os.path.normpath(exe)))
	# Submit to ccs
	log("Submitting ...")
	call = "ccsalloc -J 0-{0}:{1} {2} {1}".format(joblistcount - 1, batchsize, ccswrapperfilename)
	log(call)
	os.system(call)
# --> Check whether it's TORQUE mode
if torqueMode:
	# Copy resource dir content to working dir
	log("Copying resources to current directory ...")
	distutils.dir_util.copy_tree(resourcedir, "./Resources")
	# Prepare torque mode
	log("Preparing files for torque ...")
	log("Saving jobs to file ...")
	joblistcount = savejoblist(instanceConfigSeedListing)
	log("Generating torque relay script ...")
	generatetorquewrapperscript(torqueworkingdir, logdir, cpu_time_algorithm * batchsize, torquequeue, ncpus, mem)
	log("Generating memory limiting relay script ...")
	generaterelayscript(mem)
	log("Generating call wrapper ...")
	generatejoblistcallwrapper(out_dir, "bash {0} {1}{2}".format(relayscriptname, "mono " if mono else "", os.path.normpath(exe)))
	# Submit to torque
	log("Submitting ...")
	call = ""
	if batchsize == 1:
		call = "qsub -t 0-{0} {1}".format(joblistcount - 1, torquewrapperfilename)
	else:
		call = "qsub -t {0} {1} -F \"{2}\"".format(",".join([str(j) for j in xrange(0, joblistcount - 1, batchsize)]), torquewrapperfilename, batchsize)
	log(call)
	os.system(call)
# --> Check whether it's SERVER mode
if hostServer:
	# Prepare list of idents for the jobs
	identgeneration = set()
	for i in xrange(0, len(instanceConfigSeedListing)):
		identgeneration.add(generateident(identgeneration))
	idents = list(identgeneration)
	# Prepare list of jobs - remove unnecessary path info alongside
	jobidentstodo = {}
	jobidentsinprogress = {}
	requestidents = set()
	requstidentsperjob = {}
	requeststarttimes = {}
	jobidentsdone = {}
	jobexecutiontimes = []
	for i in xrange(0, len(instanceConfigSeedListing)):
		jobidentstodo[idents[i]] = (os.path.basename(instanceConfigSeedListing[i][0]), os.path.basename(instanceConfigSeedListing[i][1]), os.path.basename(instanceConfigSeedListing[i][2]), instanceConfigSeedListing[i][3])
	log("Generated job idents: {0}".format(",".join(str(e) for e in idents)))
	log("Generated {0} job idents".format(len(jobidentstodo)))
	# Start thread managing the timeouts
	timeoutmanager = Thread(target=timeoutmonitor, args=((requeststarttimes, requstidentsperjob, jobidentsinprogress, jobidentstodo, jobidentsdone), ))
	timeoutmanager.start()
	# Host the server
	s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
	host = ""
	s.bind((host, port))
	s.listen(5)
	log("Waiting for clients ...")
	# Keep responding to worker bees as long as there are jobs todo or running
	while len(jobidentstodo) != 0 or len(jobidentsinprogress) != 0:
		# Accept the request and analyze the message
		c, addr = s.accept()
		rawmessage = c.recv(1024)
		log("rcvd: {0}".format(rawmessage))
		message = rawmessage.split(socketcommunicationdelimiter)
		# Synchronize before changing the lists
		lock.acquire()
		# Respond to message
		if message[0] == requestjobsignal:
			if len(jobidentstodo) > 0:
				# We have more jobs to do - send the next job
				log("Worker bee at {0} is requesting a new job ...".format(addr))
				jobident = random.choice(jobidentstodo.keys())
				requestident = generateident(requestidents)
				requestidents.add(requestident)
				if jobident not in requstidentsperjob:
					requstidentsperjob[jobident] = requestident
				jobdescription = socketcommunicationdelimiter.join(str(e) for e in (executesignal,requestident,jobident,jobidentstodo[jobident][0],jobidentstodo[jobident][1],jobidentstodo[jobident][2],jobidentstodo[jobident][3]))
				jobidentsinprogress[jobident] = jobidentstodo.pop(jobident)
				requeststarttimes[requestident] = datetime.datetime.now()
				log("Sending job: {0}".format(jobdescription))
				c.send(jobdescription)
				c.close()
			else:
				# We are currently out of jobs - send the sleep command
				log("No more jobs to send - setting worker bee at {0} to sleep ...".format(addr))
				c.send(sleepmodesignal)
				c.close()
		else:
			if message[0] == submitfinishedsignal:
				if int(message[2]) in jobidentsinprogress:
					# Worker finished a job - keep the todo lists up to date
					log("Finished job with ID: {0}".format(message[2]))
					jobidentsdone[int(message[2])] = jobidentsinprogress.pop(int(message[2]))
					jobstarttime = requeststarttimes.pop(int(message[1]))
					jobexecutiontimes.append(datetime.datetime.now() - jobstarttime)
				else:
					# Worker returned after being considered as timed out - update the timeout value and cleanup
					log("Finished job with following ID after timeout: {0}".format(message[2]))
					if int(message[2]) in jobidentstodo:
						jobidentsdone[int(message[2])] = jobidentstodo.pop(int(message[2]))
					jobstarttime = requeststarttimes.pop(int(message[1]))
					newtimeout = datetime.datetime.now() - jobstarttime
					jobexecutiontimes.append(newtimeout)
					# Update the timeout if necessary
					if newtimeout > runtimeout:
						log("Set new timeout to {0} (was {1})".format(newtimeout, runtimeout))
						runtimeout = newtimeout
					if int(message[1]) == requstidentsperjob[int(message[2])]:
						# We seem to have submitted a job twice due to a timeout
						log("Warning! Following job was executed more than once: {0}".format(jobidentsdone[int(message[2])]))
			else:
				# Unknown signal
				log("Warning! Unknown message received: {0}".format(rawmessage))
		# Log some info
		log("Job status: {0}/{1}/{2} (done/inprogress/todo) todo-ETA (sum): {3} todo-ETA (real): {4}".format(len(jobidentsdone), len(jobidentsinprogress), len(jobidentstodo), "n/a" if len(jobexecutiontimes) == 0 else "{0}".format(sum(jobexecutiontimes, datetime.timedelta())/len(jobexecutiontimes)*len(jobidentstodo)), "n/a" if len(jobidentsdone) == 0 else "{0}".format((datetime.datetime.now() - experimentstarttime) / len(jobidentsdone) * len(jobidentstodo))))
		# Release the lock after changing the lists
		lock.release()
	# Terminate and wait for threads
	s.close()
	log("Waiting for threads to terminate ...")
	exitrequested = True
	timeoutmanager.join()

# Finish
log(".Fin.")

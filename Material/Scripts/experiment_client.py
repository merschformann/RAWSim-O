#!/usr/bin/python

import datetime
import distutils.dir_util
import os.path
import random
import socket
import subprocess
import sys
import threading
from threading import Thread
import thread
import time

if len(sys.argv) != 8:
    print("Usage: ./experiment_client.py <serverIP> <monoORnet> <exe> <instance+setting+config dir> <repo dir> <output dir> <threadCount>")
    sys.exit(1)

# --> Logging
global logfilename
logfilename = "experimentclient.log"
if os.path.isfile(logfilename):
	os.remove(logfilename)
def log(message):
	print "{0}: {1}".format(datetime.datetime.now().strftime("%Y-%m-%d %H:%M:%S"), message)
	with open(logfilename, "a") as logfile:
		logfile.write("{0}\n".format(message))

# Set some constant values
requestjobsignal = "1"
submitfinishedsignal = "2"
sleepmodesignal = "3"
executesignal = "4"
socketcommunicationdelimiter = ';'
port = 31353
# Check operating system
islinux = sys.platform == "linux" or sys.platform == "linux2"
# Initialize connection
host = sys.argv[1]
lock = thread.allocate_lock()
# Initialize parameters
mono = sys.argv[2] == "mono" # specifies whether mono is used for calling the exe
exe = sys.argv[3] # the executable
instanceconfigdir = sys.argv[4] # the directory containing all necessary instances, settings and configurations
repopath = os.path.normpath(sys.argv[5]) # the repository path
hgscript = "pullupdate.cmd" # the script updating the repository right before starting
buildscript = "rebuild.cmd" # the script rebuilding the sources right before starting
resourcedir = os.path.join(repopath, "Material", "Resources") # the directory containing additional resources which is copied to the working dir before requesting jobs
outputdirectory = sys.argv[6] # the output directory
threadcount = int(sys.argv[7]) # the number of parallel workers managed by this client

# Job execution - define the worker thread main method
def workerthreadcall(state):
	# Remember thread ID
	workerid = state
	# Fetch and do jobs until no further are available
	while True:
		# Get another job
		lock.acquire()
		log("worker{0}: Contacting server at {1}:{2} for another job ...".format(workerid, host, port))
		sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
		try:
			sock.connect((host, port))
		except:
			log("worker{0}: We seem to be done here - server seems to be offline".format(workerid))
			sock.close()
			lock.release()
			break
		sock.send(str(requestjobsignal))
		rawmessage = sock.recv(1024)
		log("rcvd: {0}".format(rawmessage))
		sock.close()
		lock.release()
		# Prepare
		message = rawmessage.split(socketcommunicationdelimiter)
		# Analyze message
		if message[0] == sleepmodesignal:
			# Currently no more jobs to do - sleep and then ask again
			log("worker{0}: Out of jobs - sleep for a while and try again ...".format(workerid))
			time.sleep(120)
		else:
			if message[0] == executesignal:
				# Execute the next job
				log("worker{0}: Received job: {1}".format(workerid, message))
				instancepath = os.path.join(instanceconfigdir, os.path.normpath(message[3]))
				settingpath = os.path.join(instanceconfigdir, os.path.normpath(message[4]))
				configpath = os.path.join(instanceconfigdir, os.path.normpath(message[5]))
				out_dir = os.path.join(outputdirectory, "worker{0}".format(workerid))
				seed = message[6]
				call = "{0}{1} {2} {3} {4} {5} {6}".format("mono " if mono else "", os.path.normpath(exe), os.path.normpath(instancepath), os.path.normpath(settingpath), os.path.normpath(configpath), os.path.normpath(out_dir), seed)
				process = subprocess.Popen(call, creationflags=subprocess.CREATE_NEW_CONSOLE)
				log("worker{0}: Started job: {1}".format(workerid, call))
				process.wait()
				# Submit result
				lock.acquire()
				log("worker{0}: Signaling job finished to server at {1}:{2} ...".format(workerid, host, port))
				sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
				try:
					sock.connect((host, port))
				except:
					log("worker{0}: Server unreachable for result submission - discarding it ...".format(workerid))
					sock.close()
					lock.release()
					continue
				sock.send(socketcommunicationdelimiter.join((str(submitfinishedsignal), message[1], message[2])))
				sock.close()
				lock.release()
			else:
				# Unknown signal
				log("worker{0}: Unknown message received: {1}".format(workerid, rawmessage))
		# Wait a bit before annoying the server again
		time.sleep(5)

# Copy resource dir content to working dir
log("Copying resources to current directory ...")
distutils.dir_util.copy_tree(resourcedir, ".")

# Update and rebuild sources
originalworkdir = os.getcwd()
os.chdir(repopath)
hgprocess = subprocess.call(hgscript, shell=True)
rebuildprocess = subprocess.call(buildscript, shell=True)
os.chdir(originalworkdir)

# Start worker threads
workerthreads = []
for i in range(0, threadcount):
	log("Starting worker thread: {0}".format(i))
	workerthread = Thread(target=workerthreadcall, args=(i, ))
	workerthreads.append(workerthread)
	workerthread.start()
	time.sleep(5)

# Wait for all the workers
log("Waiting for workers to return ...")
for i in range(0, threadcount):
	workerthreads[i].join()

# Finish
log(".Fin.")
#!/usr/bin/env python

import os
from os import listdir
from os.path import isfile, join
import sys

if len(sys.argv) != 3:
    print("Usage: ./validate_subjobs.py <jobfile> <tagfile>")
    sys.exit(1)

# Init
startedjobs = set()
startedjobdescriptions = dict()
finishedjobs = set()

# Scan job file
print("Scanning job file: {0}".format(sys.argv[1]))
with open(sys.argv[1], "r") as ins:
	for line in ins:
		elements = line.split(";")
		startedjobs.add(int(elements[0]))
		startedjobdescriptions[int(elements[0])] = line.strip()
print("Found {0} jobs!".format(len(startedjobs)))

# Scan tag file
print("Scanning tag file: {0}".format(sys.argv[2]))
with open(sys.argv[2], "r") as ins:
	for line in ins:
		if not line.startswith("Tag"):
			elements = line.split(";")
			if int(elements[0]) in finishedjobs:
				print("WARNING: found duplicate jobID {0} in tag-file - did you clear the tag file before starting the experiment? you may want to check the timestamps in the tag-file.".format(elements[0]))
			finishedjobs.add(int(elements[0]))

# Find incomplete jobs
incompletejobs = startedjobs.difference(finishedjobs)
print("Found {0} incomplete jobs!".format(len(incompletejobs)))
if len(incompletejobs) > 0:
	print("Job IDs:")
	print(",".join([str(e) for e in incompletejobs]))
	print("Listing incomplete jobs:")
	for jobID in incompletejobs:
		print(startedjobdescriptions[jobID])

# Finish
print(".Fin.")

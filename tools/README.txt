----------
EMERGETOOL
----------

emergetool is a command line program featuring various tools for the analysis
of EmergeTk projects.

-------------------
Building emergetool
-------------------

To build emergetool, simply open emergetool.csproj in Visual Studio or Monodevelop,
make sure all the references are set up correctly, and build.

------------------
Running emergetool
------------------

To run emergetool, navigate to where emergetool.exe exists (most likely the bin 
directory) and run

./emergetool.exe <tool> <args>

where <tool> is the tool you wish to use and <args> is the list of arguments you
wish to pass to the tool. For more information about using emergetool, run 

./emergetool.exe help

For information about a specific tool, run

./emergetool.exe help <tool>

For information about EmergeTk, visit

http://www.emergetk.com

---------------------------
Currently implemented tools
---------------------------

synch 
-----
The synch tool analyzes a project's database for synchronization with the
project's model and prints SQL alter statements designed to fix any problems.

help
----
The help tool prints usage for this program and its tools.

-------------
Adding a tool
-------------

If you wish to add a tool, please follow these steps:

1. make a tool class that implements the IEmergeTool interface
2. In EmergeTools.cs, add the tool's name to the switch block in getToolFromName
3. In EmergeTools.cs, add the tool's name to the availableTools array
4. Add a description of the tool in this README

--------------------
Questions, comments? 
--------------------

Email me at jeff@skullsquad.com


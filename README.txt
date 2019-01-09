Thea 2 compiler operation, V1.0.0.1
https://github.com/Aqarius90/Thea2ModCompiler

***Starting assumptions***
-/StreamingAssets folder (henceforth "/root") contains the default game xml. indices(terrains.xml, database.xml, and eventModules.xml), and the folders DataBase, Modules, and Terrain Sources, that the indices point to.
-root also contains mod folders, the names of which all start with "@" (eg. /@RPMod)
-Each mod folder contains indices pointing to the mod files, in their respective folders (eg, /@RPMod/database.xml, pointing to /@RPMod/DataBase/moddeddbfile.xml), as listed in compiler on startup.


***Operation***
-On launch, the program asks to be pointed to /Streaming assers/database.xml, in order to find the game files.
-On "load", it trawls through /StreamingAssets, looking for mod folders
-On "Compile, the program starts reading:
	**Game files**
	-It looks in root for xml indices, and loads them into memory (if not found, the program exits, as there is something wrong with the game).
	-Then, it reads through the indices, and loads into memory the files listed in them.
	-Then, it creates /StreamingAssets/Backup, and writes the files in it's memory into it. *If /Backup exists, the files are assumed already modded, and the backup is not created*
	
	**Mod files**
	-Program loops through the mod folders as if they were root, except in this case the files are optional (eg. you don't need to have eventModules.xml if you don't have /Modules).
	-Then, it compares the default indices with the mod indices:
		-New files in mod indices are straight loaded together with the default files, and the ref. to them added to the relevant index file.
		-Modified default files are loaded, compared, and (hopefully) merged into the default files (see below)
	-Process is repeated for next mod folder
-When merging files, the first step is overwriting the old header(The "<?xml" part). Some files don't have it. This is intentional, and OK.
-The next step is to take the two root nodes, and merge them:
	-All attributes in the new root node are assigned to the old node. Existing ones are overwritten
	-if new root node is tagged with a "MOD_PARAM" attribute, it does as told (see Options)
	-else, if inferFromProto setting is true, it tries to guess the preffered action from reading proto files (see Options)
	-else, it resorts to the action set as default.
	
***Merge actions***
-There are three possible merge actions: Overwrite, Add, and Merge.
	-Overwrite discards the old node, and replaces it with the new one:
		<a>
			<b/>
			<c>
				<d/>
			</c>
		</a>
		+
		<a>
			<c>
				<f/>
			</c>
			<e/>
		</a>
		=
		<a>
			<c>
				<f/>
			</c>
			<e/>
		</a>,
		This is useful if you intend to completely replace a set of data, eg. changing the research points progress.
	-Add adds the new node to the old one:
		<a>
			<b/>
			<c>
				<d/>
			</c>
		</a>
		+
		<a>
			<c>
				<f/>
			</c>
			<e/>
		</a>
		=
		<a>
			<b/>
			<c>
				<d/>
			</c>
			<c>
				<f/>
			</c>
			<e/>
		</a>,
		This is useful for adding to a list, eg. raising the research point limit, or adding more names to a race. *Will create duplicates*.
	-Merge 
		<a>
			<b/>
			<c>
				<d/>
			</c>
		</a>
		+
		<a>
			<c>
				<f/>
			</c>
			<e/>
		</a>
		=
		<a>
			<b/>
			<c>
				<d/>
				<f/>
			</c>
			<e/>
		</a>,
		Attempts to merge the old and new:
			-For each child of new root, try to find a *unique* match child in old root. Search for a match starts with node name. If there is more than one match, narrow search by parameter. Eg.:
				-<LOC_LIBRARY-EN_UI> is a unique match.
				-<Entry> is a match, but not unique. <Entry Key="QUOTE_MARKS_EXAMPLE">, however, is unique.
			-If no match is found, assume it's a new node, and *add* it to old root.
			-If multiple matches are found, assume it's an array (eg, names), and *add* it to old root (but log the addition in debug output)
			-If one match is found, the node pair is merged, as if file root (ie. the root node process is repeated for all child nodes that are **MERGE**d).

***Options***
-Parameter parsing (lax/strict):
	-A MOD_PARAM is an xml attribute that tells the compiler what to do with a node (eg. <EV_EXP-BASE MOD_PARAM="OVERWRITE">). Valid params are: OVERWRITE, MERGE, and ADD.
	-Strict parsing errors out if no unique match can be found for a MERGE param node.
	/*note to self - revise this*/
-Infer from prototype (y/n):
	-/DataBase contains /Proto, which defines "classes" used in /DataBase. The default game file scan looks for "object properties" tagged as "array", and notes them.
	-If proto inferrence is "yes", nodes that are "probably" earmarked as "arrays" are merged as  **ADD**. This is probably broken and does not work properly. Use as last resort.
-Default to (add/overwrite/merge)
	-Default action on a node not caught by any of the above.
		-Overwrite forces an overwrite.
		-Add forces an add.
		-Merge tries to find a unique match to merge, otherwise defaults to add.
# AOE3-DE Automatic Packaging Tool
A tool program that automatically converts the XML source code of AOE3-DE game data into XMB, and packages it into a BAR file.

## Instructions for Use
- Requires two directories: source and data. The source directory contains XML source code, while the data directory contains XMB files and other files extracted from the BAR.
- The data directory is obtained directly by unpacking the BAR file.
The source directory is obtained by decoding XMB files, and the paths of each XML file must match those of the XMB.
- After modifying files in the source directory, running the tool program will encode XML files into XMB, replace corresponding files in the data directory according to the directory structure, and finally package the data directory. It generates a BAR file named Data_generated.bar in the same directory.

## Program Parameters
- source - Source directory, default value: ./xml_data_source
- data - Data directory, default value: ./Data
- suffix - Suffix of BAR file, default value: generated

## Usage Example
### Run exe directly
- Assuming the data directory is G:\AOE3\Data and the source directory is G:\AOE3\xml_data_source
- Run the program via command line: .\aoe3-auto-packager.exe source=G:\AOE3\xml_data_source data=G:\AOE3\Data
- The resulting file will be G:\AOE3\Data_generated.bar

### Work with git hook
- Assuming a git repository exists in source directory
- Assuming the executable file exists in F:/Projects/sharp/aoe3-auto-packager/bin/Debug/net8.0/
- Assuming the data directory is G:/AOE3/Data
- Enable git hook named post-commit
```
#!/bin/sh
HEAD_BRANCH=`head -n 1 '.git/HEAD' | cut -d " " -f 2`
HEAD_COMMIT=`head -n 1 ".git/$HEAD_BRANCH"`
F:/Projects/sharp/aoe3-auto-packager/bin/Debug/net8.0/aoe3-auto-packager.exe source=. data=G:/AOE3/Data suffix=$HEAD_COMMIT
```
- After committed, bar file named Data_<commit_hash>.bar will be automatically created in G:/AOE3

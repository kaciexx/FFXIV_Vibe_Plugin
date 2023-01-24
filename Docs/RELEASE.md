# Internal release process reminder
1. Bump the C# assembly project version (double click on FFXIV\_Vibe\_Plugin in Visual Studio).
2. Build in release mode (ignore the error saying that the manifest file could not be found)
3. Go to the FFXIV\_Vibe\_Plugin\bin\x64\Release folder and **again** in FFXIV\_Vibe\_Plugin folder.
4. Rename the ZIP to the corresponding version (eg: FFXIV\_Vibe\_Plugin\_v1.4.0.0.zip)
5. Update the repo.json version, timestamp, downloadCount and download links. 
6. Commit your changes and push.
7. Tag the project version (eg: `git tag v1.4.0.0`) and push the tag.
8. Create a release on github, upload the zip and publish
9. Remove the FFXIV\_Vibe\_Plugin in devPlugins/FFXIV\_Vibe\_Plugin


# Conventions

Version: 1.8.0.0 refers to Dalamud API 8

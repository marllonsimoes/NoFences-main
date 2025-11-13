All Fences:

* ✅ DONE: The help icon is still visible after fence fades out (Session 11 - Fixed with Visibility property instead of ForeColor alpha)



ClockFence:

* ✅ DONE: Add options to show/hide elements (Session 11 - Added 16 customization properties including ShowLocation, TimeFormat, ShowSeconds, ShowDate, ShowFeelsLike, ShowHumidity, ShowClouds, ShowSunrise, ShowSunset, ShowWind. UI fully connected with persistent settings)
* ✅ DONE: Font customization (Session 11 - Added TimeFontSize (24-96px), DateFontSize (10-48px), WeatherFontSize (10-48px) with slider controls and live preview)
* ✅ DONE: Show location name (Session 11 - Added ShowLocation property and display)
* FUTURE: Advanced layouts - Vertical layout (all elements stacked), Pixel Phone layout (big weather icon, hour/minute/second on separate lines). UI prepared with ClockLayout property, implementation marked as future enhancement



FilesFence:

* ✅ DONE: Get information from detectors with useful metadata (Session 11 - Now uses InstalledSoftware model with full metadata from Steam, GOG, Epic, Amazon Games, etc. Icon extraction prioritizes detector-provided IconPath. Icon caching provides 50-100x performance improvement. Platform badges show source)
* Rework datalayer:

 	- We can have just one table with games and software, identifying them with better categorization. We can categorize what's installed based on the source of it. Let's discuss this point

 	- Create database locally only - no source of truth for now. We will introduce a sync service that will send the collected data (software installed) to a web service later.

 	- Get software categorization from reliable sources adhoc like cnet, winget, Wikipedia. If the source is steam (or any gamming platform), most likely will be a game, and we can get data from rawg. Will also be synchronized with a webservice later. Use the same table for games and software with the columns: id, name, manufacture(publisher/developer), type, categories(labels for games), \*audit info

 	- In the Filter Type, we can introduce the "game" type that will work the same way as the software type, however, the subfilter "category" will list only game categories, as well as the software will show only software categories.

* This is a question: UniGetUI - a visual client for winget - can list all software and games installed in the computer, showing the sources from "other stores" like Steam, UbiConnect, etc. Can we have the same feature here? I've tried to identify what they do and it seems they use winget to list the installed software, but they also identify the source somehow. That would help collect information about the software





AmazonGamesDetector

* ✅ DONE: Use SQLite databases from Data/Games/Sql folder (Session 11 - Implemented AmazonGamesRepository with repository pattern. Queries GameInstallInfo.sqlite for installation data and ProductDetails.sqlite for rich metadata. Includes schema introspection, proper ProductTitle extraction, and three-tier fallback logic. Extracts Developers, Publisher, Genres, GameModes, Description, Screenshots, etc. from ProductDetails JSON)







Installer

* ✅ DONE: Finish auto download and silent install (Session 11 - LaunchInstaller now supports /SILENT flag)
* ✅ DONE: Fix UpdateManager package selection - now prefers bootstrapper over MSI (Session 11 - smart asset selection with priority order)
* Review installer so new version installation is successful. Seems after reinstalling or installing a new version, the installer fails due to the service not being removed or can't be replace. Maybe add validations to it and force service remove/re-install?



Services

* The cloud sync must be a 3-way backup system, in the following sense:

 	- The user has a cloud storage that shows many files in a private cloud (we are using seafile for general use, but there is also other apps that the user has data such as immich, and other media providers with profiles). THe ideia is to show the user everything that he has access to from the cloud perspective.

 	- The user can create backup rules, or copy rules, selecting a source and a targets (multi target to keep redundant) and informs if it's birectional (synced folder/driver). At this moment we identify if source/target is a removable drive and store it in the local database for further checks, along with the source and other targets. We will need to trigger the backup once the drive is connected. The service then starts the directional backup, using the CloudSync windows api (https://learn.microsoft.com/en-us/windows/win32/cfapi/build-a-cloud-file-sync-engine). We might need an audit or history table to know what was backedup as well as identify files that were moved around in another computer (using hash to identify the file). I believe some other metadata will also be needed, as the user can edit the file AND move it around, making it hard to identify if it was moved, deleted, renamed...

 	- The user might select what will be synced (always available, free-up space etc...)

 	- All of it must be shown to the user in a nice status window with details of what was synced, what is being synced at this moment, and what couldn't be for any reason (giving options to solve the possible conflict or error).

 	- This is where our first widget fence will be created: to show the "virtual" drive of the user. We will also need to introduce a login window where he can login to the private cloud, and then see its files.

 		- the WidgetFence will show the VirtualFolders the user have

 		- Another WidgetFence will give stats from the cloud drive (available storage, file count, folder count, files synced etc)

 		- Yet another WidgetFence might show a specific Virtual Folder (Media, Photos, Books, Documents, Music, User - with placeholders for files not synced)

 		- The user must be able to see the same Virtual Drive on windows explorer, and the folders listed there might have different permissions for actions, like some of them might not allow create sub folders. Or won't be able to upload files, only donwload. The service that provides the storage information might give everything we need - just let me know the requirements for this (as far as I can tell, it's an API that returns the data needed - I can easily implement it).

 


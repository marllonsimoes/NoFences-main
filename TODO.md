General

* Introduce the possibility to see logs and change its level from INFO to DEBUG at least. For now, it can be in the trayicon context menu.



EditWindow

* Because of the theme used, the usability is not good. It's hard to find where things are due to the bright blue color. Please make it more accessible.



ClockFence:

* Add options in edit window to show more weather information such as feels\_like, sunset and sunrise, clouds, wind speed in km/h and direction. Use nice icons for the weather, temperature and wind direction, not only symbols.



FilesFence:

* Allow the user select the files it wants to see inside the fence (dropping the files is better - but works selecting as well)
* Rework datalayer:

 	- We can have just one table with games and software, identifying them with better categorization. We can categorize what's installed based on the source of it. Let's discuss this point

 	- Create database locally only - no source of truth for now. We will introduce a sync service that will send the collected data (software installed) to a web service later.

 	- Get software categorization from reliable sources adhoc like cnet, winget, Wikipedia. If the source is steam (or any gamming platform), most likely will be a game, and we can get data from rawg. Will also be synchronized with a webservice later. Use the same table for games and software with the columns: id, name, manufacture(publisher/developer), type, categories(labels for games), \*audit info

 	- In the Filter Type, we can introduce the "game" type that will work the same way as the software type, however, the subfilter "category" will list only game categories, as well as the software will show only software categories.

* BUG: Position the content a little bit down, it's being drawn under the title.
* This is a question: UniGetUI - a visual client for winget - can list all software and games installed in the computer, showing the sources from "other stores" like Steam, UbiConnect, etc. Can we have the same feature here? I've tried to identify what they do and it seems they use winget to list the installed software, but they also identify the source somehow. That would help collect information about the software
* BUG: the icons drawn somethings have long names making it look weird. Introduce string pipe ellipsis ("when the string is to long, add ...")



PicturesFence

* Is there any possibility to actually play the animated gifs? They are static today



VideoFence?

* Can we have a videoFence? That plays a video or playlist in loop?s



All fences

* I really need to introduce drag'n drop and a better way to switch between fences types using the files we have in it.
* BUG: Work on the size title - we have the options to change the title size, but when displaying it, it doesn't change the height.
* If the mouse is over a fence, always show the resizing corner (all directions). Currently it only shows if starts resizing.
* We need to introduce usuability tips. It's not clear the user has options for the fences. Maybe the first fence, or when creating a new fence, add a panel with information about it, like a manual? Would be nice to have a "?" icon where the user can review what he can do.
* Customization - let's allow the user to decide wheather they want the fade effect.
* BUG: The fences can be hidden outside the window if they fade out, it shouldn't happen. We must limit the fences to be within the desktop area.



Installer

* Let's introduce a service that checks for new versions of the software and automatically downloads (optional - only if the user accepts) and starts the process of update. I believe the installer must be changed in order to have this feature, like introducing a valid uid for the software, links and urls that are relevant for the process. We might need to rewrite the LICENSE.rtf as well, as eventually we will be collecting data from the user machine (anonymously - like the software installed and it's categories).



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

 




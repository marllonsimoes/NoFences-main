All Fences:

* BUG: The help icon is still visible after fence fades out



ClockFence:

* Add options on edit windwo to show what the user wants only (feels like, sunrise/sundown, wind, date, time)
* Customize fonts or layout maybe? Like the clock on block screen for pixel smartphones (vertical date; hour, minute, second in different lines; big icon with weather, big number for temp; show location)



FilesFence:

* BUG: need to get information collected from GetAllInstalled() that returns useful metadata. Currently, we are only getting the path of the shortcut and extracting info from there, but the icon names are weird, point to the .exe file. Should get the name resolved by the detectors.
* Rework datalayer:

 	- We can have just one table with games and software, identifying them with better categorization. We can categorize what's installed based on the source of it. Let's discuss this point

 	- Create database locally only - no source of truth for now. We will introduce a sync service that will send the collected data (software installed) to a web service later.

 	- Get software categorization from reliable sources adhoc like cnet, winget, Wikipedia. If the source is steam (or any gamming platform), most likely will be a game, and we can get data from rawg. Will also be synchronized with a webservice later. Use the same table for games and software with the columns: id, name, manufacture(publisher/developer), type, categories(labels for games), \*audit info

 	- In the Filter Type, we can introduce the "game" type that will work the same way as the software type, however, the subfilter "category" will list only game categories, as well as the software will show only software categories.

* This is a question: UniGetUI - a visual client for winget - can list all software and games installed in the computer, showing the sources from "other stores" like Steam, UbiConnect, etc. Can we have the same feature here? I've tried to identify what they do and it seems they use winget to list the installed software, but they also identify the source somehow. That would help collect information about the software





AmazonGamesDetector

* I might have found a better way to list the amazon games - in the installation folder, there is a Data/Sql folder with some valuable information. It uses SQLite (we already have the driver and infrastructure to use the database in readonly mode). 
  CommonData.sqlite has the information about the libraries;
  GameInstallDetails has the games installation folder, last updated date, title
  ProductDetails has lots of information, like the json below:
* {
* &nbsp;   "Background": "https://m.media-amazon.com/images/I/71c+TMbLTmL.jpg",
* &nbsp;   "Background2": "https://m.media-amazon.com/images/I/71c+TMbLTmL.jpg",
* &nbsp;   "Developers": \[
* &nbsp;       "Broken Arms Games"
* &nbsp;   ],
* &nbsp;   "EsrbRating": "EVERYONE\_TEN\_PLUS",
* &nbsp;   "ExternalWebsites": {},
* &nbsp;   "GameModes": \[
* &nbsp;       "Single Player"
* &nbsp;   ],
* &nbsp;   "Genres": \[
* &nbsp;       "Simulator",
* &nbsp;       "Strategy",
* &nbsp;       "Indie"
* &nbsp;   ],
* &nbsp;   "Id": "amzn1.adg.product.c1cd3750-55cc-47d5-b137-db9a8dde1617",
* &nbsp;   "IsDescriptionRightToLeft": false,
* &nbsp;   "Keywords": \[
* &nbsp;       "farming",
* &nbsp;       "time management"
* &nbsp;   ],
* &nbsp;   "LastModifiedDateTime": "2025-11-11T18:48:12.2985633+01:00",
* &nbsp;   "LocalCacheExpirationTime": "11/11/2025 19:03:12",
* &nbsp;   "Locale": "en-US",
* &nbsp;   "OfficialWebsite": "http://www.hundreddaysgame.com",
* &nbsp;   "PGCrownImageUrl": null,
* &nbsp;   "PegiRating": "THREE",
* &nbsp;   "ProductAsin": null,
* &nbsp;   "ProductAsinVersion": null,
* &nbsp;   "ProductDescription": "It takes a hundred days for a vine leaf to complete its life cycle: from spring to autumn, the leaves thrive and provide the fundamental energy to grow the grapes. Hundred Days will put you in charge of managing a small and abandoned winery: from selecting the types of vine you want to grow, to naming your final product, every decision of the challenging business of winemaking will be in your hands.  \\n\\nTake care of your vineyard, learn to follow the rhythm of the seasons, harvest, label your bottles and sell them on the market: every choice you make will have an impact on the quality and quantity of the wine you produce and sell. Increase the reputation of your company worldwide, expand your business and manage the tight schedule of your daily tasks.",
* &nbsp;   "ProductDomain": null,
* &nbsp;   "ProductIconUrl": "https://m.media-amazon.com/images/I/81qM0C1t4+L.jpg",
* &nbsp;   "ProductId": {
* &nbsp;       "Id": "amzn1.adg.product.c1cd3750-55cc-47d5-b137-db9a8dde1617"
* &nbsp;   },
* &nbsp;   "ProductLine": "Sonic:Game",
* &nbsp;   "ProductLogoUrl": "https://m.media-amazon.com/images/I/41Rrb+pCRIL.png",
* &nbsp;   "ProductPublisher": "Broken Arms Games",
* &nbsp;   "ProductSku": "amzn1.resource.a2c4fd7d-8602-f67a-c291-93296c247074",
* &nbsp;   "ProductTitle": "Hundred Days",
* &nbsp;   "ProductVendor": "e9039554-7a9a-4ebf-8985-29aa059411bc",
* &nbsp;   "ReleaseDate": "2021-05-13T02:00:00",
* &nbsp;   "Screenshots": \[
* &nbsp;       "https://m.media-amazon.com/images/I/A1h+c02GV2L.jpg",
* &nbsp;       "https://m.media-amazon.com/images/I/91reyxfcauL.jpg",
* &nbsp;       "https://m.media-amazon.com/images/I/A1TNs9EYQ5L.jpg",
* &nbsp;       "https://m.media-amazon.com/images/I/91QjXYypfNL.jpg",
* &nbsp;       "https://m.media-amazon.com/images/I/91fWJn-PjEL.jpg"
* &nbsp;   ],
* &nbsp;   "TrailerImageUrl": null,
* &nbsp;   "UskRating": "ZERO",
* &nbsp;   "Version": null,
* &nbsp;   "Videos": \[]
* }







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

 


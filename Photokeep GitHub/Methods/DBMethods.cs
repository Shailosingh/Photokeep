using DSharpPlus.EventArgs;
using DSharpPlus.Entities;
using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace PhotoKeep.Methods
{
    class DBMethods
    {
        //Databse Links and Tokens
        private const string COSMOS_URL = "Insert your own CosmosDB URL";
        private const string COSMOS_KEY = "Insert your own CosmosDB keys";

        //Database Name Constants (These can be changed depending on what you want)
        private const string DATABASE_NAME = "PhotokeepDB";
        private const string USERS_CONTAINER = "Users";
        private const string FOLDERS_CONTAINER = "Folders";

        //User dictionaries for fast access
        private static Dictionary<string, User> userDictionary = new Dictionary<string, User>();

        /// <summary>
        /// Called at beginning of runtime to initialize database, 
        /// pull ALL user info from database and place in the userDictionary
        /// </summary>
        /// <returns></returns>
        public static async Task GetAllUsers()
        {
            using (CosmosClient client = new CosmosClient(COSMOS_URL, COSMOS_KEY))
            {
                //Create database if not already there
                Database database = await client.CreateDatabaseIfNotExistsAsync(DATABASE_NAME);
                Console.WriteLine("Database built!");

                //Create user container if it does not exist
                Container userContainer = await database.CreateContainerIfNotExistsAsync(USERS_CONTAINER, "/id", 400);
                Console.WriteLine("User container inserted!");

                //Create folders container if it does not exist
                Container folderContainer = await database.CreateContainerIfNotExistsAsync(FOLDERS_CONTAINER, "/id", 400);
                Console.WriteLine("Folder container inserted!");

                //Define SQL query to retrieve user info of every user
                var findUserQuery = $"SELECT u.id, u.secretID, u.name, u.photoCount, u.folderCount, u.folders, u.folderSizeDictionary FROM {USERS_CONTAINER} u";

                //Create actual query 
                QueryDefinition queryDefinition = new QueryDefinition(findUserQuery);
                Console.WriteLine("Query defined");

                //Cycle through query, get each user and place in user dictionary
                using(FeedIterator<User> feed = userContainer.GetItemQueryIterator<User>(queryDefinition))
                {
                    while(feed.HasMoreResults)
                    {
                        FeedResponse<User> response = await feed.ReadNextAsync();
                        foreach (User currentUser in response)
                        {
                            //Print to console (Debugging reasons)
                            Console.WriteLine();
                            Console.WriteLine(currentUser);
                            Console.WriteLine();

                            //Put in dictionary
                            userDictionary.Add(currentUser.id, currentUser);
                        }
                    }
                }

                //All users loaded
                Console.WriteLine("All users stored in dictionary for use.");
                Console.WriteLine($"Number of users: {userDictionary.Count}\n");
            }
        }

        /// <summary>
        /// Checks if user is already in database and if not, insert them in
        /// </summary>
        /// <param name="e"> Message Variable </param>
        public static async Task CreateAccount(MessageCreateEventArgs e)
        {
            //Check if an account is not recorded in the server
            if(!userDictionary.ContainsKey(e.Author.Id.ToString()))
            {
                //Add to dictionary
                User newUser = new User(e.Author.Id, e.Author.Username);
                userDictionary.Add(newUser.id, newUser);
                Console.WriteLine("Added to dictionary");

                using (CosmosClient client = new CosmosClient(COSMOS_URL, COSMOS_KEY))
                {
                    //Open database
                    Database database = client.GetDatabase(DATABASE_NAME);
                    Console.WriteLine("Database opened!");

                    //Open users container
                    Container userContainer = database.GetContainer(USERS_CONTAINER);
                    Console.WriteLine("Users container opened!");

                    //Create new user object (OBJECTS CANNOT HAVE NUMERICS)
                    Console.WriteLine(newUser.id);
                    Console.WriteLine(newUser.name);
                    await userContainer.CreateItemAsync(newUser);
                    Console.WriteLine($"{newUser.id} Added to database");

                    //DM User first message 
                    string welcomeMessage = "Thank you for using PhotoKeep. Here are some tips and tricks to make your experience better\n" +
                                            "1. Use the **///help** command to see a list of commands.\n" +
                                            "2. Upload your photos to the bot in a channel where you know they won't be deleted. Preferably in this DM chat. You can send the photos anywhere though obviously.\n" +
                                            "3. Visit our companion website so you can view your photos and folders in your browser.\n" +
                                            "https://photokeepwebsite.azurewebsites.net/ \n" +
                                            "4. Have fun! :blush:";

                    //If user made the message in a guild
                    if(e.Guild != null)
                    {
                        DiscordMember discordUser = await e.Guild.GetMemberAsync(e.Author.Id);
                        await discordUser.SendMessageAsync(welcomeMessage);
                    }

                    //If the user DM's bot
                    else
                    {
                        await e.Message.RespondAsync(welcomeMessage);
                    }
                }
            }

            else
            {
                Console.WriteLine("User already in database");
            }

            Console.WriteLine($"Number of users: {userDictionary.Count}\n");
        }

        /// <summary>
        /// Takes valid folder name and creates folder in database for specified user
        /// </summary>
        /// <param name="e">Message info</param>
        /// <param name="folderName">Name of folder to be inserted</param>
        public static async Task CreateFolderInDB(MessageCreateEventArgs e, string folderName)
        {
            //Attempt to put in user's object folder list. If successful, insert into database
            if(userDictionary[e.Author.Id.ToString()].AddFolder(folderName))
            {
                using (CosmosClient client = new CosmosClient(COSMOS_URL, COSMOS_KEY))
                {
                    //Open database
                    Database database = client.GetDatabase(DATABASE_NAME);
                    Console.WriteLine("Database opened!");

                    //Open users container
                    Container userContainer = database.GetContainer(USERS_CONTAINER);
                    Console.WriteLine("Users container opened!");

                    //Open folders container
                    Container folderContainer = database.GetContainer(FOLDERS_CONTAINER);
                    Console.WriteLine("Folders container opened!");

                    //Update current user document
                    await userContainer.ReplaceItemAsync(userDictionary[e.Author.Id.ToString()], e.Author.Id.ToString(), new PartitionKey(e.Author.Id.ToString()));
                    Console.WriteLine("Document updated, folder added to user log\n");

                    //Add new folder to folder container
                    Folder newFolder = new Folder(folderName, e.Author.Id.ToString());
                    await folderContainer.CreateItemAsync(newFolder);

                    //Inform user on successful update
                    await e.Message.RespondAsync($"Folder **{folderName}** added!");

                    //Show console updated user
                    Console.WriteLine(userDictionary[e.Author.Id.ToString()]);
                    Console.WriteLine();
                }

            }

            //There are too many folders
            else if(userDictionary[e.Author.Id.ToString()].UserIsFull())
            {
                await e.Message.RespondAsync("**You are at max folder capacity**");
            }

            //When folder already exists in user account
            else
            {
                await e.Message.RespondAsync("**You already made that folder!**");
            }
            
        }
        
        /// <summary>
        /// Lists all folders for user, in groups of 9
        /// </summary>
        /// <param name="e"></param>
        public static async Task ListAllFolders(MessageCreateEventArgs e)
        {
            //Ensure that there are a non zero number of folders
            if(int.Parse(userDictionary[e.Author.Id.ToString()].folderCount) != 0)
            {
                string allFolders;
                int numOfFoldersInString = 0;
                StringBuilder allFoldersSb = new StringBuilder();
                foreach (string folder in userDictionary[e.Author.Id.ToString()].folders)
                {
                    allFoldersSb.Append($"- {folder} ({userDictionary[e.Author.Id.ToString()].folderSizeDictionary[folder]})\n");
                    numOfFoldersInString++;

                    //Check if there are 9 folders in the string yet and output if so
                    if(numOfFoldersInString == 9)
                    {
                        //Print names
                        allFolders = allFoldersSb.ToString();
                        await e.Message.RespondAsync(allFolders);

                        //Reset
                        allFoldersSb = new StringBuilder();
                        numOfFoldersInString = 0;
                    }
                }

                //Ensure the string is nonempty
                if(allFoldersSb.Length > 0)
                {
                    //Convert string builder to string
                    allFolders = allFoldersSb.ToString();

                    //Print names
                    await e.Message.RespondAsync(allFolders);
                }
            }

            //If folder is empty
            else
            {
                await e.Message.RespondAsync("**NO FOLDERS**");
            }

        }

        /// <summary>
        /// Deletes specified folder from database
        /// </summary>
        /// <param name="e">Message variable</param>
        /// <param name="folderName">Name of folder</param>
        public static async Task DeleteFolderInDB(MessageCreateEventArgs e, string folderName)
        {
            //Attempt to delete folder from folder list, if succssful remove from database
            if(userDictionary[e.Author.Id.ToString()].DeleteFolder(folderName))
            {
                using (CosmosClient client = new CosmosClient(COSMOS_URL, COSMOS_KEY))
                {
                    //Open database
                    Database database = client.GetDatabase(DATABASE_NAME);
                    Console.WriteLine("Database opened!");

                    //Open users container
                    Container userContainer = database.GetContainer(USERS_CONTAINER);
                    Console.WriteLine("Users container opened!");

                    //Open folders container
                    Container folderContainer = database.GetContainer(FOLDERS_CONTAINER);
                    Console.WriteLine("Folders container opened!");

                    //Remove folder from folers container
                    Folder oldFolder = new Folder(folderName, e.Author.Id.ToString());
                    oldFolder = await folderContainer.ReadItemAsync<Folder>(oldFolder.id, new PartitionKey(oldFolder.id));
                    await folderContainer.DeleteItemAsync<Folder>(oldFolder.id, new PartitionKey(oldFolder.id));

                    //Update user's photocount
                    userDictionary[e.Author.Id.ToString()].UpdatePhotoCount(-1 * Int32.Parse(oldFolder.photoCount));

                    //Replace  user document in users container
                    await userContainer.ReplaceItemAsync(userDictionary[e.Author.Id.ToString()], e.Author.Id.ToString(), new PartitionKey(e.Author.Id.ToString()));
                    Console.WriteLine("Document updated, folder removed from user log");

                    //Inform user on successful removal
                    await e.Message.RespondAsync($"Folder **{folderName}** removed!");

                    //Show console updated user
                    Console.WriteLine(userDictionary[e.Author.Id.ToString()]);
                    Console.WriteLine();
                }
            }

            else
            {
                await e.Message.RespondAsync("**FOLDER DOES NOT EXIST**");
            }
        }

        /// <summary>
        /// Will upload the photo in the correct user and folder
        /// </summary>
        /// <param name="e">The message variable</param>
        /// <param name="photoName">The name of the photo to be uploaded</param>
        /// <param name="photoLink">The link to the photo</param>
        /// <param name="folderName">The name of the folder to be uploaded to</param>
        public static async Task UploadPhotoInDB(MessageCreateEventArgs e, string photoName, string photoLink, string folderName)
        {
            //Ensure folder is actually in user account
            if (userDictionary[e.Author.Id.ToString()].folders.Contains(folderName))
            {
                using (CosmosClient client = new CosmosClient(COSMOS_URL, COSMOS_KEY))
                {
                    //Open database
                    Database database = client.GetDatabase(DATABASE_NAME);
                    Console.WriteLine("Database opened!");

                    //Open users container
                    Container userContainer = database.GetContainer(USERS_CONTAINER);
                    Console.WriteLine("Users container opened!");

                    //Open folders container
                    Container folderContainer = database.GetContainer(FOLDERS_CONTAINER);
                    Console.WriteLine("Folders container opened!");

                    //Retrieve specified folder
                    Folder retrievedFolder = new Folder(folderName, e.Author.Id.ToString());
                    retrievedFolder = await folderContainer.ReadItemAsync<Folder>(retrievedFolder.id, new PartitionKey(retrievedFolder.id));

                    //Attempts to insert into folder object. If successful insert into database
                    if(retrievedFolder.InsertPhoto(photoName, photoLink))
                    {
                        //Increment user photo count
                        userDictionary[e.Author.Id.ToString()].UpdatePhotoCount(1);

                        //Increment the folderSize
                        userDictionary[e.Author.Id.ToString()].UpdateFolderSize(folderName, 1);

                        //Remove and replace old documents
                        Console.WriteLine("Removing and replacing the old user and folder docs");
                        await folderContainer.ReplaceItemAsync(retrievedFolder, retrievedFolder.id, new PartitionKey(retrievedFolder.id));
                        await userContainer.ReplaceItemAsync(userDictionary[e.Author.Id.ToString()], e.Author.Id.ToString(), new PartitionKey(e.Author.Id.ToString()));

                        //Prompt user that upload is successful
                        await e.Message.RespondAsync("Photo uploaded");

                    }

                    //Folder is full
                    else if(retrievedFolder.FolderIsFull())
                    {
                        await e.Message.RespondAsync("**Folder full!**");
                    }

                    //If photo is already there
                    else
                    {
                        await e.Message.RespondAsync($"Photo *{photoName}* already exists");
                        await e.Message.RespondAsync(retrievedFolder.photos[photoName]);
                    }

                }
            }

            //If folder specified don't exist
            else
            {
                await e.Message.RespondAsync($"FOLDER **{folderName}** DOES NOT EXIST");
            }
        }

        /// <summary>
        /// Attempts to delete the specified photo in the specified folder.
        /// </summary>
        /// <param name="e">Message variable</param>
        /// <param name="photoName">Name of the photo to be deleted</param>
        /// <param name="folderName">Name of the folder the photo is supposedly in</param>
        public static async Task DeletePhotoInDB(MessageCreateEventArgs e, string photoName, string folderName)
        {
            //Ensure folder is actually in user account and folder is non-empty
            if (userDictionary[e.Author.Id.ToString()].folders.Contains(folderName) && !userDictionary[e.Author.Id.ToString()].FolderIsEmpty(folderName))
            {
                using (CosmosClient client = new CosmosClient(COSMOS_URL, COSMOS_KEY))
                {
                    //Open database
                    Database database = client.GetDatabase(DATABASE_NAME);
                    Console.WriteLine("Database opened!");

                    //Open users container
                    Container userContainer = database.GetContainer(USERS_CONTAINER);
                    Console.WriteLine("Users container opened!");

                    //Open folders container
                    Container folderContainer = database.GetContainer(FOLDERS_CONTAINER);
                    Console.WriteLine("Folders container opened!");

                    //Retrieve specified folder
                    Folder retrievedFolder = new Folder(folderName, e.Author.Id.ToString());
                    retrievedFolder = await folderContainer.ReadItemAsync<Folder>(retrievedFolder.id, new PartitionKey(retrievedFolder.id));

                    //Attempts to delete photo from folder object. If successful delete from database
                    if (retrievedFolder.DeletePhoto(photoName))
                    {
                        //Decrement user photo count
                        userDictionary[e.Author.Id.ToString()].UpdatePhotoCount(-1);

                        //Decrement the folderSize
                        userDictionary[e.Author.Id.ToString()].UpdateFolderSize(folderName, -1);

                        //Remove and replace old documents
                        Console.WriteLine("Removing and replacing the old user and folder docs");
                        await folderContainer.ReplaceItemAsync(retrievedFolder, retrievedFolder.id, new PartitionKey(retrievedFolder.id));
                        await userContainer.ReplaceItemAsync(userDictionary[e.Author.Id.ToString()], e.Author.Id.ToString(), new PartitionKey(e.Author.Id.ToString()));

                        //Prompt user that deletion is successful
                        await e.Message.RespondAsync("Photo deleted");

                    }

                    //If photo is not there
                    else
                    {
                        await e.Message.RespondAsync($"Photo *{photoName}* does not exist exists");
                    }

                }
            }

            //If folder specified don't exist
            else if (!userDictionary[e.Author.Id.ToString()].folders.Contains(folderName))
            {
                await e.Message.RespondAsync($"FOLDER **{folderName}** DOES NOT EXIST");
            }

            //If the folder is empty
            else
            {
                await e.Message.RespondAsync("**FOLDER EMPTY**");
            }
        }

        /// <summary>
        /// Will randomly select one of the user's non-empty folders and return its
        /// name
        /// </summary>
        /// <param name="e">The message variable</param>
        /// <returns>Name of random selected folder. Null if all folders are empty</returns>
        public static string GetRandomFolderName(MessageCreateEventArgs e)
        {
            //Cycle through each folder and make a list of only the non empty folders
            List<string> nonEmptyFolders = new List<string>();
            int numberOfFolders = int.Parse(userDictionary[e.Author.Id.ToString()].folderCount);

            for (int index = 0; index < numberOfFolders; index++)
            {
                //Ensures the entry is non empty
                if(!userDictionary[e.Author.Id.ToString()].FolderIsEmpty(userDictionary[e.Author.Id.ToString()].folders[index]))
                {
                    nonEmptyFolders.Add(userDictionary[e.Author.Id.ToString()].folders[index]);
                }
            }

            //Check if all folders are empty
            if(nonEmptyFolders.Count == 0)
            {
                return null;
            }

            //Select random folder name from user's list of folders
            Random randomGenerator = new Random();
            int randomIndex = randomGenerator.Next(nonEmptyFolders.Count);
            string folderName = nonEmptyFolders[randomIndex];

            //Return the random folder name
            return folderName;
        }

        /// <summary>
        /// Select random photo in a specified folder
        /// </summary>
        /// <param name="e">Message variable</param>
        /// <param name="folderName">Name of the folder to be drawn from</param>
        /// <param name="isRandom">If the name of the folder was drawn at random</param>
        public static async Task RandomPhotoInFolder(MessageCreateEventArgs e, string folderName, bool isRandom)
        {
            //Ensure folder is actually in user account and folders are not empty
            if (folderName != null && (isRandom || (userDictionary[e.Author.Id.ToString()].folders.Contains(folderName) && !userDictionary[e.Author.Id.ToString()].FolderIsEmpty(folderName))))
            {
                using (CosmosClient client = new CosmosClient(COSMOS_URL, COSMOS_KEY))
                {
                    //Open database
                    Database database = client.GetDatabase(DATABASE_NAME);
                    Console.WriteLine("Database opened!");

                    //Open folders container
                    Container folderContainer = database.GetContainer(FOLDERS_CONTAINER);
                    Console.WriteLine("Folders container opened!");

                    //Retrieve specified folder
                    Folder retrievedFolder = new Folder(folderName, e.Author.Id.ToString());
                    retrievedFolder = await folderContainer.ReadItemAsync<Folder>(retrievedFolder.id, new PartitionKey(retrievedFolder.id));

                    //Get random photo from folder
                    Random rand = new Random();
                    int index = rand.Next(int.Parse(retrievedFolder.photoCount));
                    List<string> keys = new List<string>(retrievedFolder.photos.Keys);
                    string photoLink = retrievedFolder.photos[keys[index]];

                    //Give user photo
                    await e.Message.RespondAsync(photoLink);

                }
            }

            //If all folders are empty
            else if (folderName == null)
            {
                await e.Message.RespondAsync("**ALL FOLDERS EMPTY**");
            }

            //If the specified folder isn't in user accounts
            else if (!userDictionary[e.Author.Id.ToString()].folders.Contains(folderName))
            {
                await e.Message.RespondAsync("**FOLDER DOES NOT EXIST**");
            }

            //If the folder is empty
            else
            {
                await e.Message.RespondAsync("**FOLDER EMPTY**");
            }
        }

        /// <summary>
        /// Lists all photo names in specific folder
        /// </summary>
        /// <param name="e">Message variable</param>
        /// <param name="folderName">Name of folder for photos to be listed from</param>
        public static async Task ListAllPhotos(MessageCreateEventArgs e, string folderName)
        {
            //Ensure folder is actually in user account and is non-empty
            if (userDictionary[e.Author.Id.ToString()].folders.Contains(folderName) && !userDictionary[e.Author.Id.ToString()].FolderIsEmpty(folderName))
            {
                using (CosmosClient client = new CosmosClient(COSMOS_URL, COSMOS_KEY))
                {
                    //Open database
                    Database database = client.GetDatabase(DATABASE_NAME);
                    Console.WriteLine("Database opened!");

                    //Open folders container
                    Container folderContainer = database.GetContainer(FOLDERS_CONTAINER);
                    Console.WriteLine("Folders container opened!");

                    //Retrieve specified folder
                    Folder retrievedFolder = new Folder(folderName, e.Author.Id.ToString());
                    retrievedFolder = await folderContainer.ReadItemAsync<Folder>(retrievedFolder.id, new PartitionKey(retrievedFolder.id));

                    //Get name of each photo
                    string allPhotos;
                    int numOfPhotosInString = 0;
                    StringBuilder allPhotosSb = new StringBuilder();
                    foreach (string photo in retrievedFolder.photos.Keys)
                    {
                        allPhotosSb.Append($"- {photo}\n");

                        numOfPhotosInString++;

                        //Check if there are 9 photos in the string yet and output if so
                        if (numOfPhotosInString == 9)
                        {
                            //Print names
                            allPhotos = allPhotosSb.ToString();
                            await e.Message.RespondAsync(allPhotos);

                            //Reset
                            allPhotosSb = new StringBuilder();
                            numOfPhotosInString = 0;
                        }
                    }

                    //Convert string builder to string
                    allPhotos = allPhotosSb.ToString();

                    //Ensure the string is nonempty
                    if (allPhotosSb.Length > 0)
                    {
                        //Convert string builder to string
                        allPhotos = allPhotosSb.ToString();

                        //Print names
                        await e.Message.RespondAsync(allPhotos);
                    }
                }

            }

            //If the specified folder isn't in user account
            else if (!userDictionary[e.Author.Id.ToString()].folders.Contains(folderName))
            {
                await e.Message.RespondAsync("**FOLDER DOES NOT EXIST**");
            }

            //If the folder is empty
            else
            {
                await e.Message.RespondAsync("**FOLDER IS EMPTY**");
            }
        }

        /// <summary>
        /// Gets the photo within the specified folder that goes by the specified file name
        /// </summary>
        /// <param name="e">Message variable</param>
        /// <param name="fileName">Photo name to be retrieved</param>
        /// <param name="folderName">Folder for which photo is in</param>
        public static async Task GetSpecificPhoto(MessageCreateEventArgs e, string fileName, string folderName)
        {
            //Ensure folder is actually in user account and is non-empty
            if (userDictionary[e.Author.Id.ToString()].folders.Contains(folderName) && !userDictionary[e.Author.Id.ToString()].FolderIsEmpty(folderName))
            {
                using (CosmosClient client = new CosmosClient(COSMOS_URL, COSMOS_KEY))
                {
                    //Open database
                    Database database = client.GetDatabase(DATABASE_NAME);
                    Console.WriteLine("Database opened!");

                    //Open folders container
                    Container folderContainer = database.GetContainer(FOLDERS_CONTAINER);
                    Console.WriteLine("Folders container opened!");

                    //Retrieve specified folder
                    Folder retrievedFolder = new Folder(folderName, e.Author.Id.ToString());
                    retrievedFolder = await folderContainer.ReadItemAsync<Folder>(retrievedFolder.id, new PartitionKey(retrievedFolder.id));

                    //If the file exists in the folder then give to user
                    if(retrievedFolder.photos.ContainsKey(fileName))
                    {
                        await e.Message.RespondAsync(retrievedFolder.photos[fileName]);
                    }

                    else
                    {
                        await e.Message.RespondAsync("**PHOTO DOES NOT EXIST**");
                    }

                }
            }

            //If the specified folder isn't in user account
            else if (!userDictionary[e.Author.Id.ToString()].folders.Contains(folderName))
            {
                await e.Message.RespondAsync("**FOLDER DOES NOT EXIST**");
            }

            //If the folder is empty
            else
            {
                await e.Message.RespondAsync("**FOLDER EMPTY**");
            }
        }

        public static async Task GetStats(MessageCreateEventArgs e)
        {
            //Get number of users
            long numberOfUsers = userDictionary.Count;

            //Get number of folders and photos
            long numberOfFolders = 0;
            long numberOfPhotos = 0;

            foreach(KeyValuePair<string, User> entry in userDictionary)
            {
                numberOfFolders += Convert.ToInt64(entry.Value.folderCount);
                numberOfPhotos += Convert.ToInt64(entry.Value.photoCount);
            }

            await e.Message.RespondAsync(
                $"Number of users: {numberOfUsers}\n" +
                $"Number of folders: {numberOfFolders}\n" +
                $"Number of photos: {numberOfPhotos}\n");
        }
    }
}

using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PhotoKeep.Methods
{
    /// <summary>
    /// Every method in this file corresponds to a command in the bot
    /// </summary>
    class Commands
    {
        public static async void Help(MessageCreateEventArgs e)
        {
            await e.Message.RespondAsync(
                    "__**Commands**__ \n" +
                    "**///allfolders**:\n Lists every folder you have created \n\n" + //IMPLEMENTED
                    "**///viewfolder/[folder_name]**:\n List all images in folder \n\n" + //IMPLEMENTED
                    "**///newfolder/[folder_name]**:\n Create folder with name *folder_name* (**NO SPACES OR SPECIAL SYMBOLS IN NAME, ONLY ALPHANUMERIC**) \n\n" + //IMPLEMENTED
                    "**///deletefolder/[folder_name]**:\n Delete folder with name *folder_name*. \n\n" + //IMPLEMENTED
                    "**///getphoto/[folder_name]/[photo_name]**:\n Post the image named *photo_name* in the folder *folder_name* \n\n" + //IMPLEMENTED
                    "**///deletephoto/[folder_name]/[photo_name]**:\n Delete the image named *photo_name* in the folder *folder_name* \n\n" + //IMPLEMENTED
                    "**///uploadphoto/[folder_name]/[photo_name]**:\n Upload attachment to bot inside folder called *folder_name* and name the photo *photo_name* (**IMAGE ATTACHMENT MUST BE IN COMMAND MESSAGE. NO SPACES OR SPECIAL SYMBOLS IN NAMES, ONLY ALPHANUMERIC.**) \n\n" + //IMPLEMENTED
                    "**///random**:\n Uploads a random photo from any of your folders.\n\n" + //IMPLEMENTED
                    "**///random/[folder_name]**:\n Uploads a random photo from the folder *folder_name*. \n\n"); //IMPLEMENTED
        }

        public static async void AllFolders(MessageCreateEventArgs e)
        {
            await e.Message.RespondAsync("__All folders__");
            await DBMethods.ListAllFolders(e);
        }

        public static async void ViewFolder(MessageCreateEventArgs e)
        {
            //Find folder name
            string key = "///viewfolder/";
            string folderName = e.Message.Content.Substring(key.Length);

            //Ensure folder name is alphanumeric
            if (Helper.NameValid(folderName))
            {
                await e.Message.RespondAsync($"Viewing folder: {folderName}");
                await DBMethods.ListAllPhotos(e, folderName);
            }

            //If folder name is not proper
            else
            {
                await e.Message.RespondAsync("FOLDER NAME MUST BE ALPHANUMERIC AND NONEMPTY");
            }
        }

        public static async void CreateFolder(MessageCreateEventArgs e)
        {
            //Find folder name
            string key = "///newfolder/";
            string folderName = e.Message.Content.Substring(key.Length);

            //Ensure folder name is alphanumeric
            if (Helper.NameValid(folderName))
            {
                await e.Message.RespondAsync($"Creating Folder: {folderName}");
                await DBMethods.CreateFolderInDB(e, folderName);
            }

            //If folder name is not proper
            else
            {
                await e.Message.RespondAsync("FOLDER NAME MUST BE ALPHANUMERIC AND NONEMPTY");
            }
        }

        public static async void DeleteFolder(MessageCreateEventArgs e)
        {
            //Find folder name
            string key = "///deletefolder/";
            string folderName = e.Message.Content.Substring(key.Length);

            //Ensure folder name is alphanumeric
            if (Helper.NameValid(folderName))
            {
                await e.Message.RespondAsync($"Deleting Folder: {folderName}");
                await DBMethods.DeleteFolderInDB(e, folderName);
            }

            //If folder name is not proper
            else
            {
                await e.Message.RespondAsync("FOLDER NAME MUST BE ALPHANUMERIC AND NONEMPTY");
            }
        }

        public static async void GetPhoto(MessageCreateEventArgs e)
        {
            //Get extension of command (folder and file)
            string key = "///getphoto/";
            string extension = e.Message.Content.Substring(key.Length);
            int indexOfSlash = extension.IndexOf("/");

            //Check that extension is valid
            if (indexOfSlash != -1)
            {
                string folderName = extension.Substring(0, indexOfSlash);
                string fileName = extension.Substring(indexOfSlash + 1);

                //Ensure the supposed folder and filenames are valid names
                if (Helper.NameValid(folderName) && Helper.NameValid(fileName))
                {
                    await DBMethods.GetSpecificPhoto(e, fileName, folderName);
                }

                //If the folder and filename is not valid
                else
                {
                    await e.Message.RespondAsync("FOLDER AND FILENAME MUST BE ALPHANUMERIC AND NONEMPTY");
                }
            }

            //If the whole command is invalid (missing slash)
            else
            {
                await e.Message.RespondAsync("INVALID COMMAND");
            }
        }

        public static async void DeletePhoto(MessageCreateEventArgs e)
        {
            //Get extension of command (folder and file)
            string key = "///deletephoto/";
            string extension = e.Message.Content.Substring(key.Length);
            int indexOfSlash = extension.IndexOf("/");

            //Check that extension is valid
            if (indexOfSlash != -1)
            {
                string folderName = extension.Substring(0, indexOfSlash);
                string fileName = extension.Substring(indexOfSlash + 1);

                //Ensure the supposed folder and filenames are valid names
                if (Helper.NameValid(folderName) && Helper.NameValid(fileName))
                {
                    await e.Message.RespondAsync($"Deleting Photo named '{fileName}' from folder '{ folderName} '");
                    await DBMethods.DeletePhotoInDB(e, fileName, folderName);
                }

                //If the folder and filename is not valid
                else
                {
                    await e.Message.RespondAsync("FOLDER AND FILENAME MUST BE ALPHANUMERIC AND NONEMPTY");
                }
            }

            //If the whole command is invalid (missing slash)
            else
            {
                await e.Message.RespondAsync("INVALID COMMAND");
            }
        }

        public static async void UploadPhoto(MessageCreateEventArgs e)
        {
            //Ensure there is only one attachment
            if (e.Message.Attachments.Count == 1)
            {
                //Get extension of command (folder and file)
                string key = "///uploadphoto/";
                string extension = e.Message.Content.Substring(key.Length);
                int indexOfSlash = extension.IndexOf("/");

                //Check that extension is valid
                if (indexOfSlash != -1)
                {
                    string folderName = extension.Substring(0, indexOfSlash);
                    string fileName = extension.Substring(indexOfSlash + 1);

                    //Ensure the supposed folder and filenames are valid names
                    if (Helper.NameValid(folderName) && Helper.NameValid(fileName))
                    {
                        string fileURL = e.Message.Attachments.FirstOrDefault().Url;
                        await e.Message.RespondAsync($"Uploading Photo named '{fileName}' into folder '{folderName}'");
                        await DBMethods.UploadPhotoInDB(e, fileName, fileURL, folderName);
                    }

                    //If the folder and filename is not valid
                    else
                    {
                        await e.Message.RespondAsync("FOLDER AND FILENAME MUST BE ALPHANUMERIC AND NONEMPTY");
                    }
                }

                //If the whole command is invalid (missing slash)
                else
                {
                    await e.Message.RespondAsync("INVALID COMMAND");
                }
            }

            //When there isn't one and only one attachmenmt
            else
            {
                await e.Message.RespondAsync("**THERE HAS TO BE ONE AND ONLY ONE ATTATCHMENT TO THIS COMMAND**");
            }

        }

        public static async void RandomPhoto(MessageCreateEventArgs e)
        {
            //await e.Message.RespondAsync("Random");
            await DBMethods.RandomPhotoInFolder(e, DBMethods.GetRandomFolderName(e), true);
        }

        public static async void SpecificRandomPhoto(MessageCreateEventArgs e)
        {
            //Find folder name
            string key = "///random/";
            string folderName = e.Message.Content.Substring(key.Length);

            //Ensure folder name is alphanumeric
            if (Helper.NameValid(folderName))
            {
                await DBMethods.RandomPhotoInFolder(e, folderName, false);
            }

            //If folder name is not proper
            else
            {
                await e.Message.RespondAsync("FOLDER NAME MUST BE ALPHANUMERIC AND NONEMPTY");
            }
        }

        public static async void GetStats(MessageCreateEventArgs e)
        {
            await DBMethods.GetStats(e);
        }
    }
}

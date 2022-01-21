/*
 * Name: Shailendra Singh
 * Date: May 18th, 2021
 * Version: Photokeep 1.00
 * Description: Photokeep is a discord bot that allows users to create folders and store photos in those folders. 
 */

using DSharpPlus;
using DSharpPlus.EventArgs;
using System;
using System.Linq;
using System.Threading.Tasks;
using PhotoKeep.Methods;

namespace PhotoKeep
{
    class Program
    {
        //Constants
        private const string TOKEN_CONSTANT = "Insert Your Own Discord Token Here";

        static void Main(string[] args)
        {
            MainAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Asynchronous main method
        /// </summary>
        static async Task MainAsync()
        {
            //Initialize bot
            var discord = new DiscordClient(new DiscordConfiguration()
            {
                Token = TOKEN_CONSTANT,
                TokenType = TokenType.Bot
            });

            //Get all users into cache
            await DBMethods.GetAllUsers();

            //Wait for message
            discord.MessageCreated += OnMessageCreated;

            //Connect and login to discord
            await discord.ConnectAsync();

            //Keep current program running forever
            await Task.Delay(-1);
        }

        /// <summary>
        /// Prints to the console the basic info of every message.
        /// Make sure to disable before formal deployment. Use for 
        /// debugging purposes
        /// </summary>
        /// <param name="e">Message Variable</param>
        private static void BasicInfo(MessageCreateEventArgs e)
        {
            //Give basic info on each message
            Console.WriteLine("Username: {0}", e.Message.Author.Username);
            Console.WriteLine("User ID: {0}", e.Author.Id);
            Console.WriteLine("Message: {0}", e.Message.Content);
            Console.WriteLine("Date: {0}", e.Message.Timestamp);
            Console.WriteLine("Number of attachments {0}", e.Message.Attachments.Count);

            //If attachment, record it
            if (e.Message.Attachments.Count != 0)
            {
                Console.WriteLine("Link to attachment: {0}", e.Message.Attachments.FirstOrDefault().Url);
            }
            Console.WriteLine();
        }

        /// <summary>
        /// This method handles every message that the bot has access to.
        /// </summary>
        /// <param name="e">Message variable</param>
        /// <returns></returns>
        private static async Task OnMessageCreated(MessageCreateEventArgs e)
        {
            //Give basic info on message to console
            BasicInfo(e);

            //DECISION HANDLER
            //Help page
            if (string.Equals(e.Message.Content, "///help", StringComparison.OrdinalIgnoreCase))
            {
                await DBMethods.CreateAccount(e);
                Commands.Help(e);
            }

            //Gets list of all folders
            else if(string.Equals(e.Message.Content, "///allfolders", StringComparison.OrdinalIgnoreCase))
            {
                await DBMethods.CreateAccount(e);
                Commands.AllFolders(e);
            }

            //View content in specified folder
            else if (e.Message.Content.ToLower().StartsWith("///viewfolder/"))
            {
                await DBMethods.CreateAccount(e);
                Commands.ViewFolder(e);
            }

            //Create folder in database with specified name
            else if (e.Message.Content.ToLower().StartsWith("///newfolder/"))
            {
                await DBMethods.CreateAccount(e);
                Commands.CreateFolder(e);
            }

            //Delete folder in database with specified name
            else if (e.Message.Content.ToLower().StartsWith("///deletefolder/"))
            {
                await DBMethods.CreateAccount(e);
                Commands.DeleteFolder(e);
            }

            //Retrieves photo with specified name in specified folder
            else if (e.Message.Content.ToLower().StartsWith("///getphoto/"))
            {
                await DBMethods.CreateAccount(e);
                Commands.GetPhoto(e);
            }

            //Delete photo in database with specified name in specified folder
            else if (e.Message.Content.ToLower().StartsWith("///deletephoto/"))
            {
                await DBMethods.CreateAccount(e);
                Commands.DeletePhoto(e);
            }

            //Upload photo with a specified name and a specified folder in the database
            else if (e.Message.Content.ToLower().StartsWith("///uploadphoto/"))
            {
                await DBMethods.CreateAccount(e);
                Commands.UploadPhoto(e);
            }

            //Posts random photo from database
            else if (string.Equals(e.Message.Content, "///random", StringComparison.OrdinalIgnoreCase))
            {
                await DBMethods.CreateAccount(e);
                Commands.RandomPhoto(e);
            }

            //Posts random photo from folder
            else if (e.Message.Content.ToLower().StartsWith("///random/"))
            {
                await DBMethods.CreateAccount(e);
                Commands.SpecificRandomPhoto(e);
            }

            //SECRET COMMANDS
            //Get the total number of users, number of folders and number of photos
            else if(string.Equals(e.Message.Content, "///stats", StringComparison.OrdinalIgnoreCase))
            {
                await DBMethods.CreateAccount(e);
                Commands.GetStats(e);
            }

        }
    }
}

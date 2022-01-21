using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PhotoKeep
{
    public class User
    {
        //Datafields
        public string id { get; set; }
        public string secretID { get; set; }
        public string name { get; set; }
        public string photoCount { get; set; }
        public string folderCount { get; set; }
        public List<string> folders { get; set; }
        public Dictionary<string, string> folderSizeDictionary { get; set; }

        //User restraints
        private const int FOLDER_LIMIT = 100;

        //Constructor
        public User(ulong id, string name)
        {
            this.id = id.ToString();
            this.secretID = Guid.NewGuid().ToString(); //Potentally be used if I ever want to let users identify themselves on a companion project without using OAuth2 Discord login
            this.name = name;
            this.photoCount = "0";
            this.folderCount = "0";
            this.folders = new List<string>();
            this.folderSizeDictionary = new Dictionary<string, string>();
        }

        /// <summary>
        /// Updates the folder count in
        /// current user object.
        /// </summary>
        private void UpdateFolderCount()
        {
            folderCount = folders.Count.ToString();
        }

        /// <summary>
        /// Increments/Decrements the photo count
        /// by the given parameter (Negative numbers to decrement)
        /// </summary>
        /// <param name="increment">Number to change count by</param>
        public void UpdatePhotoCount(int increment)
        {
            int photoCountInt = Int32.Parse(this.photoCount);
            photoCountInt += increment;
            photoCount = photoCountInt.ToString();
        }

        /// <summary>
        /// Adds folder to user's list of folders
        /// </summary>
        /// <param name="folderName">Name of folder</param>
        /// <returns>True if the folder is successfuly inserted</returns>
        public bool AddFolder(string folderName)
        {
            //If the folder is not there already and that there isn't too many folders
            if(!folders.Contains(folderName) && !UserIsFull())
            {
                folders.Add(folderName);
                folderSizeDictionary.Add(folderName,"0"); 
                UpdateFolderCount();
                return true;
            }

            //If the folder already exists
            return false;
        }

        /// <summary>
        /// Remove folder from user's list of folders
        /// </summary>
        /// <param name="folderName">Name of folder</param>
        /// <returns>True if the folder is successfuly deleted</returns>
        public bool DeleteFolder(string folderName)
        {
            //If the folder is exist
            if (folders.Contains(folderName))
            {
                folders.Remove(folderName);
                folderSizeDictionary.Remove(folderName);
                UpdateFolderCount();
                return true;
            }

            //If the folder does not exists
            return false;
        }

        /// <summary>
        /// This changes the size of the specified folder in the user
        /// document so that the program can easily know the size of the folder
        /// without fiddling in the folder container and wasting time.
        /// </summary>
        /// <param name="folderName">Name of folder that is having count changed</param>
        /// <param name="increment">Amount the count is being changed (negatives are allowed)</param>
        public void UpdateFolderSize(string folderName, int increment)
        {
            int folderSize = int.Parse(folderSizeDictionary[folderName]);
            folderSize += increment;
            folderSizeDictionary[folderName] = folderSize.ToString();
        }

        /// <summary>
        /// Checks if the specifiedfolder has no files
        /// </summary>
        /// <param name="folderName">The folder name to be checked</param>
        /// <returns>Retruns true if folder is empty. False if not</returns>
        public bool FolderIsEmpty(string folderName)
        {
            return folderSizeDictionary[folderName].Equals("0");
        }

        /// <summary>
        /// Checks if the user is at max capacity for folders
        /// </summary>
        /// <returns>True if the user at max capacity, false if not</returns>
        public bool UserIsFull()
        {
            return (int.Parse(folderCount) >= FOLDER_LIMIT);
        }

        /// <summary>
        /// Ensures the objects have the same ID
        /// </summary>
        /// <param name="obj">Object to be compared</param>
        public override bool Equals(object obj)
        {
            //Check for null and compare run-time types.
            if ((obj == null) || !GetType().Equals(obj.GetType()))
            {
                return false;
            }

            //Ensure their ID's are the same
            else
            {
                User user = (User)obj;
                return (id.Equals(user.id));
            }
        }

        public override int GetHashCode()
        {
            return id.GetHashCode();
        }

        public override string ToString()
        {
            string stringForm = $"ID: {id}\n" +
                                $"Secret ID: {secretID}\n" +
                                $"Name: {name}\n" +
                                $"Photo Count: {photoCount}\n" +
                                $"Folder Count: {folderCount}\n";

            return stringForm;
        }

    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace PhotoKeep
{
    public class Folder
    {
        //Datafields
        public string id { get; set; }
        public string name { get; set; }
        public string photoCount { get; set; }
        public Dictionary<string, string> photos { get; set; }

        //Folder restraints
        private const int PHOTO_LIMIT = 1000;

        //Constructor
        public Folder(string name, string userId)
        {
            this.id = $"{userId}{name}";
            this.name = name;
            this.photoCount = "0";
            photos = new Dictionary<string, string>();
        }

        /// <summary>
        /// Increments/Decrements the photo count
        /// by the given parameter (Negative numbers to decrement)
        /// </summary>
        /// <param name="increment">Number to change count by</param>
        private void UpdatePhotoCount(int increment)
        {
            int photoCountInt = int.Parse(photoCount);
            photoCountInt += increment;
            photoCount = photoCountInt.ToString();
        }

        /// <summary>
        /// Add a photo into the folder object dictionary
        /// </summary>
        /// <param name="photoName">Name of photo to be uploaded</param>
        /// <param name="photoLink">Discord link to photo</param>
        /// <returns>True if insert was successful. False if unsucessful</returns>
        public bool InsertPhoto(string photoName, string photoLink)
        {
            //Check if photo does not exist yet and the folder is not full
            if(!photos.ContainsKey(photoName) && !FolderIsFull())
            {
                photos.Add(photoName, photoLink);
                UpdatePhotoCount(1);
                return true;
            }

            //If the photo already exists
            return false;
        }

        /// <summary>
        /// Attempts to delete specified photo from folder
        /// </summary>
        /// <param name="photoName">Name of photo to be deleted</param>
        /// <returns>True if deletion was successful. False if failure.</returns>
        public bool DeletePhoto(string photoName)
        {
            //Check if photo exists already
            if(photos.ContainsKey(photoName))
            {
                photos.Remove(photoName);
                UpdatePhotoCount(-1);
                return true;
            }

            //If the photo does not exist
            return false;
        }

        /// <summary>
        /// Checks if the folder is at max capacity for files
        /// </summary>
        /// <returns>True if full, false if not</returns>
        public bool FolderIsFull()
        {
            return (int.Parse(photoCount) >= PHOTO_LIMIT);
        }
    }
}

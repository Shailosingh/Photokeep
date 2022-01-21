using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PhotoKeep.Methods
{
    class Helper
    {
        /// <summary>
        /// Ensures name is valid: Is alphanumeric  
        /// and nonempty but, less than or equal to 200 characters
        /// </summary>
        /// <param name="name">The name to be checked</param>
        /// <returns></returns>
        public static bool NameValid(string name)
        {
            //Initialize variables
            bool isValid = false;

            //Ensure folder name is alphanumeric and non empty
            if (name.All(char.IsLetterOrDigit) && name.Length != 0 && name.Length <= 200)
            {
                return true;
            }

            //Return validity
            return isValid;
        }
    }
}

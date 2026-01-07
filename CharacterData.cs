using System;

namespace GetALife.Game
{
    [Serializable]
    public class CharacterData
    {
        public string FirstName;
        public string LastName;
        public string Country;
        public string City;
        public string Gender;
        public int BirthYear;
        public int BirthMonth;
        public int BirthDay;

        public string FullName => $"{FirstName} {LastName}".Trim();
        public string BirthDate => BirthYear <= 0 ? string.Empty : $"{BirthMonth:D2}/{BirthDay:D2}/{BirthYear}";
    }
}
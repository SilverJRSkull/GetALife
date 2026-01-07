using System;
using System.Collections.Generic;
using UnityEngine;

namespace GetALife.Game
{
    public class CharacterGenerator : MonoBehaviour
    {
        private static readonly string[] MaleFirstNames =
        {
            "William",
            "Donovan",
            "Benjamin",
            "Nicholas",
            "Garion",
            "Alexander",
            "James",
            "Joey",
            "Henry",
            "Liam",
            "Noah",
            "Ethan",
            "Mason",
            "Logan"
        };

        private static readonly string[] FemaleFirstNames =
        {
            "Claira",
            "Maybel",
            "Peggy",
            "Kylie",
            "Shelby",
            "Emma",
            "Olivia",
            "Ava",
            "Mia",
            "Sophia"
        };

        private static readonly string[] LastNames =
        {
            "Savage",
            "Adair",
            "Manning",
            "McKay",
            "McCarthy",
            "Smith",
            "Johnson",
            "Brown",
            "Garcia",
            "Wilson"
        };

        private static readonly string[] CountriesList =
        {
            "Canada",
            "United States"
        };

        private static readonly string[] GendersList = { "Male", "Female" };

        private static readonly Dictionary<string, string[]> CitiesByCountry =
            new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                { "Canada", new[] { "Toronto", "Vancouver", "Montreal", "Calgary", "Ottawa" } },
                { "United States", new[] { "New York", "Los Angeles", "Chicago", "Houston", "Miami" } }
            };

        public string[] Countries => CountriesList;
        public string[] Genders => GendersList;

        public CharacterData GenerateRandomCharacter()
        {
            string gender = GetRandomGender();
            string firstName = GetRandomFirstName(gender);
            DateTime birthDate = GenerateRandomBirthDate();

            string country = GetRandomFrom(CountriesList);
            string city = GetRandomFrom(GetCitiesForCountry(country));

            return new CharacterData
            {
                FirstName = firstName,
                LastName = GetRandomFrom(LastNames),
                Country = country,
                City = city,
                Gender = gender,
                BirthYear = birthDate.Year,
                BirthMonth = birthDate.Month,
                BirthDay = birthDate.Day
            };
        }

        private string GetRandomFrom(string[] pool)
        {
            if (pool == null || pool.Length == 0)
            {
                return string.Empty;
            }

            return pool[UnityEngine.Random.Range(0, pool.Length)];
        }

        public string GetRandomFirstName(string gender)
        {
            if (string.Equals(gender, "Female", StringComparison.OrdinalIgnoreCase))
            {
                return GetRandomFrom(FemaleFirstNames);
            }

            return GetRandomFrom(MaleFirstNames);
        }

        public string GetRandomLastName()
        {
            return GetRandomFrom(LastNames);
        }

        public string GetRandomGender()
        {
            return GetRandomFrom(GendersList);
        }

        public string[] GetFirstNamesForGender(string gender)
        {
            return string.Equals(gender, "Female", StringComparison.OrdinalIgnoreCase)
                ? FemaleFirstNames
                : MaleFirstNames;
        }

        public string[] GetCitiesForCountry(string country)
        {
            if (string.IsNullOrWhiteSpace(country))
            {
                return Array.Empty<string>();
            }

            return CitiesByCountry.TryGetValue(country, out string[] cities) ? cities : Array.Empty<string>();
        }

        private DateTime GenerateRandomBirthDate()
        {
            int currentYear = DateTime.Now.Year;
            int year = UnityEngine.Random.Range(1950, currentYear + 1);
            int month = UnityEngine.Random.Range(1, 13);
            int daysInMonth = DateTime.DaysInMonth(year, month);
            int day = UnityEngine.Random.Range(1, daysInMonth + 1);

            return new DateTime(year, month, day);
        }
    }
}
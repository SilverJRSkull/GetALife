using UnityEngine;

namespace GetALife.Game
{
    public class GameSession : MonoBehaviour
    {
        [SerializeField] private CharacterData currentCharacter;

        public CharacterData CurrentCharacter => currentCharacter;

        public void StartNewLife(CharacterData character)
        {
            currentCharacter = character;
        }
    }
}
using Microsoft.Xna.Framework.Input;

namespace Maze.Engine
{
    public class InputController
    {
        private KeyboardState _oldState;
        private KeyboardState _currentState;

        public void Update()
        {
            _oldState = _currentState;
            _currentState = Keyboard.GetState();
        }

        public bool Pressed(Keys key) =>
            _currentState.IsKeyDown(key);

        public bool ChangedUp(Keys key) =>
            _oldState.IsKeyDown(key) && _currentState.IsKeyUp(key);

        public bool ChangedDown(Keys key) =>
            _oldState.IsKeyUp(key) && _currentState.IsKeyDown(key);

    }
}

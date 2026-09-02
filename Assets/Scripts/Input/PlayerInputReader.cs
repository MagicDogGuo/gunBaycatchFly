using UnityEngine;

namespace DreamChase
{
    public readonly struct PlayerInputSnapshot
    {
        public readonly Vector2 Move;
        public readonly bool RunHeld;
        public readonly bool JumpPressed;

        public PlayerInputSnapshot(Vector2 move, bool runHeld, bool jumpPressed)
        {
            Move = move;
            RunHeld = runHeld;
            JumpPressed = jumpPressed;
        }
    }

    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public sealed class PlayerInputReader : MonoBehaviour
    {
        public PlayerInputSnapshot Current { get; private set; }

        public PlayerInputSnapshot Sample()
        {
            Current = ReadNow();
            return Current;
        }

        static PlayerInputSnapshot ReadNow()
        {
            float x = 0f;
            if (UnityEngine.Input.GetKey(KeyCode.A) || UnityEngine.Input.GetKey(KeyCode.LeftArrow))
                x -= 1f;
            if (UnityEngine.Input.GetKey(KeyCode.D) || UnityEngine.Input.GetKey(KeyCode.RightArrow))
                x += 1f;

            float y = 0f;
            if (UnityEngine.Input.GetKey(KeyCode.S) || UnityEngine.Input.GetKey(KeyCode.DownArrow))
                y -= 1f;
            if (UnityEngine.Input.GetKey(KeyCode.W) || UnityEngine.Input.GetKey(KeyCode.UpArrow))
                y += 1f;

            Vector2 move = new Vector2(x, y);
            if (move.sqrMagnitude > 1f)
                move.Normalize();

            bool runHeld = UnityEngine.Input.GetKey(KeyCode.LeftShift)
                || UnityEngine.Input.GetKey(KeyCode.RightShift);
            bool jumpPressed = UnityEngine.Input.GetKeyDown(KeyCode.Space);

            return new PlayerInputSnapshot(move, runHeld, jumpPressed);
        }
    }
}

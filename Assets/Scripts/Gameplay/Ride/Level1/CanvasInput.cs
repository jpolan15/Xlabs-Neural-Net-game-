using UnityEngine;

namespace Convergence.Gameplay.Ride.Level1
{
    /// <summary>A game played on the drawing canvas (draw your 3, stump Pixel). Points are canvas units, 0 to 1, y down.</summary>
    public interface ICanvasGame
    {
        bool IsLive { get; }

        bool StrokeBegin(int hand, Vector2 point, bool pressed);

        void StrokeMove(int hand, Vector2 point);

        void StrokeEnd(int hand);
    }

    /// <summary>A game with buttons on the game table (DONE, CLEAR, the three step sizes).</summary>
    public interface IButtonGame
    {
        bool IsLive { get; }

        /// <summary>The laser pressed button b. Returns true when it did something.</summary>
        bool Press(int button, int hand);

        /// <summary>The hand that pressed button b let go of its trigger (a hold button stops).</summary>
        void Release(int button, int hand);
    }

    /// <summary>
    /// The drawing canvas on the game table: one flat collider (a unit quad, scaled). A held laser on it is a stroke; this
    /// turns the world point into canvas units (0 to 1, x right, y down) and hands it to whichever canvas game is live.
    /// It decides nothing.
    /// </summary>
    public sealed class CanvasInput : MonoBehaviour, IRideStrokeTarget
    {
        [SerializeField] private MonoBehaviour[] games = new MonoBehaviour[0];

        private ICanvasGame _stroking;

        /// <summary>The live canvas game, or null.</summary>
        public ICanvasGame Live
        {
            get
            {
                foreach (MonoBehaviour m in games)
                {
                    if (m is ICanvasGame g && g.IsLive) return g;
                }

                return null;
            }
        }

        /// <summary>A world point on the canvas in canvas units.</summary>
        public Vector2 ToCanvas(Vector3 world)
        {
            Vector3 local = transform.InverseTransformPoint(world);
            return new Vector2(local.x + 0.5f, 0.5f - local.y);
        }

        /// <summary>A canvas point in the world (for views).</summary>
        public Vector3 ToWorld(Vector2 canvas) => transform.TransformPoint(new Vector3(canvas.x - 0.5f, 0.5f - canvas.y, 0f));

        public bool StrokeBegin(int hand, Vector3 point, bool pressed)
        {
            ICanvasGame game = Live;
            if (game == null || !game.StrokeBegin(hand, ToCanvas(point), pressed)) return false;
            _stroking = game;
            return true;
        }

        public void StrokeMove(int hand, Vector3 point)
        {
            if (_stroking != null && _stroking.IsLive) _stroking.StrokeMove(hand, ToCanvas(point));
        }

        public void StrokeEnd(int hand)
        {
            _stroking?.StrokeEnd(hand);
            _stroking = null;
        }
    }
}

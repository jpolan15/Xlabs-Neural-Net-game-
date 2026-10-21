namespace Convergence.Gameplay.Ride
{
    /// <summary>
    /// Something the rider can pet (Pixel). XR turns a click or a hand touch into <see cref="Pet"/>; the thing itself
    /// decides how to look happy about it. It never affects the ride or any puzzle.
    /// </summary>
    public interface IPettable
    {
        void Pet();
    }
}

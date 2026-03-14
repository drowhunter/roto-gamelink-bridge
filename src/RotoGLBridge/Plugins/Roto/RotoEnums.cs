namespace RotoGLBridge.Plugins
{
    /*
    public enum RotoDirection : byte
    {
        /// <summary>
        /// Rotate to the left (counterclockwise).
        /// </summary>
        Left = 0x00,

        /// <summary>
        /// Rotate to the right (clockwise).
        /// </summary>
        Right = 0x01,
    }
    */

    public enum RunMode
    {
        /// <summary>
        /// The chair remains stationary and ignores all rotation commands.
        /// </summary>

        Idle,
        /// <summary>
        /// Calibration mode. Used to reset or adjust the chair's default orientation.
        /// </summary>

        Calibrating,
        /// <summary>
        /// Follow Object mode. Uses <see cref="HeadTrack"/> internally.
        /// Allows the chair to follow the rotation of a specified GameObject in the scene.
        /// </summary>

        Follow,
        /// <summary>
        /// Free movement mode. The user can manually rotate the chair without any angle restrictions.
        /// </summary>

        Free,
        /// <summary>
        /// Cockpit mode. The user can rotate the chair, but movement is restricted within predefined angle limits.
        /// </summary>

        Cockpit
    }
    

    /*
    public enum RotoMovementMode
    {
        Smooth,
        Jerky
        
    }
    */
}

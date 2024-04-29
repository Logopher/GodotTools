using Godot;

/// <summary>
/// This class calculates position and direction on a hexagonal grid.
/// See either of these links for an explanation:
///     https://www.redblobgames.com/grids/hexagons/
///     https://math.stackexchange.com/questions/2254655/hexagon-grid-coordinate-system
/// 
/// Math assumes consistently sized regular hexagons with outer diameter 1,
/// inner diameter <see cref="ApothemRatio"/>. (This will be refactored with
/// inner diameter 1.)
/// 
/// Angles start at 0 to the east (x=1, y=0) and increase counterclockwise, following the
/// left-hand rule.
/// </summary>
public static class HexGrid
{
    public const float DegreeIncrement = 60;
    public const float RadianIncrement = Mathf.Pi / 3;
    public static readonly float ApothemRatio = Mathf.Cos(Mathf.Pi / 6);

    #region directions
    public static readonly Vector3 North = new(0.5f, 0.5f, -1);
    public static readonly Vector3 N = North;

    public static readonly Vector3 South = new(-0.5f, -0.5f, 1);
    public static readonly Vector3 S = South;

    public static readonly Vector3 East = new(1, -1, 0);
    public static readonly Vector3 E = East;

    public static readonly Vector3 West = new(-1, 1, 0);
    public static readonly Vector3 W = West;

    public static readonly Vector3 NorthNorthEast = new(1, 0, -1);
    public static readonly Vector3 NNE = NorthNorthEast;

    public static readonly Vector3 NorthNorthWest = new(0, 1, -1);
    public static readonly Vector3 NNW = NorthNorthWest;

    public static readonly Vector3 SouthSouthEast = new(0, -1, 1);
    public static readonly Vector3 SSE = SouthSouthEast;

    public static readonly Vector3 SouthSouthWest = new(-1, 0, 1);
    public static readonly Vector3 SSW = SouthSouthWest;
    #endregion

    // This is a utility method for other methods in this class.
    static float[] GetCoordinates(this Vector3 v) => [v.X, v.Y, v.Z];

    /// <summary>
    /// Finds the number of moves between adjacent hexes required to
    /// get from (0, 0, 0) to <paramref name="v"/>.
    /// </summary>
    /// <param name="v">A hex-grid position.</param>
    /// <returns>The number of moves between adjacent hexe required to
    /// get from (0, 0, 0) to <paramref name="v"/>.</returns>
    public static float GetManhattanMagnitude(this Vector3 v)
    {
        var manhattan = v.GetCoordinates()
            .Select(Math.Abs)
            .Max();

        return manhattan;
    }

    /// <summary>
    /// Finds the shortest distance from (0, 0, 0) to <paramref name="v"/>.
    /// </summary>
    /// <param name="v">A hex-grid position.</param>
    /// <returns>The shortest distance from (0, 0, 0) to <paramref name="v"/>.</returns>
    public static float GetEuclideanMagnitude(this Vector3 v)
    {
        var coords = v.GetCoordinates()
            .Select(a => a * ApothemRatio)
            .ToArray();

        if (coords.All(c => c == 0))
        {
            return 0;
        }

        var manhattan = v.GetManhattanMagnitude() * ApothemRatio;

        if (coords.Any(c => c == 0))
        {
            return manhattan;
        }

        var sides = coords
            .Except([manhattan])
            .ToArray();

        var a = sides[0];
        var b = sides[1];
        var c = Mathf.Sqrt((a * a) + (b * b) - (2 * a * b * Mathf.Cos(120)));

        return c;
    }

    /// <summary>
    /// Finds the 2-dimensional orientation from (0, 0, 0)
    /// to <paramref name="v"/>.
    /// </summary>
    /// <param name="v">A hex-grid position.</param>
    /// <returns>The 2-dimensional orientation from (0, 0, 0)
    /// to <paramref name="v"/>.</returns>
    /// <exception cref="Exception">The vector is not a valid hex-grid position.</exception>
    public static float GetAngle(this Vector3 v)
    {
        var coords = v.GetCoordinates();

        var neg = coords.Count(f => f < 0);
        var pos = coords.Count(f => 0 < f);

        var x = Math.Abs(v.X);
        var y = Math.Abs(v.Y);
        var z = Math.Abs(v.Z);

        var manhattan = v.GetManhattanMagnitude();

        // East is 0 degrees.
        // North and South are not cardinal directions.

        // origin - technically this should return NaN
        if (v == Vector3.Zero)
        {
            return 0;
        }

        // East-North-East
        if (pos == 1 && 0 < v.X)
        {
            return RadianIncrement * (0 + (z / manhattan));
        }

        // North
        if (neg == 1 && v.Z < 0)
        {
            return RadianIncrement * (1 + (y / manhattan));
        }

        // West-North-West
        if (pos == 1 && 0 < v.Y)
        {
            return RadianIncrement * (2 + (x / manhattan));
        }

        // West-South-West
        if (neg == 1 && v.X < 0)
        {
            return RadianIncrement * (3 + (z / manhattan));
        }

        // South
        if (pos == 1 && 0 < v.Z)
        {
            return RadianIncrement * (4 + (y / manhattan));
        }

        // East-South-East
        if (neg == 1 && v.Y < 0)
        {
            return RadianIncrement * (5 + (x / manhattan));
        }

        throw new Exception();
    }

    /// <summary>
    /// Gets the hex-grid position matching the given polar coordinates.
    /// 
    /// This method does not round to the nearest hex center.
    /// </summary>
    /// <param name="magnitude">A distance from (0, 0, 0).</param>
    /// <param name="angle">A 2-dimensional orientation around (0, 0, 0).</param>
    /// <returns>A hex-grid position.</returns>
    /// <exception cref="Exception">Your machine is bad at math.</exception>
    public static Vector3 FromPolar(float magnitude, float angle)
    {
        angle %= 360;

        var sector = angle / 60;

        Vector3 result;
        Vector3 increment;
        switch (Math.Floor(sector))
        {
            case 0:
                result = East;
                increment = new Vector3(0, 1, -1);
                break;
            case 1:
                result = NNE;
                increment = new Vector3(-1, 1, 0);
                break;
            case 2:
                result = NNW;
                increment = new Vector3(-1, 0, 1);
                break;
            case 3:
                result = West;
                increment = new Vector3(0, -1, 1);
                break;
            case 4:
                result = SSW;
                increment = new Vector3(1, -1, 0);
                break;
            case 5:
                result = SSE;
                increment = new Vector3(1, 0, -1);
                break;
            default:
                throw new Exception();
        };

        var lateralOffset = (sector % 1) * magnitude;

        result += lateralOffset * increment;

        return result;
    }

    /// <summary>
    /// Finds the number of moves between adjacent hexes required to
    /// get from <paramref name="a"/> to <paramref name="b"/>.
    /// </summary>
    /// <param name="a">A hex-grid position.</param>
    /// <param name="b">Another hex-grid position.</param>
    /// <returns>The number of moves between adjacent hexes required to
    /// get from <paramref name="a"/> to <paramref name="b"/>.</returns>
    public static float ManhattanDistance(Vector3 a, Vector3 b)
    {
        var diff = a - b;

        return diff.GetManhattanMagnitude();
    }

    /// <summary>
    /// Finds the shortest distance from <paramref name="a"/> to <paramref name="b"/>.
    /// </summary>
    /// <param name="a">A hex-grid position.</param>
    /// <param name="b">Another hex-grid position.</param>
    /// <returns>The shortest distance from <paramref name="a"/> to <paramref name="b"/>.</returns>
    public static float EuclideanDistance(Vector3 a, Vector3 b)
    {
        var diff = a - b;

        return diff.GetEuclideanMagnitude();
    }

    /// <summary>
    /// Gets the Cartesian vector matching <paramref name="v"/>.
    /// </summary>
    /// <param name="v">A hex-grid position.</param>
    /// <returns>The Cartesian vector matching <paramref name="v"/>.</returns>
    public static Vector2 ToCartesian(Vector3 v)
    {
        var mag = v.GetEuclideanMagnitude();
        var ang = v.GetAngle();

        var x = mag * Mathf.Cos(ang);
        var y = mag * Mathf.Sin(ang);

        return new Vector2(x, y);
    }

    /// <summary>
    /// Gets the hex-grid position matching <paramref name="v"/>.
    /// </summary>
    /// <param name="v">A 2-dimensional Cartesian vector.</param>
    /// <returns>The hex-grid position matching <paramref name="v"/>.</returns>
    public static Vector3 ToHex(Vector2 v)
    {
        var mag = v.Length();
        var ang = Mathf.Atan2(v.Y, v.X);

        return FromPolar(mag, ang);
    }
}
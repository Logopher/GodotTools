using Godot;
using System;

namespace Clarus.ClarusAsm.Geometry
{
    /// <summary>
    /// This class calculates position and direction on a hexagonal grid.
    /// See either of these links for an explanation:
    ///     https://www.redblobgames.com/grids/hexagons/
    ///     https://math.stackexchange.com/questions/2254655/hexagon-grid-coordinate-system
    /// 
    /// Math assumes consistently sized regular hexagons with inner diameter 1,
    /// outer diameter <see cref="WideWidth"/>.
    /// 
    /// Angles start at 0 to the east (x=1, y=0) and increase counterclockwise, following the
    /// left-hand rule.
    /// </summary>
    public static class HexGrid
    {
        public const float DegreeIncrement = 60;
        public const float RadianIncrement = Mathf.Tau / 6;

        /// <summary>
        /// The cube Z axis in 2D world space.
        /// </summary>
        public static readonly Vector2 CubeZAxis = new(0, 1);
        /// <summary>
        /// The cube X axis in 2D world space.
        /// </summary>
        public static readonly Vector2 CubeXAxis = CubeZAxis.Rotated(Mathf.Tau / -3f);
        /// <summary>
        /// The cube Y axis in 2D world space.
        /// </summary>
        public static readonly Vector2 CubeYAxis = CubeZAxis.Rotated(Mathf.Tau / 3f);

        public const float SideLength = Width / 2;
        public const float Height = 1.1547f;
        public const float Width = 1f;
        // The components of this vector can be made negative to find
        // the positions of all 4 diagonal adjacent hexagons.
        public static readonly Vector2 DiagonalOffset = new(Width / 2, Height * 3 / 4);

        #region directions
        public static readonly Vector3 North = new(0.5f, 0.5f, -1);
        public static readonly Vector3 N = North;

        public static readonly Vector3 South = new(-0.5f, -0.5f, 1);
        public static readonly Vector3 S = South;

        public static readonly Vector3 East = new(-1, 1, 0);
        public static readonly Vector3 E = East;

        public static readonly Vector3 West = new(1, -1, 0);
        public static readonly Vector3 W = West;

        public static readonly Vector3 NorthNorthEast = new(-1, 0, 1);
        public static readonly Vector3 NNE = NorthNorthEast;

        public static readonly Vector3 NorthNorthWest = new(0, -1, 1);
        public static readonly Vector3 NNW = NorthNorthWest;

        public static readonly Vector3 SouthSouthEast = new(0, 1, -1);
        public static readonly Vector3 SSE = SouthSouthEast;

        public static readonly Vector3 SouthSouthWest = new(1, 0, -1);
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
                .Select(Mathf.Abs)
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
                .Select(a => a)
                .ToList();

            if (coords.All(c => c == 0))
            {
                return 0;
            }

            var manhattan = v.GetManhattanMagnitude();

            if (coords.Any(c => c == 0))
            {
                return manhattan;
            }

            var sides = coords
                .Select(c => new
                {
                    Value = c,
                    AbsoluteValue = Mathf.Abs(c),
                })
                .ToList();

            // Remove exactly one coordinate of magnitude 'manhattan'.
            var index = sides.FindIndex(o => o.AbsoluteValue == manhattan);
            if (index == -1)
            {
                throw new Exception();
            }

            sides.RemoveAt(index);

            var a = coords[0];
            var b = coords[1];
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

            if (0.1f <= coords.Sum())
            {
                GD.Print($"Invalid hex vector: {v}");
                throw new Exception();
            }

            var neg = coords.Count(f => f < 0);
            var pos = coords.Count(f => 0 < f);

            var x = Math.Abs(v.X);
            var y = Math.Abs(v.Y);
            var z = Math.Abs(v.Z);

            var manhattan = v.GetManhattanMagnitude();

            // West is 0 degrees.
            // North and South are not cardinal directions.

            // origin - technically this should return NaN
            if (v == Vector3.Zero)
            {
                return 0;
            }

            float? factor = null;

            // West
            if (pos == 1 && 0 < v.Y)
            {
                factor = 0 + (z / manhattan);
            }

            // North-North-West
            if (neg == 1 && v.Z < 0)
            {
                factor = 1 + (x / manhattan);
            }

            // North-North-East
            if (pos == 1 && 0 < v.X)
            {
                factor = 2 + (y / manhattan);
            }

            // East
            if (neg == 1 && v.Y < 0)
            {
                factor = 3 + (z / manhattan);
            }

            // South-South-East
            if (pos == 1 && 0 < v.Z)
            {
                factor = 4 + (x / manhattan);
            }

            // South-South-West
            if (neg == 1 && v.X < 0)
            {
                factor = 5 + (y / manhattan);
            }

            if (factor == null)
            {
                GD.Print($"Invalid hex vector: {v}");
                throw new Exception();
            }

            return RadianIncrement * factor.Value;
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
            angle = (Mathf.Tau + angle) % Mathf.Tau;

            var sector = angle / RadianIncrement;

            Vector3 result;
            Vector3 increment;
            switch (Math.Floor(sector))
            {
                case 0:
                    result = East;
                    increment = new Vector3(0, -1, 1);
                    break;
                case 1:
                    result = NNE;
                    increment = new Vector3(1, -1, 0);
                    break;
                case 2:
                    result = NNW;
                    increment = new Vector3(1, 0, -1);
                    break;
                case 3:
                    result = West;
                    increment = new Vector3(-1, 1, 0);
                    break;
                case 4:
                    result = SSW;
                    increment = new Vector3(0, 1, -1);
                    break;
                case 5:
                    result = SSE;
                    increment = new Vector3(-1, 0, 1);
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

        static Line2 GetCubeXRank(Vector3 v) => new Line2(v.X * CubeXAxis, CubeXAxis.Rotated(Mathf.Tau / -3));
        static Line2 GetCubeYRank(Vector3 v) => new Line2(v.Z * CubeYAxis, CubeYAxis.Rotated(Mathf.Tau / 3));

        static Line2 GetCubeXRank(Vector2 v) => new Line2(v.Project(CubeXAxis), CubeXAxis.Rotated(Mathf.Tau / -3));
        static Line2 GetCubeYRank(Vector2 v) => new Line2(v.Project(CubeYAxis), CubeYAxis.Rotated(Mathf.Tau / 3));

        /// <summary>
        /// Gets the Cartesian vector matching <paramref name="v"/>.
        /// </summary>
        /// <param name="v">A hex-grid position.</param>
        /// <returns>The Cartesian vector matching <paramref name="v"/>.</returns>
        public static Vector2 ToCartesian(this Vector3 v)
        {
            var xRank = GetCubeXRank(v);
            var yRank = GetCubeYRank(v);

            var result = xRank.GetIntersection(yRank);

            return result.Normalized();
        }

        /// <summary>
        /// Gets the hex-grid position matching <paramref name="v"/>.
        /// </summary>
        /// <param name="v">A 2-dimensional Cartesian vector.</param>
        /// <returns>The hex-grid position matching <paramref name="v"/>.</returns>
        public static Vector3 ToHex(this Vector2 v)
        {
            var xRank = GetCubeXRank(v);
            var yRank = GetCubeYRank(v);

            var x = v.Dot(xRank.Direction);
            var y = v.Dot(yRank.Direction);
            var z = -(x + y);

            return new Vector3(x, y, z);
        }

        public static IEnumerable<Vector3> GetAdjacent(this Vector3 v)
        {
            yield return v + East;
            yield return v + NNE;
            yield return v + NNW;
            yield return v + West;
            yield return v + SSW;
            yield return v + SSE;
        }

        public static IEnumerable<Vector2> GetVertices(this Vector3 v)
        {
            var position = v.ToCartesian();

            var magnitude = Width / 2;

            foreach (var angle in Enumerable.Range(0, 6).Select(i => i * 60))
            {
                // Vector2.FromPolar
                var x = magnitude * Mathf.Cos(angle);
                var y = magnitude * Mathf.Sin(angle);

                yield return position + new Vector2(x, y);
            }
        }

        public static bool IsWithinCartesianArea(this Vector3 v, Rect2 area)
            => v.GetVertices().All(area.HasPoint);

        public static Vector3 Round(this Vector3 v)
        {
            v = v with
            {
                X = Mathf.Round(v.X),
                Y = Mathf.Round(v.Y),
            };

            return v;
        }

        public static IEnumerable<Vector3> GetPositions(Rect2 rect)
        {
            var points = rect.GetPoints();
            var hexes = points
                .Select(p => p.ToHex().Round())
                .Distinct();
            return hexes;
        }
    }
}

﻿using System;

namespace DeBroglie
{

    public static class TopArrayUtils
    {
        public delegate bool TileRotate<T>(T tile, out T result);

        public static ITopArray<Tile> Rotate(ITopArray<Tile> original, int rotate, bool reflectX = false, TileRotation tileRotation = null)
        {
            bool TileRotate(Tile tile, out Tile result)
            {
                return tileRotation.Rotate(tile, rotate, reflectX, out result);
            }
            return Rotate<Tile>(original, rotate, reflectX, tileRotation == null ? null : (TileRotate<Tile> )TileRotate);
        }

        public static ITopArray<T> Rotate<T>(ITopArray<T> original, int rotate, bool reflectX = false, TileRotate<T> tileRotate = null)
        {
            if (rotate == 0 && !reflectX)
                return original;

            ValueTuple<int, int> MapCoord(int x, int y)
            {
                if(reflectX)
                {
                    x = -x;
                }
                switch (rotate)
                {
                    case 0:
                        return (x, y);
                    case 1:
                        return (y, -x);
                    case 2:
                        return (-x, -y);
                    case 3:
                        return (-y, x);
                    default:
                        throw new Exception();
                }
            }

            return RotateInner(original, MapCoord, tileRotate);
        }

        public static ITopArray<Tile> HexRotate(ITopArray<Tile> original, int rotate, bool reflectX = false, TileRotation tileRotation = null)
        {
            bool TileRotate(Tile tile, out Tile result)
            {
                return tileRotation.Rotate(tile, rotate, reflectX, out result);
            }
            return HexRotate<Tile>(original, rotate, reflectX, tileRotation == null ? null : (TileRotate<Tile>)TileRotate);
        }

        public static ITopArray<T> HexRotate<T>(ITopArray<T> original, int rotate, bool reflectX, TileRotate<T> tileRotate = null)
        {
            if (rotate == 0 && !reflectX)
                return original;

            var microRotate = rotate % 3;
            var rotate180 = rotate % 2 == 1;

            // Actually do a reflection/rotation
            ValueTuple<int, int> MapCoord(int x, int y)
            {
                if (reflectX)
                {
                    x = -x + y;
                }
                var q = x - y;
                var r = -x;
                var s = y;
                var q2 = q;
                switch (microRotate)
                {
                    case 0: break;
                    case 1: q = r; r = s; s = q2; break;
                    case 2: q = s; s = r; r = q2; break;
                }
                if (rotate180)
                {
                    q = -q;
                    r = -r;
                    s = -s;
                }
                x = -r;
                y = s;
                return (x, y);
            }

            return RotateInner(original, MapCoord, tileRotate);
        }


        private static ITopArray<T> RotateInner<T>(ITopArray<T> original, Func<int, int, ValueTuple<int, int>> mapCoord, TileRotate<T> tileRotate = null)
        {
            // Find new bounds
            var (x1, y1) = mapCoord(0, 0);
            var (x2, y2) = mapCoord(original.Topology.Width - 1, 0);
            var (x3, y3) = mapCoord(original.Topology.Width - 1, original.Topology.Height - 1);
            var (x4, y4) = mapCoord(0, original.Topology.Height - 1);

            var minx = Math.Min(Math.Min(x1, x2), Math.Min(x3, x4));
            var maxx = Math.Max(Math.Max(x1, x2), Math.Max(x3, x4));
            var miny = Math.Min(Math.Min(y1, y2), Math.Min(y3, y4));
            var maxy = Math.Max(Math.Max(y1, y2), Math.Max(y3, y4));

            // Arrange so that co-ordinate transfer is into the rect bounced by width, height
            var offsetx = -minx;
            var offsety = -miny;
            var width = maxx - minx + 1;
            var height = maxy - miny + 1;

            var mask = new bool[width * height];
            var topology = new Topology(original.Topology.Directions, width, height, original.Topology.Depth, false, mask);
            var values = new T[width, height];

            // Copy from original to values based on the rotation, setting up the mask as we go.
            for (var z = 0; z < original.Topology.Depth; z++)
            {
                for (var y = 0; y < original.Topology.Height; y++)
                {
                    for (var x = 0; x < original.Topology.Width; x++)
                    {
                        var (newX, newY) = mapCoord(x, y);
                        newX += offsetx;
                        newY += offsety;
                        int newIndex = topology.GetIndex(newX, newY, 0);
                        var newValue = original.Get(x, y, 0);
                        bool hasNewValue = true;
                        if(tileRotate != null)
                        {
                            hasNewValue = tileRotate(newValue, out newValue);
                        }
                        values[newX, newY] = newValue;
                        mask[newIndex] = hasNewValue && original.Topology.ContainsIndex(original.Topology.GetIndex(x, y, 0));
                    }
                }
            }

            return new TopArray2D<T>(values, topology);
        }
    }
}
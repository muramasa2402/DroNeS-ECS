﻿using System;
using System.Collections.Generic;
using DroNeS.Mapbox.Interfaces;
using Mapbox.Map;
using Mapbox.Utils;
using Unity.Mathematics;

// ReSharper disable HeapView.ObjectAllocation.Evident

namespace DroNeS.Mapbox.Custom
{
    public static class ManhattanTileProvider
    {
        public static HashSet<UnwrappedTileId> Tiles { get; } = new HashSet<UnwrappedTileId>();
        
        private static readonly float2[] West = {
            new float2(40.83f,-73.958f),
            new float2(40.814f,-73.963f),
            new float2(40.807f,-73.974f),
            new float2(40.804f,-73.974f),
            new float2(40.796f,-73.981f),
            new float2(40.792f,-73.986f),
            new float2(40.788f,-73.986f),
            new float2(40.783f,-73.990f),
            new float2(40.775f,-73.995f),
            new float2(40.767f,-74.00f),
            new float2(40.762f,-74.01f),
            new float2(40.76f,-74.01f),
            new float2(40.757f,-74.01f),
            new float2(40.753f,-74.01f),
            new float2(40.745f,-74.018f),
            new float2(40.734f,-74.018f),
            new float2(40.71f,-74.02f),
            new float2(40.707f,-74.025f),
            new float2(40.705f,-74.023f),
            new float2(40.702f,-74.025f),
            new float2(40.699f,-74.025f)};
        private static readonly float2[] East = {
            new float2(40.83f,-73.934f),
            new float2(40.814f,-73.934f),
            new float2(40.807f,-73.937f),
            new float2(40.804f,-73.937f),
            new float2(40.796f,-73.928f),
            new float2(40.792f,-73.936f),
            new float2(40.788f,-73.945f),
            new float2(40.783f,-73.940f),
            new float2(40.775f,-73.940f),
            new float2(40.767f,-73.953f),
            new float2(40.762f,-73.958f),
            new float2(40.76f,-73.963f),
            new float2(40.757f,-73.97f),
            new float2(40.753f,-73.964f),
            new float2(40.745f,-73.967f),
            new float2(40.734f,-73.972f),
            new float2(40.71f,-73.971f),
            new float2(40.707f,-73.992f),
            new float2(40.705f,-74.003f),
            new float2(40.702f,-74.003f),
            new float2(40.699f,-74.003f)};

        public static HashSet<UnwrappedTileId> GetTiles(IMap map)
        {
            return Actual(map);
        }

        private static HashSet<UnwrappedTileId> Actual(IMap map)
        {
            var n = West.Length;
            for (var i = 0; i < n; i++)
            {
                var j = i + (i != n - 1 ? 1 : 0);
                var xy0 = TileId(West[i], map.AbsoluteZoom);
                var yn = TileId(West[j], map.AbsoluteZoom).Y;
                var xn = TileId(East[i], map.AbsoluteZoom).X;
                
                for (var y = xy0.Y; y <= yn; y++)
                {
                    for (var x = xy0.X; x <= xn; x++)
                    {
                        Tiles.Add(new UnwrappedTileId(map.AbsoluteZoom, x, y));
                    }
                }
            }
            return Tiles;
        }

        private static HashSet<UnwrappedTileId> Test(IMap map)
        {
            var latLng = new Vector2d(map.CenterLatitudeLongitude.x, map.CenterLatitudeLongitude.y);
            var centerTile = TileCover.CoordinateToTileId(latLng, map.AbsoluteZoom);
            Tiles.Add(new UnwrappedTileId(map.AbsoluteZoom, centerTile.X, centerTile.Y));

            for (var x = (centerTile.X - 1); x <= (centerTile.X + 1); x++)
            {
                for (var y = (centerTile.Y - 1); y <= (centerTile.Y + 1); y++)
                {
                    Tiles.Add(new UnwrappedTileId(map.AbsoluteZoom, x, y));
                }
            }

            return Tiles;
        }

        private static UnwrappedTileId TileId(float2 coord, int zoom)
        {
            var lat = coord.x;
            var lng = coord.y;
            
            var x = (int)Math.Floor((lng + 180.0) / 360.0 * Math.Pow(2.0, zoom));
            var y = (int)Math.Floor((1.0 - Math.Log(Math.Tan(lat * Math.PI / 180.0)
                                                    + 1.0 / Math.Cos(lat * Math.PI / 180.0)) / Math.PI) / 2.0 * Math.Pow(2.0, zoom));

            return new UnwrappedTileId(zoom, x, y);
        }
    }
}

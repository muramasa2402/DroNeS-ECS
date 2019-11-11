﻿using System.Collections.Generic;
using Mapbox.Unity.MeshGeneration.Data;
using Mapbox.Unity.Utilities;
using Mapbox.Utils;
using Mapbox.VectorTile;
using Mapbox.VectorTile.Geometry;
using UnityEngine;

namespace DroNeS.Mapbox.ECS
{
    public class CustomFeatureUnity
	{
		public VectorTileFeature Data;
		public Dictionary<string, object> Properties;
		public readonly List<List<Vector3>> Points = new List<List<Vector3>>();
		public CustomTile Tile;

		private double _rectSizex;
		private double _rectSizey;
		private int _geomCount;
		private int _pointCount;
		private List<Vector3> _newPoints = new List<Vector3>();
		private List<List<Point2d<float>>> _geom;

		public CustomFeatureUnity()
		{
			Points = new List<List<Vector3>>();
		}

		public CustomFeatureUnity(VectorTileFeature feature, CustomTile tile, float layerExtent, bool buildingsWithUniqueIds = false)
		{
			Data = feature;
			Properties = Data.GetProperties();
			Points.Clear();
			Tile = tile;

			//this is a temp hack until we figure out how streets ids works
			_geom = buildingsWithUniqueIds ? feature.Geometry<float>() : feature.Geometry<float>(0);

			_rectSizex = tile.Rect.Size.x;
			_rectSizey = tile.Rect.Size.y;

			_geomCount = _geom.Count;
			for (var i = 0; i < _geomCount; i++)
			{
				_pointCount = _geom[i].Count;
				_newPoints = new List<Vector3>(_pointCount);
				for (var j = 0; j < _pointCount; j++)
				{
					var point = _geom[i][j];
					_newPoints.Add(new Vector3((float)(point.X / layerExtent * _rectSizex - (_rectSizex / 2)) * tile.TileScale, 0, (float)((layerExtent - point.Y) / layerExtent * _rectSizey - (_rectSizey / 2)) * tile.TileScale));
				}
				Points.Add(_newPoints);
			}
		}

		public CustomFeatureUnity(VectorTileFeature feature, List<List<Point2d<float>>> geom, CustomTile tile, float layerExtent, bool buildingsWithUniqueIds = false)
		{
			Data = feature;
			Properties = Data.GetProperties();
			Points.Clear();
			Tile = tile;
			_geom = geom;

			_rectSizex = tile.Rect.Size.x;
			_rectSizey = tile.Rect.Size.y;

			_geomCount = _geom.Count;
			for (var i = 0; i < _geomCount; i++)
			{
				_pointCount = _geom[i].Count;
				_newPoints = new List<Vector3>(_pointCount);
				for (var j = 0; j < _pointCount; j++)
				{
					var point = _geom[i][j];
					_newPoints.Add(new Vector3((float)(point.X / layerExtent * _rectSizex - (_rectSizex / 2)) * tile.TileScale, 0, (float)((layerExtent - point.Y) / layerExtent * _rectSizey - (_rectSizey / 2)) * tile.TileScale));
				}
				Points.Add(_newPoints);
			}
		}

		public bool ContainsLatLon(Vector2d coord)
		{
			//first check tile
			var coordinateTileId = Conversions.LatitudeLongitudeToTileId(
				coord.x, coord.y, Tile.CurrentZoom);

			if (Points.Count > 0)
			{
				var from = Conversions.LatLonToMeters(coord.x, coord.y);

				var to = new Vector2d((Points[0][0].x / Tile.TileScale) + Tile.Rect.Center.x, (Points[0][0].z / Tile.TileScale) + Tile.Rect.Center.y);
				var dist = Vector2d.Distance(from, to);
				if (Mathd.Abs(dist) < 50)
				{
					return true;
				}
			}

			if ((!coordinateTileId.Canonical.Equals(Tile.CanonicalTileId)))
			{
				return false;
			}

			//then check polygon
			var point = Conversions.LatitudeLongitudeToVectorTilePosition(coord, Tile.CurrentZoom);
			var output = PolygonUtils.PointInPolygon(new Point2d<float>(point.x, point.y), _geom);

			return output;
		}

		public static explicit operator VectorFeatureUnity(CustomFeatureUnity data)
		{
			return new VectorFeatureUnity
			{
				Properties = data.Properties,
				Points = data.Points
			};
		}
	}
}

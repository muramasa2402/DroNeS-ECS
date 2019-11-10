﻿using System;
using System.Collections.Generic;
using DroNeS.Mapbox;
using Mapbox.Map;
using Mapbox.Unity;
using Mapbox.Unity.Map;
using Mapbox.Unity.Map.Interfaces;
using Mapbox.Unity.Map.Strategies;
using Mapbox.Unity.Map.TileProviders;
using Mapbox.Unity.Utilities;
using Mapbox.Utils;
using UnityEngine;

namespace DroNeS
{
    [Serializable]
    public class DronesMap : IMap
    {
        #region Private Fields
        [SerializeField] private MapOptions _options = new MapOptions();
        private MapboxAccess _fileSource;
        #endregion
        
        #region IMap Properties
        public Vector2d CenterMercator { get; private set; }
        public float WorldRelativeScale { get; private set; }
        public Vector2d CenterLatitudeLongitude { get; private set; }
        public float Zoom => _options.locationOptions.zoom;
        public int InitialZoom => 16;

        public int AbsoluteZoom => (int) Math.Floor(Zoom);

        public Transform Root { get; }
        public float UnityTileSize { get; }
        public Texture2D LoadingTexture { get; }
        public Material TileMaterial { get; }
        public HashSet<UnwrappedTileId> CurrentExtent { get; }
        #endregion

        public static void Build()
        {
            var c = new DronesMap();
        }
        private DronesMap()
        {
            _options.locationOptions.zoom = 16;
            MapOnStartRoutine();
        }
        
        private ManhattanVisualizer Visualizer { get; set; }
        public event Action OnInitialized;
        public event Action OnUpdated;
        public Vector2d WorldToGeoPosition(Vector3 point)
        {
            var sf = Mathf.Pow(2, (InitialZoom - AbsoluteZoom));

            return Vector3.zero.GetGeoPosition(CenterMercator, WorldRelativeScale * sf);
        }
        public Vector3 GeoToWorldPosition(Vector2d latitudeLongitude, bool queryHeight = true) => Vector3.zero;

        public void SetCenterMercator(Vector2d centerMercator)
        {
            CenterMercator = centerMercator;
        }

        public void SetCenterLatitudeLongitude(Vector2d centerLatitudeLongitude)
        {
            _options.locationOptions.latitudeLongitude = $"{centerLatitudeLongitude.x}, {centerLatitudeLongitude.y}";
            CenterLatitudeLongitude = centerLatitudeLongitude;
        }

        public void SetWorldRelativeScale(float scale)
        {
            WorldRelativeScale = scale;
        }

        public void SetZoom(float zoom) { }

        public void UpdateMap(Vector2d latLon, float zoom) { }

        public void ResetMap()
        {
            MapOnStartRoutine(false);
        }

        private void MapOnStartRoutine(bool coroutine = true)
        {
            if (!Application.isPlaying) return;
            SetUpMap();
        }
        
        private void SetUpMap()
        {
            _options.scalingOptions.scalingStrategy = new MapScalingAtWorldScaleStrategy();
            _options.placementOptions.placementStrategy = new MapPlacementAtTileCenterStrategy();
            
            InitializeMap(_options);
        }

        private void InitializeMap(MapOptions options)
        {
            _fileSource = MapboxAccess.Instance;
            CenterLatitudeLongitude = Conversions.StringToLatLon("40.764170691358686, -73.97670925665614");
            SetWorldRelativeScale(Mathf.Pow(2, AbsoluteZoom - InitialZoom) * Mathf.Cos(Mathf.Deg2Rad * (float)CenterLatitudeLongitude.x));
            SetCenterMercator(Conversions.TileBounds(TileCover.CoordinateToTileId(CenterLatitudeLongitude, AbsoluteZoom)).Center);

            Visualizer = new ManhattanVisualizer(this);

            ManhattanTileProvider.Initialize(this, TriggerTileRedrawForExtent);
        }
        
        private void TriggerTileRedrawForExtent(ExtentArgs currentExtent)
        {
            foreach (var tileId in currentExtent.activeTiles)
            {
                Visualizer.LoadTile(tileId);
            }
        }

    }
}

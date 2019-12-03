﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DroNeS.Mapbox.Interfaces;
using Mapbox.Unity.Map;
using Mapbox.Unity.MeshGeneration.Data;
using Mapbox.Unity.MeshGeneration.Enums;
using Mapbox.Unity.MeshGeneration.Filters;
using Mapbox.VectorTile;
using Mapbox.VectorTile.Geometry;
using UnityEngine;

namespace DroNeS.Mapbox.Custom
{
    public class ParallelMeshBuilder : IMeshBuilder
    {
        public VectorSubLayerProperties SubLayerProperties { get; }
        public IMeshProcessor Processor => _processor;
        private readonly ParallelMeshProcessor _processor;
        private readonly GeometryExtrusionWithAtlasOptions _atlasOptions;
        private readonly UVModifierOptions _uvOptions;

        public ParallelMeshBuilder(VectorSubLayerProperties subLayerProperties, IMeshProcessor processor)
        {
            SubLayerProperties = subLayerProperties;
            _processor = processor as ParallelMeshProcessor ?? throw new ArgumentException($"Expected {_processor.GetType().Name}");

            SubLayerProperties.materialOptions.SetDefaultMaterialOptions();
			
            _uvOptions = new UVModifierOptions
            {
                texturingType = UvMapType.Atlas,
                atlasInfo = Resources.Load("Atlases/BuildingAtlas") as AtlasInfo,
                style = StyleTypes.Custom
            };
            
            SubLayerProperties.extrusionOptions.extrusionType = ExtrusionType.PropertyHeight;
            SubLayerProperties.extrusionOptions.extrusionScaleFactor = 1.3203f;
            SubLayerProperties.extrusionOptions.propertyName = "height";
            SubLayerProperties.extrusionOptions.extrusionGeometryType = ExtrusionGeometryType.RoofAndSide;
            
            _atlasOptions = new GeometryExtrusionWithAtlasOptions(SubLayerProperties.extrusionOptions, _uvOptions);

            SubLayerProperties.filterOptions.RegisterFilters();
        }

        public void Create(VectorTileLayer layer, CustomTile tile)
        {
            if (tile == null || layer == null) return;

            CoroutineManager.Run(ProcessLayer(MakeProperties(layer), tile));
        }
        private BuildingMeshBuilderProperties MakeProperties(VectorTileLayer layer)
        {
            var output = new BuildingMeshBuilderProperties
            {
                VectorTileLayer = layer,
                FeatureCount = layer?.FeatureCount() ?? 0,
                FeatureProcessingStage = FeatureProcessingStage.PreProcess,
                LayerFeatureFilters =
                    SubLayerProperties.filterOptions.filters.Select(m => m.GetFilterComparer()).ToArray(),
                LayerFeatureFilterCombiner = new LayerFilterComparer()
            };
            switch (SubLayerProperties.filterOptions.combinerType)
            {
                case LayerFilterCombinerOperationType.Any:
                    output.LayerFeatureFilterCombiner = LayerFilterComparer.AnyOf(output.LayerFeatureFilters);
                    break;
                case LayerFilterCombinerOperationType.All:
                    output.LayerFeatureFilterCombiner = LayerFilterComparer.AllOf(output.LayerFeatureFilters);
                    break;
                case LayerFilterCombinerOperationType.None:
                    output.LayerFeatureFilterCombiner = LayerFilterComparer.NoneOf(output.LayerFeatureFilters);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            output.BuildingsWithUniqueIds = SubLayerProperties.honorBuildingIdSetting && SubLayerProperties.buildingsWithUniqueIds;
            return output;
        }

        private IEnumerator ProcessLayer(BuildingMeshBuilderProperties properties, CustomTile tile)
        {
            for (var i = 0; i < properties.FeatureCount; ++i)
            {
                ProcessFeature(i, tile, properties);
                yield return null;
            }
        }
        
        private void ProcessFeature(int index, CustomTile tile, BuildingMeshBuilderProperties layerProperties)
        {
            var layerExtent = layerProperties.VectorTileLayer.Extent;
            var fe = layerProperties.VectorTileLayer.GetFeature(index);
            List<List<Point2d<float>>> geom;
			
            if (layerProperties.BuildingsWithUniqueIds)
            {
                geom = fe.Geometry<float>(); 

                if (geom[0][0].X < 0 || geom[0][0].X > layerExtent || geom[0][0].Y < 0 || geom[0][0].Y > layerExtent) return;
            }
            else
            {
                geom = fe.Geometry<float>(0); //passing zero means clip at tile edge
            }

            var feature = new CustomFeatureUnity(
                layerProperties.VectorTileLayer.GetFeature(index),
                geom,
                tile,
                layerProperties.VectorTileLayer.Extent,
                layerProperties.BuildingsWithUniqueIds);


            if (!IsFeatureEligibleAfterFiltering(feature, layerProperties) ||
                tile == null || tile.VectorDataState == TilePropertyState.Cancelled) return;
            
            if (feature.Properties.ContainsKey("extrude") && !Convert.ToBoolean(feature.Properties["extrude"])) return;
            if (feature.Points.Count < 1) return;
            
            _processor.Execute(tile, feature, _uvOptions, _atlasOptions);
            
        }
        
        private static bool IsFeatureEligibleAfterFiltering(CustomFeatureUnity feature, BuildingMeshBuilderProperties layerProperties)
        {
            return layerProperties.LayerFeatureFilters.Length < 1 || layerProperties.LayerFeatureFilterCombiner.Try((VectorFeatureUnity)feature);
        }
        
        
    }
}

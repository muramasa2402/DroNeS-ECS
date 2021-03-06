﻿using System.Collections.Generic;
using DroNeS.Mapbox.Custom.Parallel;
using DroNeS.Mapbox.Interfaces;
using Mapbox.Unity.Map;
using Mapbox.Unity.MeshGeneration.Data;
using Mapbox.Unity.MeshGeneration.Enums;
using Mapbox.Unity.MeshGeneration.Modifiers;
using Unity.Rendering;
using UnityEngine;

namespace DroNeS.Mapbox.Custom
{
	public class MeshProcessor : IMeshProcessor
    {
	    private readonly Dictionary<CustomTile, MeshData> _accumulation = new Dictionary<CustomTile, MeshData>();
	    private readonly MeshModifier[] _modifiers;
	    private readonly Dictionary<CustomTile, int> _indices = new Dictionary<CustomTile, int>();
	    private Material _buildingMaterial;

	    public void SetOptions(UVModifierOptions uvOptions, GeometryExtrusionWithAtlasOptions extrusionOptions)
	    {
		    _modifiers[0].SetProperties(uvOptions);
		    _modifiers[1].SetProperties(extrusionOptions);
		    _modifiers[0].Initialize();
		    _modifiers[1].Initialize();
	    }

	    public Material BuildingMaterial
	    {
		    get
		    {
			    if (_buildingMaterial == null) _buildingMaterial = Resources.Load("Materials/BuildingMaterial") as Material;
			    return _buildingMaterial;
		    }
	    }

	    public MeshProcessor()
		{
			_modifiers = new[]
			{
				(MeshModifier)ScriptableObject.CreateInstance<StrippedPolygonMeshModifier>(),
				ScriptableObject.CreateInstance<StrippedTextureSideWallModifier>(),
			};
		}
	    public void Execute(CustomTile tile, CustomFeatureUnity feature)
		{
			var meshData = new MeshData{TileRect = tile.Rect};
		    if (!_accumulation.ContainsKey(tile))
		    {
			    _accumulation.Add(tile, new MeshData
			    {
					Edges = new List<int>(),
					Normals = new List<Vector3>(),
					Tangents = new List<Vector4>(),
					Triangles = new List<List<int>>{new List<int>()},
					UV = new List<List<Vector2>>{new List<Vector2>()},
					Vertices = new List<Vector3>()
			    });
			    _indices.Add(tile, 0);
		    }

		    foreach (var modifier in _modifiers)
		    {
			    modifier.Run((VectorFeatureUnity)feature, meshData);
		    }

		    if (_accumulation[tile].Vertices.Count + meshData.Vertices.Count < 65000)
		    {
			    Append(tile, meshData);
		    }
		    else
		    {
			    Terminate(tile, meshData);
		    }
	    }
	    private void Append(CustomTile tile, MeshData data)
	    {
		    if (!_accumulation.TryGetValue(tile, out var value))
		    {
			    value = _accumulation[tile] = data;
		    }
		    if (data.Vertices.Count <= 3) return;
		    var st = value.Vertices.Count;
		    value.Vertices.AddRange(data.Vertices);
		    value.Normals.AddRange(data.Normals);
		    
		    for (var j = 0; j < data.UV.Count; j++)
		    {
			    if (value.UV.Count <= j)
			    {
				    value.UV.Add(new List<Vector2>(data.UV[j].Count));
			    }
		    }
		    
		    for (var j = 0; j < data.UV.Count; j++)
		    {
			    value.UV[j].AddRange(data.UV[j]);
		    }
		    
		    for (var j = 0; j < data.Triangles.Count; j++)
		    {
			    if (value.Triangles.Count <= j)
			    {
				    value.Triangles.Add(new List<int>(data.Triangles[j].Count));
			    }
		    }
		    
		    for (var j = 0; j < data.Triangles.Count; j++)
		    {
			    for (var k = 0; k < data.Triangles[j].Count; k++)
			    {
				    value.Triangles[j].Add(data.Triangles[j][k] + st);
			    }
		    }
		    
	    }
	    private void MakeEntity(CustomTile tile, MeshData value)
	    {
		    var go = new GameObject($"Building {_indices[tile]++.ToString()}");
		    go.transform.position = tile.Position;
		    go.transform.SetParent(tile.Transform, true);
		    
		    var filter = go.AddComponent<MeshFilter>();
		    filter.sharedMesh = new Mesh();
		    go.AddComponent<MeshRenderer>().sharedMaterial = BuildingMaterial;
		    
		    filter.sharedMesh.subMeshCount = value.Triangles.Count;
		    filter.sharedMesh.SetVertices(value.Vertices);
		    filter.sharedMesh.SetNormals(value.Normals);

		    for (var i = 0; i < value.Triangles.Count; i++)
		    {
			    filter.sharedMesh.SetTriangles(value.Triangles[i], i);
		    }

		    for (var i = 0; i < value.UV.Count; i++)
		    {
			    filter.sharedMesh.SetUVs(i, value.UV[i]);
		    }
		    go.layer = LayerMask.NameToLayer("Buildings");
	    }
	    private void Terminate(CustomTile tile, MeshData data)
	    {
		    if (!_accumulation.TryGetValue(tile, out var value) || value.Vertices.Count <= 3) return;
		    
		    MakeEntity(tile, value);
		    _accumulation[tile] = data;
	    }
	    
	    public void Terminate(CustomTile tile)
	    {
		    if (_accumulation.TryGetValue(tile, out var value) && value.Vertices.Count > 3)
		    {
			    MakeEntity(tile, value);
		    }
		    
		    tile.VectorDataState = TilePropertyState.Loaded;
		    _accumulation.Remove(tile);
	    }
	    
    }
}

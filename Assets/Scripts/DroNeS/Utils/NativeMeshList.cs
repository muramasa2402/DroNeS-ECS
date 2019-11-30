﻿using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;

// ReSharper disable MemberCanBePrivate.Global

// ReSharper disable InconsistentNaming

namespace DroNeS.Utils
{
    [StructLayout(LayoutKind.Sequential)]
    [NativeContainer]
    [DebuggerDisplay("Length = {Length}")]
    [DebuggerTypeProxy(typeof(NativeMeshListDebugView))]
    public unsafe struct NativeMeshList : IDisposable
    {
        private static readonly int MaxMeshSize = 130000 * sizeof(Vector3) + 195000 * sizeof(int) + 65000 * sizeof(Vector2);
        
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal AtomicSafetyHandle m_Safety;

        [NativeSetClassTypeToNullOnSchedule] 
        private DisposeSentinel m_DisposeSentinel;
#endif

        [NativeDisableUnsafePtrRestriction]
        internal UnsafeList* m_meshes;

        internal Allocator m_allocator;


        internal struct NativeMeshElement
        {
            [NativeDisableUnsafePtrRestriction]
            public UnsafeList* m_vertices;
            [NativeDisableUnsafePtrRestriction]
            public UnsafeList* m_normals;
            [NativeDisableUnsafePtrRestriction]
            public UnsafeList* m_triangles;
            [NativeDisableUnsafePtrRestriction]
            public UnsafeList* m_uv;

            public NativeMeshElement(NativeMesh mesh, Allocator allocator)
            {
                var length = mesh.m_vertices->Length;
                m_vertices = UnsafeList.Create(UnsafeUtility.SizeOf<Vector3>(), UnsafeUtility.AlignOf<Vector3>(), length, allocator);
                UnsafeUtility.MemCpy(m_vertices, mesh.m_vertices, (long)sizeof(Vector3) * length);

                length = mesh.m_normals->Length;
                m_normals = UnsafeList.Create(UnsafeUtility.SizeOf<Vector3>(), UnsafeUtility.AlignOf<Vector3>(), length, allocator);
                UnsafeUtility.MemCpy(m_normals, mesh.m_vertices, (long)sizeof(Vector3) * length);
                
                length = mesh.m_triangles->Length;
                m_triangles = UnsafeList.Create(UnsafeUtility.SizeOf<int>(), UnsafeUtility.AlignOf<int>(), length, allocator);
                UnsafeUtility.MemCpy(m_triangles, mesh.m_vertices, (long)sizeof(int) * length);
                
                length = mesh.m_uv->Length;
                m_uv = UnsafeList.Create(UnsafeUtility.SizeOf<Vector2>(), UnsafeUtility.AlignOf<Vector2>(), length, allocator);
                UnsafeUtility.MemCpy(m_uv, mesh.m_vertices, (long)sizeof(Vector2) * length);
            }
        }

        public NativeMeshList(Allocator allocator)
            : this(1, allocator, 2)
        {
        }

        public NativeMeshList(int initialCapacity, Allocator allocator)
            : this(initialCapacity, allocator, 2)
        {
        }

        private NativeMeshList(int meshes, Allocator allocator,
            int disposeSentinelStackDepth)
        {
            var totalSize = (long)MaxMeshSize * meshes;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            // Native allocation is only valid for Temp, Job and Persistent.
            if (allocator <= Allocator.None)
                throw new ArgumentException("Allocator must be Temp, TempJob or Persistent", nameof(allocator));
            if (totalSize > int.MaxValue)
                throw new ArgumentOutOfRangeException($"Capacity has exceeded {int.MaxValue} bytes");

            DisposeSentinel.Create(out m_Safety, out m_DisposeSentinel, disposeSentinelStackDepth, allocator);
#endif
            m_meshes = UnsafeList.Create(UnsafeUtility.SizeOf<NativeMeshElement>(), UnsafeUtility.AlignOf<NativeMeshElement>(), meshes, allocator);
            
            m_allocator = allocator;

#if UNITY_2019_3_OR_NEWER && ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.SetBumpSecondaryVersionOnScheduleWrite(m_Safety, true);
#endif
        }
        
        public NativeMesh this[int index]
        {
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
                if ((uint)index >= (uint)m_meshes->Length)
                    throw new IndexOutOfRangeException($"Index {index} is out of range in NativeList of '{m_meshes->Length}' Length.");
#endif
                var element = UnsafeUtility.ReadArrayElement<NativeMeshElement>(m_meshes->Ptr, index);
                return AsNativeMesh(element);
            }
            set
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
                if ((uint)index >= (uint)m_meshes->Length)
                    throw new IndexOutOfRangeException($"Index {index} is out of range in NativeList of '{m_meshes->Length}' Length.");
#endif
                UnsafeUtility.WriteArrayElement(m_meshes->Ptr, index, new NativeMeshElement(value, m_allocator));
            }
        }
        
        public int Length
        {
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                return m_meshes->Length;
            }
        }
        
        public int Capacity
        {
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                return m_meshes->Capacity;
            }

            set
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Safety);
                if (value < m_meshes->Length)
                    throw new ArgumentException("Capacity must be larger than the length of the NativeList.");
#endif
                m_meshes->SetCapacity<NativeMeshElement>(value);
            }
        }
        
        public void Add(NativeMesh element)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Safety);
#endif
            m_meshes->Add(new NativeMeshElement(element, m_allocator));
        }
        
        public void RemoveAtSwapBack(int index)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Safety);

            if (index < 0 || index >= Length)
                throw new ArgumentOutOfRangeException(index.ToString());
#endif
            m_meshes->RemoveAtSwapBack<NativeMeshElement>(index);
        }
        
        public bool IsCreated => m_meshes != null;
        
        private void Deallocate()
        {
            var l = Length;

            for (var i = 0; i < l; ++i)
            {
                Deallocate(i);
            }
            UnsafeList.Destroy(m_meshes);
            m_meshes = null;
        }

        private void Deallocate(int index)
        {
            var element = UnsafeUtility.ReadArrayElement<NativeMeshElement>(m_meshes, index);
            UnsafeList.Destroy(element.m_normals);
            UnsafeList.Destroy(element.m_triangles);
            UnsafeList.Destroy(element.m_vertices);
            UnsafeList.Destroy(element.m_uv);
                
            element.m_triangles = null;
            element.m_vertices = null;
            element.m_normals = null;
            element.m_uv = null;
        }

        public void Dispose()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            DisposeSentinel.Dispose(ref m_Safety, ref m_DisposeSentinel);
#endif
            Deallocate();
        }
        
        public JobHandle Dispose(JobHandle inputDeps)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            DisposeSentinel.Clear(ref m_DisposeSentinel);
#endif
            var jobHandle = new DisposeJob { Container = this }.Schedule(
                new DisposeElementsJob{Container = this}.Schedule(Length, 2, inputDeps));

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.Release(m_Safety);
#endif
            m_meshes = null;

            return jobHandle;
        }
        
        [BurstCompile]
        private struct DisposeElementsJob : IJobParallelFor
        {
            public NativeMeshList Container;

            public void Execute(int index)
            {
                Container.Deallocate(index);
            }
        }
        
        [BurstCompile]
        private struct DisposeJob : IJob
        {
            public NativeMeshList Container;

            public void Execute()
            {
                UnsafeList.Destroy(Container.m_meshes);
                Container.m_meshes = null;
            }
        }
        
        public void Clear()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Safety);
#endif
            var l = Length;

            for (var i = 0; i < l; ++i)
            {
                Clear(i);
            }
            m_meshes->Clear();
        }

        private void Clear(int index)
        {
            var element = UnsafeUtility.ReadArrayElement<NativeMeshElement>(m_meshes, index);
            element.m_triangles->Clear();
            element.m_vertices->Clear();
            element.m_normals->Clear();
            element.m_uv->Clear();
        }

        private NativeMesh AsNativeMesh(NativeMeshElement element)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckGetSecondaryDataPointerAndThrow(m_Safety);
            var safety = m_Safety;
            AtomicSafetyHandle.UseSecondaryVersion(ref safety);
#endif

            return new NativeMesh
            {
                m_allocator = Allocator.Invalid,
                m_triangles = element.m_triangles,
                m_normals = element.m_normals,
                m_vertices = element.m_vertices,
                m_uv = element.m_uv,
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                m_Safety = safety
#endif
            };
        }

        public Mesh AsMesh(int index) => this[index].AsMesh();
    }

    internal struct NativeMeshListDebugView
    {
        private NativeMeshList m_List;

        public NativeMeshListDebugView(NativeMeshList meshList)
        {
            m_List = meshList;
        }
    }
}

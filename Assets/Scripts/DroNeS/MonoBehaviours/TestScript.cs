﻿using System;
using System.Collections;
using DroNeS.Utils;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace DroNeS.MonoBehaviours
{
    public class TestScript : MonoBehaviour
    {
        private unsafe void Start()
        {
            var array = new int[1];
            array[0] = 123123;
            
            var job = new TestJob2();
            job.Integer = (int*)UnsafeUtility.PinGCArrayAndGetDataAddress(array, out job.GcHandle);
            var handle = job.Schedule();
            handle.Complete();
            
            Debug.Log(array[0]);

        }
    }

    public unsafe struct TestJob2 : IJob
    {
        [NativeDisableUnsafePtrRestriction] 
        public int* Integer;
        
        public ulong GcHandle;
        public void Execute()
        {
            *Integer = 5;
            UnsafeUtility.ReleaseGCObject(GcHandle);
        }
    }
    
    [BurstCompile]
    public unsafe struct TestJob : IJob
    {
        public void Execute()
        {
            var points = new NativeList<float3>(12, Allocator.Temp)
            {
                float3.zero, 
                new float3(1, 1, 1),
                new float3(1, 2, 3),
            };
            
            points.Add(new float3(12,12,12));
            var zeroth = (float3*) points.GetUnsafePtr();
            var fourth = (float3*) ((IntPtr) points.GetUnsafePtr() + (int) 3 * sizeof(float3));

            var tmp = *zeroth;
            *zeroth = *fourth;
            *fourth = tmp;
            
        }

        private static void Add(float3 point, ref NativeList<float3> points)
        {
            points.Add(point);
        }
    }

}
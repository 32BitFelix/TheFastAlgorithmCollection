


using System.Net.Security;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using Core.MemoryManagement;

namespace Core.Algorithms;


public struct EntityPoint
{
    public int Entity;

    public float Point;
}


// A helper class for sporting
// sorting algorithms

public unsafe static class Sorting
{


    // A slightly less space efficient
    // approach to radix sort, inspired
    // by Michael Herf's approach

    public static void RadixSortHistogram(int* array)
    {
        // This and the given array will
        // be used interchangeably for storing
        // the results of each radix

        int* swapBuffer = (int*)NativeMemory.Alloc((nuint)(sizeof(int) * CompactArray.Length(array)));


        int* histogram0 = stackalloc int[256];

        histogram0[0] = -1;

        int* histogram1 = stackalloc int[256];

        histogram1[0] = -1;

        int* histogram2 = stackalloc int[256];

        histogram2[0] = -1;

        int* histogram3 = stackalloc int[256];

        histogram3[128] = -1;


        Sse.PrefetchNonTemporal(array);

        // Set the records of the histograms

        for(int i = CompactArray.Length(array) - 1; i > -1; i--)
        {
            int val = array[i];


            histogram0[(byte)val]++;

            val >>= 8;


            histogram1[(byte)val]++;

            val >>= 8;


            histogram2[(byte)val]++;

            val >>= 8;


            histogram3[(byte)val]++;
        }


        // Sum the records of the histograms
        // with their previous ones

        for(int i = 1; i < 256; i++)
        {
            histogram0[i] += histogram0[i - 1];

            histogram1[i] += histogram1[i - 1];

            histogram2[i] += histogram2[i - 1];

            histogram3[(byte)(i + 128)] += histogram3[(byte)((byte)(i + 128) - 1)];
        }


        Sse.PrefetchNonTemporal(array);

        Sse.PrefetchNonTemporal(swapBuffer);


        // The first pass

        Sse.Prefetch0(histogram0);

        for(int i = CompactArray.Length(array) - 1; i > -1; i--)
        {
            byte val = (byte)array[i];

            swapBuffer[histogram0[val]--] = array[i];
        }

        // The second pass

        Sse.Prefetch0(histogram1);

        for(int i = CompactArray.Length(array) - 1; i > -1; i--)
        {
            byte val = (byte)(swapBuffer[i] >> 8);

            array[histogram1[val]--] = swapBuffer[i];
        }

        // The third pass

        Sse.Prefetch0(histogram2);

        for(int i = CompactArray.Length(array) - 1; i > -1; i--)
        {
            byte val = (byte)(array[i] >> 16);

            swapBuffer[histogram2[val]--] = array[i];
        }

        // The fourth pass

        Sse.Prefetch0(histogram3);

        for(int i = CompactArray.Length(array) - 1; i > -1; i--)
        {
            byte val = (byte)(swapBuffer[i] >> 24);

            array[histogram3[val]--] = swapBuffer[i];
        }
    }


    // Sorts the given compact array
    // with the radix sorting algorithm

    public static void RadixSort(int* array)
    {

        // This and the given array will
        // be used interchangeably for storing
        // the results of each radix

        int* swapBuffer = (int*)NativeMemory.Alloc((nuint)(sizeof(int) * CompactArray.Length(array)));


        // Initialise the counter for keeping
        // track of multiple of a certain byte value
        
        int* counter = stackalloc int[256];

        // Initialise the table for keeping
        // track of at what index the values
        // will be set for each iteration

        int* offsetTable = stackalloc int[256];    


        // The loop to iterate through each pass

        for(byte p = 0; p < 3; p++)
        {
            // Reset the counter

            for(byte i = 0; i < 256 / 2; i++)
                ((long*)counter)[i] ^= ((long*)counter)[i];


            // Reset the offset table

            offsetTable[0] ^= offsetTable[0];


            // Evaluate the source and target array

            int* source;

            int* target;

            {
                bool swapIndicator = (p & 1) == 0;

                source = swapIndicator ? array : swapBuffer;

                target = swapIndicator ? swapBuffer : array;
            }


            // Count the instances

            for(int i = 0; i < CompactArray.Length(array); i++)
            {
                byte val = (byte)(source[i] >> (8 * p));                

                counter[val]++;
            }


            // Build the offset table

            for(int i = 1; i < 256; i++)
                offsetTable[i] = offsetTable[i - 1] + counter[i - 1];

            
            // Save the values at their
            // orderly index

            for(int i = 0; i < CompactArray.Length(array); i++)
            {
                byte val = (byte)(source[i] >> (8 * p));                

                target[offsetTable[val]++] = source[i];
            }
        }


        // The final pass


        // Reset the counter

        for(byte i = 0; i < 256 / 2; i++)
            ((long*)counter)[i] ^= ((long*)counter)[i];


        // Count the instances

        for(int i = 0; i < CompactArray.Length(array); i++)
        {
            byte val = (byte)(swapBuffer[i] >> 24);                

            counter[val]++;
        }


        // Count the amount of negative values

        int numNeg = 0;

        for(int i = 128; i < 256; i++)
            numNeg += counter[i];

        
        // Reset the offset table
        // for the positive portion

        offsetTable[0] = numNeg;

        // Build the offset table
        // for the positive values

        for(int i = 1; i < 128; i++)
            offsetTable[i] = offsetTable[i - 1] + counter[i - 1];    


        // Reset the offset table
        // for the negative portion

        offsetTable[128] ^= offsetTable[128];

        // Build the offset table
        // for the positive values

        for(int i = 129; i < 256; i++)
            offsetTable[i] = offsetTable[i - 1] + counter[i - 1]; 


        // Save the values at their
        // orderly index

        for(int i = 0; i < CompactArray.Length(array); i++)
        {
            byte val = (byte)(swapBuffer[i] >> 24);                

            array[offsetTable[val]++] = swapBuffer[i];
        }   


        // Free the swapbuffer

        NativeMemory.Free(swapBuffer);
    }
}
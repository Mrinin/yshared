using System.Collections.Generic;
using UnityEngine;
using YShared.MathHelper;

namespace YShared.RandomHelper
{
    
    public static class YRandom
    {
        public static float NormalNumber(
            float mean,
            float standardDeviation,
            float min = float.MinValue,
            float max = float.MaxValue
        )
        {
            float u1 = 1f - UnityEngine.Random.value;
            float u2 = 1f - UnityEngine.Random.value;

            float standardNormal =
                Mathf.Sqrt(-2f * Mathf.Log(u1)) *
                Mathf.Cos(2f * Mathf.PI * u2);

            float random_number = mean + standardNormal * standardDeviation;

            return Mathf.Clamp(random_number, min, max);
        }

        public static int NormalNumber(
            float mean,
            float standardDeviation,
            int min = int.MinValue,
            int max = int.MaxValue
        )
        {
            return (int)NormalNumber(mean, standardDeviation, (float)min, (float)max);
        }

        public static T GetWeightedRandom<T>(
            List<(T item, float weight)> items
        )
        {
            float totalWeight = 0f;

            foreach (var item in items)
                totalWeight += item.weight;

            float randomValue = UnityEngine.Random.value * totalWeight;

            foreach (var item in items)
            {
                randomValue -= item.weight;

                if (randomValue <= 0f)
                    return item.item;
            }

            return items[^1].item;
        }

        public static bool Bool(float true_chance)
        {
            return UnityEngine.Random.Range(0f, 1f) <= true_chance;
        }

        public static int Sign(float plus_chance)
        {
            return UnityEngine.Random.Range(0f, 1f) <= plus_chance ? 1 : -1;
        }

        public static int OneOrZero(float one_chance)
        {
            return UnityEngine.Random.Range(0f, 1f) <= one_chance ? 1 : 0;
        }

        public static T Enum<T>() where T : struct, System.Enum
		{
			T[] arr = (T[])System.Enum.GetValues(typeof(T));
			return arr.PickRandomly();
		}

        public static T Array<T>(T[] arr)
        {
             int ind = UnityEngine.Random.Range(0, arr.Length);
             return arr[ind];
        }
    }
}
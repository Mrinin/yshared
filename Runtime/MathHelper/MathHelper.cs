using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System;
using Unity.Collections;
using UnityEngine;
using System.Linq;

namespace YShared.MathHelper
{
	public static class YMathHelper
	{
		public static readonly string[] indentLevel = new string[] {
			"  ", "    ", "      ", "        ", "          ", "            ", "              "
		};


		public static float Damp(float current, float target, float dampingFactor, float deltaTime)
		{
			// dampingFactor should be > 0 (e.g., 5 or 10)
			// deltaTime is usually Time.deltaTime

			float t = 1 - Mathf.Exp(-dampingFactor * deltaTime);
			return Mathf.Lerp(current, target, t);
		}

		public static Vector2 Damp(Vector2 current, Vector2 target, float dampingFactor, float deltaTime)
		{
			// dampingFactor should be > 0 (e.g., 5 or 10)
			// deltaTime is usually Time.deltaTime

			float t = 1 - Mathf.Exp(-dampingFactor * deltaTime);
			return Vector2.Lerp(current, target, t);
		}
		// Checks if a block is in the boudns oa map.
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsInBounds(this int i, int lower, int upper)
		{
			return i >= lower && i <= upper;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsInBounds(this float i, float lower, float upper)
		{
			return i >= lower && i <= upper;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsInBounds(this Vector2Int i, Vector2Int lower, Vector2Int upper)
		{
			return IsInBounds(i.x, i.y, lower.x, lower.y, upper.x, upper.y);
		}

		public static bool IsInBounds(int x, int y, int low_x, int low_y, int high_x, int high_y)
		{
			return x >= low_x && x <= high_x && y >= low_y && y <= high_y;
		}

		public static Vector3 GetRandomPointInBounds(Vector3 min, Vector3 max)
		{
			return new Vector3(
				UnityEngine.Random.Range(min.x, max.x),
				UnityEngine.Random.Range(min.y, max.y),
				UnityEngine.Random.Range(min.z, max.z)
			);
		}

		public static float GetRandomPoint(this Vector2 v)
		{
			return Mathf.Lerp(v.x, v.y, UnityEngine.Random.Range(0f, 1f));
		}

		public static string V2IString(this Vector2Int a)
		{
			return $"x: {a.x}, y: {a.y}";
		}


		public static byte addBytes(byte a, byte b)
		{
			return (byte)(a + b);
		}

		public static Vector3 V2I_to_V3(this Vector2Int a, float b = 0)
		{
			return new(a.x, a.y, b);
		}

		public static T[] ToRegularArray<T>(this NativeList<T> a) where T : unmanaged
		{
			var res = new T[a.Length];
			for (int i = 0; i < a.Length; i++)
			{
				res[i] = a[i];
			}
			return res;
		}

		public static (float, Vector3) MakeLookAtLocation(UnityEngine.Transform t, Vector3 target, float offset = 0)
		{
			// Calculate the direction to the target
			Vector3 direction = target - t.position;

			// Calculate the angle in degrees
			float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + offset;

			// Set the rotation (only affects Z-axis in 2D)
			t.rotation = Quaternion.Euler(0, 0, angle);
			return (angle, direction);
		}

		public static (float, Vector3) GetLookAngleAndDir(UnityEngine.Transform t, Vector3 target, float offset = 0)
		{
			// Calculate the direction to the target
			Vector3 direction = target - t.position;

			// Calculate the angle in degrees
			float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + offset;

			// Set the rotation (only affects Z-axis in 2D)
			return (angle, direction.normalized);
		}

		public static bool IsInDistance(Vector3 a, Vector3 b, float dst)
		{
			return (a - b).sqrMagnitude <= dst * dst;
		}



		public static float fmod(this float a, float modnum)
		{
			return a - modnum * Mathf.Floor(a / modnum);
		}

		public static int imod(this int x, int m)
		{
			return (x % m + m) % m;
		}

		public static int Floor(this float number)
		{
			return Mathf.FloorToInt(number);
		}

		public static int Ceil(this float number)
		{
			return Mathf.CeilToInt(number);
		}

		public static float Abs(this float number)
		{
			return Mathf.Abs(number);
		}

		public static int Floor(this int number)
		{
			return Mathf.FloorToInt(number);
		}

		public static int Ceil(this int number)
		{
			return Mathf.CeilToInt(number);
		}

		public static int Abs(this int number)
		{
			return Mathf.Abs(number);
		}

		public static float RoundToXDecimals(this float number, int x)
		{
			float m = Mathf.Pow(10, x);
			return Mathf.RoundToInt(number * m) / m;
		}

		public static string RoundToXDecimalsButAsString(this float number, int x)
		{
			return number.ToString($"F{x}").Replace(',', '.');
		}

		public static int Flip(this int number, int origin = 0)
		{
			return origin - (number - origin);
		}

		public static float Flip(this float number, float origin = 0)
		{
			return origin - (number - origin);
		}

		public static float RoundToNearestMultiple(float value, float multiple)
		{
			if (multiple == 0)
				return value; // Avoid division by zero; return the original value.

			return Mathf.Round(value / multiple) * multiple;
		}

		public static int RoundToNearestMultiple(float value, int multiple)
		{
			if (multiple == 0)
				return Mathf.RoundToInt(value); // Avoid division by zero; return the original value.

			return Mathf.RoundToInt(value / multiple) * multiple;
		}

		public static int RoundToNearestMultiple(int value, int multiple)
		{
			if (multiple == 0)
				return value; // Avoid division by zero; return the original value.

			return Mathf.RoundToInt(value / multiple) * multiple;
		}

		// Applies formula x = (at^2) / 2 and solves for t
		public static float ResolveTimeForGivenAccelerationAndDistance(float distance, float acceleration)
		{
			return Mathf.Sqrt((distance * 2) / acceleration);
		}

		public static bool TryAs<T>(this object value, out T casted) where T : class
		{
			if (value is T)
			{
				casted = (T)value;
				return true;
			}
			casted = null;
			return false;
		}

		public static int AsInt0(this bool b)
		{
			return b ? 1 : 0;
		}

		public static int AsIntN1(this bool b)
		{
			return b ? 1 : -1;
		}

		public static T GetRandomEnum<T>() where T : struct, System.Enum
		{
			T[] arr = (T[])System.Enum.GetValues(typeof(T));
			return arr.PickRandomly();
		}
		public static bool IsColorCloseEnough(Color a, Color b, float threshold = 0.1f)
		{
			float distance = Vector3.Distance(
				new Vector3(a.r, a.g, a.b),
				new Vector3(b.r, b.g, b.b)
			);

			return distance <= threshold;
		}

	}
	/// <summary>
	/// Wrapper for a value type to track the changes to it.
	/// T[] values array - index 0 stores the latest version, while history_to_record - 1 stores the oldest stored version.
	/// </summary>
	/// <typeparam name="T"> Type to track. </typeparam>
	public class TrackedVariable<T> where T : struct
	{
		public T[] values;
		int index_at;

		public TrackedVariable(int history_to_record, T initial_value = default)
		{
			values = new T[history_to_record];
			index_at = 0;
			Set(initial_value);
		}

		public void Set(T new_value)
		{
			for (int i = values.Length - 1; i > 0; i--)
			{
				values[i] = values[i - 1];
			}

			values[0] = new_value;
			if (index_at < values.Length - 1)
				index_at += 1;
		}

		public T Get(int point_to_get = 0)
		{
			int ind = Math.Min(point_to_get, index_at - 1);

			return values[ind];
		}

		public T GetFromEnd(int point_to_get = 0)
		{
			int ind = Math.Max(index_at - point_to_get, 0);

			return values[ind];
		}

		public T GetOldest()
		{
			return Get(values.Length - 1);
		}

		public void Reset()
		{
			index_at = 0;
		}

		public override string ToString()
		{
			return $"Index at: {index_at}\n" + values.Reduce((t, s, i) =>
			{
				//s += $"{i}: {Get(i)}\n"; 
				s += $"{i}: {t.ToString()}\n";
				return s;
			}, "");
		}
	}

	public static class ArrayFunctions
	{

		public static ReturnType[] Map<ArrType, ReturnType>(this ArrType[] array, Func<ArrType, ReturnType> func)
		{
			var res = new ReturnType[array.Length];
			for (int i = 0; i < array.Length; i++)
			{
				res[i] = func(array[i]);
			}
			return res;
		}

		public static ReturnType Reduce<ArrType, ReturnType>(this ArrType[] array, Func<ArrType, ReturnType, int, ReturnType> func, ReturnType start_value)
		{
			ReturnType result = start_value;

			for (int i = 0; i < array.Length; i++)
			{
				result = func(array[i], result, i);
			}
			return result;
		}


		/* DO NOT MUTATE VALUES OR STRUCTS INSIDE THIS FOREACH FUNCTION!! */
		// I accidentally did it in TickScript and wondered why timeouts didnt get removed
		public static void ForEach<ArrType>(this ArrType[] array, Action<ArrType> act)
		{
			for (int i = 0; i < array.Length; i++)
			{

				act(array[i]);
			}
		}
		public static T PickRandomly<T>(this T[] arr)
		{
			var rand = UnityEngine.Random.Range(0, arr.Length);
			return arr[rand];
		}

		public static T PickRandomly<T>(this T[] arr, out int index)
		{
			var rand = UnityEngine.Random.Range(0, arr.Length);
			index = rand;
			return arr[rand];
		}

		public static T PickRandomly<T>(this List<T> arr)
		{
			var rand = UnityEngine.Random.Range(0, arr.Count);
			return arr[rand];
		}

		public static void PrintArray<T>(this T[] arr)
		{
			Debug.Log(arr.Reduce((ob, acc, index) => acc = acc + ob.ToString() + "\n", ""));
		}

		public static void PrintList<T>(this IEnumerable<T> arr)
		{
			Debug.Log(arr.Aggregate("", (acc, x) => acc += x.ToString() + "\n"));
		}

		public static List<T> SpiralLoop<T>(this T[,] arr)
		{
			int rows = arr.GetLength(0);
			int cols = arr.GetLength(1);
			int centerX = rows / 2;
			int centerY = cols / 2;

			List<T> result = new List<T>();
			result.Add(arr[centerX, centerY]);

			// Directions: Right, Down, Left, Up
			int[] dx = { 0, 1, 0, -1 };
			int[] dy = { 1, 0, -1, 0 };

			int steps = 1;  // Steps before changing direction
			int x = centerX, y = centerY;
			int dir = 0; // Start moving right

			while (result.Count < rows * cols)
			{
				for (int i = 0; i < 2; i++) // Every two turns, increase step count
				{
					for (int j = 0; j < steps; j++)
					{
						x += dx[dir];
						y += dy[dir];

						if (x >= 0 && x < rows && y >= 0 && y < cols)
						{
							result.Add(arr[x, y]);
						}

						if (result.Count == rows * cols)
							return result;
					}
					dir = (dir + 1) % 4; // Change direction
				}
				steps++; // Increase step count every two turns
			}

			return result;
		}

		public static T[,] ShiftArray<T>(this T[,] array, int shiftX, int shiftY)
		{
			int rows = array.GetLength(0);
			int cols = array.GetLength(1);
			T[,] newArray = new T[rows, cols];

			for (int y = 0; y < rows; y++)
			{
				for (int x = 0; x < cols; x++)
				{
					// Compute new wrapped indices
					int newX = (x + shiftX) % cols;
					int newY = (y + shiftY) % rows;

					// Ensure positive indices
					if (newX < 0) newX += cols;
					if (newY < 0) newY += rows;

					// Assign value
					newArray[newY, newX] = array[y, x];
				}
			}

			return newArray;
		}


		public static T[] RepeatToArray<T>(T obj, int amount)
		{
			T[] res = new T[amount];
			for (int i = 0; i < amount; i++)
			{
				res[i] = obj;
			}
			return res;
		}

		public static Dictionary<TKey, TValue> MergeDictionary<TKey, TValue>(this Dictionary<TKey, TValue> first, Dictionary<TKey, TValue> second)
		{
			var result = new Dictionary<TKey, TValue>(first);
			foreach (var kvp in second)
			{
				result[kvp.Key] = kvp.Value;
			}
			return result;
		}
	}

	[System.Serializable]
	public class SinValue
	{
		public float Amplitude = 15;
		public float Frequency = 2;
		public float Offset = 0;

		public float Value
		{
			get
			{
				return Mathf.Sin((Time.time + Offset) * Mathf.PI * 2 * Frequency) * Amplitude;
			}
		}
	}
}
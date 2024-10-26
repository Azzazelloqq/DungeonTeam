namespace Code.ModelStructs
{
public static class ModelStructUtils
{
	public static UnityEngine.Vector3 ToUnityVector(this Vector3 vector3)
	{
		return new UnityEngine.Vector3(vector3.X, vector3.Y, vector3.Z);
	}
	
	public static Vector3 ToModelVector(this UnityEngine.Vector3 vector3)
	{
		return new Vector3(vector3.x, vector3.y, vector3.z);
	}
	
	public static UnityEngine.Vector2 ToUnityVector(this Vector2 vector2)
	{
		return new UnityEngine.Vector2(vector2.X, vector2.Y);
	}

	public static Vector2 ToModelVector(this UnityEngine.Vector2 vector2)
	{
		return new Vector2(vector2.x, vector2.y);
	}
}
}